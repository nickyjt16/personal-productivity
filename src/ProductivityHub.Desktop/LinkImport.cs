using System.IO;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Desktop;

// Scans the configured OneDrive-synced folder for link files (from the Teams
// Power Automate flow) and imports URLs into bookmarks.
public static partial class LinkImport
{
    [GeneratedRegex(@"https?://[^\s<>""')\]]+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRx();

    public record Result(bool FolderOk, string Folder, int Imported, int Duplicates, int Skipped);

    public static async Task<Result> RunAsync(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) return new Result(false, "", 0, 0, 0);
        var dir = Environment.ExpandEnvironmentVariables(folder);
        if (!Directory.Exists(dir)) return new Result(false, dir, 0, 0, 0);

        int imported = 0, duplicates = 0, skipped = 0;
        foreach (var file in Directory.EnumerateFiles(dir))
        {
            try
            {
                var content = await File.ReadAllTextAsync(file);
                var urls = UrlRx().Matches(content)
                    .Select(m => m.Value.TrimEnd('.', ',', ';', ')', ']', '}', '"', '\''))
                    .Distinct().ToList();

                if (urls.Count == 0) { MoveToSkipped(dir, file); skipped++; continue; }

                await using var db = Db.Context();
                foreach (var url in urls)
                {
                    if (await db.Bookmarks.AnyAsync(b => b.Url == url)) { duplicates++; continue; }
                    db.Bookmarks.Add(new Bookmark
                    {
                        Id = Guid.NewGuid(), Url = url, Title = await WebTitle.FetchAsync(url),
                        CreatedAt = DateTimeOffset.UtcNow,
                    });
                    imported++;
                }
                await db.SaveChangesAsync();
                File.Delete(file);
            }
            catch { /* leave the file for next time */ }
        }
        return new Result(true, dir, imported, duplicates, skipped);
    }

    private static void MoveToSkipped(string dir, string file)
    {
        var sub = Path.Combine(dir, "_skipped");
        Directory.CreateDirectory(sub);
        var dest = Path.Combine(sub, Path.GetFileName(file));
        if (File.Exists(dest)) dest = Path.Combine(sub, $"{Guid.NewGuid():N}-{Path.GetFileName(file)}");
        File.Move(file, dest);
    }
}
