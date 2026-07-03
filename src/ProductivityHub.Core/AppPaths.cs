using System.IO;

namespace ProductivityHub.Core;

// Shared, stable locations so the web and desktop apps use the same data.
public static class AppPaths
{
    // %APPDATA%\ProductivityHub
    public static string DataDirectory
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ProductivityHub");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    // Optional pointer file: if present, its contents are the full path to the
    // database (e.g. a OneDrive-synced folder). Otherwise we use the default.
    private static string PointerFile => Path.Combine(DataDirectory, "db-location.txt");

    private static string DefaultDatabasePath => Path.Combine(DataDirectory, "productivityhub.db");

    public static string DatabasePath
    {
        get
        {
            try
            {
                if (File.Exists(PointerFile))
                {
                    var p = File.ReadAllText(PointerFile).Trim();
                    if (!string.IsNullOrWhiteSpace(p)) return p;
                }
            }
            catch { /* fall back to default */ }
            return DefaultDatabasePath;
        }
    }

    // The configured custom location, or null if using the default.
    public static string? ConfiguredLocation
    {
        get
        {
            try
            {
                if (File.Exists(PointerFile))
                {
                    var p = File.ReadAllText(PointerFile).Trim();
                    return string.IsNullOrWhiteSpace(p) ? null : p;
                }
            }
            catch { /* ignore */ }
            return null;
        }
    }

    // Point the database at a custom location (or pass null to revert to default).
    public static void SetDatabaseLocation(string? fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            if (File.Exists(PointerFile)) File.Delete(PointerFile);
        }
        else
        {
            File.WriteAllText(PointerFile, fullPath.Trim());
        }
    }

    public static string ConnectionString => $"Data Source={DatabasePath}";
}
