using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using ProductivityHub.Core;

namespace ProductivityHub.Desktop.Views;

public partial class SettingsView : UserControl
{
    private static readonly (string Key, string Label)[] Sections =
    {
        ("todos", "✅  Todos"), ("inbox", "📥  Inbox"), ("bookmarks", "🔖  Bookmarks"),
        ("notes", "📝  Notes"), ("journal", "📔  Journal"), ("projects", "📁  Projects"),
        ("pomodoro", "🍅  Pomodoro"),
    };

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly MainWindow _main;

    public SettingsView(MainWindow main)
    {
        InitializeComponent();
        _main = main;

        DarkCheck.IsChecked = App.Settings.Theme == "dark";
        TitlesCheck.IsChecked = App.Settings.AutoFetchTitles;
        FolderBox.Text = App.Settings.LinkImportFolder ?? "";

        foreach (var (key, label) in Sections)
        {
            var cb = new CheckBox { Content = label, IsChecked = App.Settings.IsVisible(key), Margin = new Thickness(0, 4, 0, 4), Tag = key };
            cb.Click += (_, _) =>
            {
                App.Settings.SetVisible(key, cb.IsChecked == true);
                _main.OnSectionsChanged();
            };
            SectionPanel.Children.Add(cb);
        }
    }

    private void Dark_Click(object sender, RoutedEventArgs e)
    {
        App.ApplyTheme(DarkCheck.IsChecked == true ? "dark" : "light");
        App.Settings.Save();
    }

    private void Titles_Click(object sender, RoutedEventArgs e)
    {
        App.Settings.AutoFetchTitles = TitlesCheck.IsChecked == true;
        App.Settings.Save();
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFolderDialog { Title = "Choose the link-import folder" };
        if (dlg.ShowDialog() == true) FolderBox.Text = dlg.FolderName;
    }

    private void SaveFolder_Click(object sender, RoutedEventArgs e)
    {
        App.Settings.LinkImportFolder = string.IsNullOrWhiteSpace(FolderBox.Text) ? null : FolderBox.Text.Trim();
        App.Settings.Save();
        ShowStatus("Folder saved.");
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            FileName = $"productivityhub-backup-{DateTime.Now:yyyy-MM-dd}.json",
            Filter = "JSON backup (*.json)|*.json",
        };
        if (dlg.ShowDialog() != true) return;

        await using var db = Db.Context();
        var backup = new BackupData(
            1, DateTimeOffset.UtcNow,
            await db.Todos.AsNoTracking().ToListAsync(),
            await db.InboxItems.AsNoTracking().ToListAsync(),
            await db.Bookmarks.AsNoTracking().ToListAsync(),
            await db.Notes.AsNoTracking().ToListAsync(),
            await db.JournalEntries.AsNoTracking().ToListAsync(),
            await db.PomodoroSessions.AsNoTracking().ToListAsync(),
            await db.Projects.AsNoTracking().ToListAsync(),
            await db.TodoProjects.AsNoTracking().ToListAsync(),
            await db.NoteProjects.AsNoTracking().ToListAsync(),
            await db.BookmarkProjects.AsNoTracking().ToListAsync());

        await File.WriteAllTextAsync(dlg.FileName, JsonSerializer.Serialize(backup, Json));
        ShowStatus("Backup exported.");
    }

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "JSON backup (*.json)|*.json" };
        if (dlg.ShowDialog() != true) return;
        if (MessageBox.Show("Importing a backup REPLACES all current data. Continue?", "Restore backup",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

        BackupData? backup;
        try { backup = JsonSerializer.Deserialize<BackupData>(await File.ReadAllTextAsync(dlg.FileName), Json); }
        catch { ShowStatus("That file isn’t a valid backup."); return; }
        if (backup is null) { ShowStatus("That file isn’t a valid backup."); return; }

        await using (var db = Db.Context())
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            await db.TodoProjects.ExecuteDeleteAsync();
            await db.NoteProjects.ExecuteDeleteAsync();
            await db.BookmarkProjects.ExecuteDeleteAsync();
            await db.PomodoroSessions.ExecuteDeleteAsync();
            await db.Todos.ExecuteDeleteAsync();
            await db.InboxItems.ExecuteDeleteAsync();
            await db.Bookmarks.ExecuteDeleteAsync();
            await db.Notes.ExecuteDeleteAsync();
            await db.JournalEntries.ExecuteDeleteAsync();
            await db.Projects.ExecuteDeleteAsync();

            db.Projects.AddRange(backup.Projects ?? []);
            db.Todos.AddRange(backup.Todos ?? []);
            db.Notes.AddRange(backup.Notes ?? []);
            db.Bookmarks.AddRange(backup.Bookmarks ?? []);
            db.InboxItems.AddRange(backup.InboxItems ?? []);
            db.JournalEntries.AddRange(backup.JournalEntries ?? []);
            db.PomodoroSessions.AddRange(backup.PomodoroSessions ?? []);
            db.TodoProjects.AddRange(backup.TodoProjects ?? []);
            db.NoteProjects.AddRange(backup.NoteProjects ?? []);
            db.BookmarkProjects.AddRange(backup.BookmarkProjects ?? []);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        ShowStatus("Backup restored.");
    }

    private async void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Delete ALL data? This cannot be undone.", "Clear all data",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

        await using var db = Db.Context();
        await db.TodoProjects.ExecuteDeleteAsync();
        await db.NoteProjects.ExecuteDeleteAsync();
        await db.BookmarkProjects.ExecuteDeleteAsync();
        await db.PomodoroSessions.ExecuteDeleteAsync();
        await db.Todos.ExecuteDeleteAsync();
        await db.InboxItems.ExecuteDeleteAsync();
        await db.Bookmarks.ExecuteDeleteAsync();
        await db.Notes.ExecuteDeleteAsync();
        await db.JournalEntries.ExecuteDeleteAsync();
        await db.Projects.ExecuteDeleteAsync();
        ShowStatus("All data cleared.");
    }

    private void ShowStatus(string text)
    {
        BackupStatus.Text = text;
        BackupStatus.Visibility = Visibility.Visible;
    }
}
