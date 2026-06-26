using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ProductivityHub.Api.Data;
using ProductivityHub.Api.Data.Entities;

namespace ProductivityHub.Api.Services;

public class LinkImportOptions
{
    public bool Enabled { get; set; }
    // Local folder the OneDrive client syncs the flow's link files into.
    // Environment variables (e.g. %OneDriveCommercial%) are expanded.
    public string FolderPath { get; set; } = "";
    public int IntervalSeconds { get; set; } = 60;
}

public record ImportResult(
    bool Enabled,
    string FolderPath,
    bool FolderExists,
    int FilesProcessed,
    int Imported,
    int Duplicates,
    int SkippedNoUrl,
    List<string> Errors);

// Scans the configured folder for files dropped by the Teams→OneDrive flow,
// extracts URLs, and adds them to the bookmarks/read-later list.
public partial class LinkImportService(
    AppDbContext db,
    IOptions<LinkImportOptions> options,
    ILogger<LinkImportService> logger)
{
    [GeneratedRegex(@"https?://[^\s<>""')\]]+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    public async Task<ImportResult> ImportAsync(CancellationToken ct)
    {
        var opt = options.Value;
        var errors = new List<string>();

        if (!opt.Enabled || string.IsNullOrWhiteSpace(opt.FolderPath))
            return new ImportResult(false, opt.FolderPath, false, 0, 0, 0, 0, errors);

        var folder = Environment.ExpandEnvironmentVariables(opt.FolderPath);
        if (!Directory.Exists(folder))
            return new ImportResult(true, folder, false, 0, 0, 0, 0, errors);

        int processed = 0, imported = 0, duplicates = 0, skipped = 0;

        // Top-level files only — our own _skipped subfolder is left alone.
        foreach (var file in Directory.EnumerateFiles(folder))
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var content = await File.ReadAllTextAsync(file, ct);
                var urls = ExtractUrls(content);

                if (urls.Count == 0)
                {
                    MoveTo(folder, "_skipped", file);
                    skipped++;
                    processed++;
                    continue;
                }

                foreach (var url in urls)
                {
                    if (await db.Bookmarks.AnyAsync(b => b.Url == url, ct))
                    {
                        duplicates++;
                        continue;
                    }
                    db.Bookmarks.Add(new Bookmark
                    {
                        Id = Guid.NewGuid(),
                        Url = url,
                        CreatedAt = DateTimeOffset.UtcNow,
                    });
                    imported++;
                }

                await db.SaveChangesAsync(ct);
                File.Delete(file);
                processed++;
            }
            catch (IOException ex)
            {
                // Often the file is still being written/synced by OneDrive — leave it for next pass.
                errors.Add($"{Path.GetFileName(file)}: {ex.Message}");
            }
            catch (Exception ex)
            {
                errors.Add($"{Path.GetFileName(file)}: {ex.Message}");
                logger.LogWarning(ex, "Link import failed for {File}", file);
            }
        }

        return new ImportResult(true, folder, true, processed, imported, duplicates, skipped, errors);
    }

    private static List<string> ExtractUrls(string content) =>
        UrlRegex().Matches(content)
            .Select(m => m.Value.TrimEnd('.', ',', ';', ')', ']', '}', '"', '\''))
            .Distinct()
            .ToList();

    private static void MoveTo(string folder, string sub, string file)
    {
        var dir = Path.Combine(folder, sub);
        Directory.CreateDirectory(dir);
        var dest = Path.Combine(dir, Path.GetFileName(file));
        if (File.Exists(dest))
            dest = Path.Combine(dir, $"{Guid.NewGuid():N}-{Path.GetFileName(file)}");
        File.Move(file, dest);
    }
}

// Periodically drains the import folder while the app is running, so links
// forwarded from Teams show up without pressing the button.
public class LinkImportBackgroundService(
    IServiceProvider services,
    IOptions<LinkImportOptions> options,
    ILogger<LinkImportBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opt = options.Value;
        if (!opt.Enabled) return;

        var interval = TimeSpan.FromSeconds(Math.Max(15, opt.IntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var importer = scope.ServiceProvider.GetRequiredService<LinkImportService>();
                await importer.ImportAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Scheduled link import failed");
            }

            try { await Task.Delay(interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
