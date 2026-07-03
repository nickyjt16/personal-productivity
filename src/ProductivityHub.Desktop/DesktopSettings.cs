using System.IO;
using System.Text.Json;
using ProductivityHub.Core;

namespace ProductivityHub.Desktop;

// Per-device preferences (theme, hidden sections, link-import folder), stored as
// JSON alongside the database.
public class DesktopSettings
{
    public string Theme { get; set; } = "light";
    public HashSet<string> HiddenSections { get; set; } = new();
    public string? LinkImportFolder { get; set; }
    public bool AutoFetchTitles { get; set; } = true;

    private static string FilePath => Path.Combine(AppPaths.DataDirectory, "desktop-settings.json");

    public static DesktopSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<DesktopSettings>(File.ReadAllText(FilePath)) ?? new DesktopSettings();
        }
        catch { /* fall through to defaults */ }
        return new DesktopSettings();
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* best effort */ }
    }

    public bool IsVisible(string key) => !HiddenSections.Contains(key);

    public void SetVisible(string key, bool visible)
    {
        if (visible) HiddenSections.Remove(key);
        else HiddenSections.Add(key);
        Save();
    }
}
