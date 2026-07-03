using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core;

namespace ProductivityHub.Desktop;

public partial class App : Application
{
    public static DesktopSettings Settings { get; private set; } = new();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            Log(args.Exception);
            MessageBox.Show(args.Exception.Message, "Productivity Hub — error");
            args.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) => Log(args.ExceptionObject as Exception);

        Settings = DesktopSettings.Load();
        ApplyTheme(Settings.Theme);

        try
        {
            await Db.InitAsync();
        }
        catch (Exception ex)
        {
            Log(ex);
            MessageBox.Show("Failed to open the database: " + ex.Message, "Productivity Hub");
        }

        new MainWindow().Show();

        await NotifyExpiringSecretsAsync();
    }

    // A week's warning before any tracked secret expires.
    private static async Task NotifyExpiringSecretsAsync()
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var cutoff = today.AddDays(7);
            await using var db = Db.Context();
            var expiring = await db.Secrets.Where(s => s.ExpiresOn <= cutoff).OrderBy(s => s.ExpiresOn).ToListAsync();
            if (expiring.Count == 0) return;

            var lines = expiring.Select(s =>
            {
                var d = s.ExpiresOn.DayNumber - today.DayNumber;
                var when = d < 0 ? $"expired {-d}d ago" : d == 0 ? "expires today" : $"expires in {d}d";
                return $"• {s.Name} — {when}";
            });
            MessageBox.Show(string.Join("\n", lines), "🔑 Secrets expiring soon");
        }
        catch (Exception ex) { Log(ex); }
    }

    // Swaps the first merged dictionary (the theme) at runtime; Controls.xaml uses
    // DynamicResource so everything recolours immediately.
    public static void ApplyTheme(string theme)
    {
        var file = theme == "dark" ? "Dark" : "Light";
        var dict = new ResourceDictionary { Source = new Uri($"Themes/{file}.xaml", UriKind.Relative) };
        Current.Resources.MergedDictionaries[0] = dict;
        Settings.Theme = theme;
    }

    private static void Log(Exception? ex)
    {
        if (ex is null) return;
        try
        {
            File.AppendAllText(Path.Combine(AppPaths.DataDirectory, "desktop-error.log"),
                $"{DateTimeOffset.Now:o}  {ex}\n\n");
        }
        catch { /* ignore */ }
    }
}
