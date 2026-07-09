using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core;

namespace ProductivityHub.Desktop;

public partial class App : Application
{
    public static DesktopSettings Settings { get; private set; } = new();

    // The unlocked master-vault key for this session (null = locked / not set up).
    public static byte[]? VaultKey { get; private set; }
    public static bool VaultUnlocked => VaultKey is not null;

    private TrayManager? _tray;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            Log(args.Exception);
            // A known .NET 9 WPF regression throws from the tooltip popup timer on
            // hover. It's harmless — swallow it silently rather than interrupting
            // the user with an error dialog.
            var stack = args.Exception.StackTrace ?? "";
            var isToolTipGlitch = stack.Contains("PopupControlService") || stack.Contains("ShowToolTip");
            if (!isToolTipGlitch)
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

        await SetupOrUnlockVaultAsync(null);

        var main = new MainWindow();
        main.Show();
        _tray = new TrayManager(main);

        await NotifyExpiringSecretsAsync();
        await NotifyDueTodosAsync();
    }

    // First run: offer to set a master password. Later runs: offer to unlock.
    // Either can be skipped — the vault just stays locked and secret values hidden.
    public static async Task SetupOrUnlockVaultAsync(Window? owner)
    {
        try
        {
            bool configured;
            string? hint = null;
            await using (var db = Db.Context())
            {
                var config = await VaultService.GetConfigAsync(db);
                configured = config is not null;
                hint = config?.Hint;
            }

            var win = new MasterPasswordWindow(configured, hint);
            if (owner is not null) win.Owner = owner;
            win.ShowDialog();
            if (win.Key is not null) VaultKey = win.Key;
        }
        catch (Exception ex) { Log(ex); }
    }

    public static void LockVault() => VaultKey = null;

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        base.OnExit(e);
    }

    // Reminder for todos due today or overdue.
    private static async Task NotifyDueTodosAsync()
    {
        try
        {
            var todayEnd = new DateTimeOffset(DateTime.Today).AddDays(1);
            await using var db = Db.Context();
            var due = await db.Todos.Where(t => !t.IsDone && t.DueDate != null && t.DueDate < todayEnd)
                .OrderBy(t => t.DueDate).ToListAsync();
            if (due.Count == 0) return;

            var today = DateTime.Today;
            var lines = due.Select(t =>
            {
                var d = t.DueDate!.Value.Date;
                var when = d < today ? "overdue" : "due today";
                return $"• {t.Title} — {when}";
            });
            MessageBox.Show(string.Join("\n", lines), "✅ Tasks due");
        }
        catch (Exception ex) { Log(ex); }
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
