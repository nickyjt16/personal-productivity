using Microsoft.Extensions.Logging;

namespace ProductivityHub.Core.Data;

// Copies the SQLite file to a timestamped backup on startup (before any schema
// changes), keeping the most recent few. Cheap insurance against corruption or
// an accidental wipe.
public static class DbBackup
{
    private const int KeepCount = 10;

    public static void CreateStartupBackup(string connectionString, ILogger logger)
    {
        var path = ParseDataSource(connectionString);
        if (path is null || !File.Exists(path)) return; // first run — nothing to back up yet

        try
        {
            var full = Path.GetFullPath(path);
            var dir = Path.Combine(Path.GetDirectoryName(full) ?? ".", "backups");
            Directory.CreateDirectory(dir);

            var name = Path.GetFileNameWithoutExtension(full);
            var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
            var dest = Path.Combine(dir, $"{name}-{stamp}.db");
            if (!File.Exists(dest))
                File.Copy(full, dest);

            // Keep only the most recent KeepCount backups.
            var stale = Directory.GetFiles(dir, $"{name}-*.db")
                .OrderByDescending(f => f)
                .Skip(KeepCount);
            foreach (var old in stale)
            {
                try { File.Delete(old); } catch { /* best effort */ }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Startup database backup failed");
        }
    }

    private static string? ParseDataSource(string connectionString)
    {
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0].Trim().Equals("Data Source", StringComparison.OrdinalIgnoreCase))
                return kv[1].Trim();
        }
        return null;
    }
}
