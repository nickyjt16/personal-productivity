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

    public static string DatabasePath => Path.Combine(DataDirectory, "productivityhub.db");

    public static string ConnectionString => $"Data Source={DatabasePath}";
}
