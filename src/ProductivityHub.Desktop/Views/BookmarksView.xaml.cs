using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Desktop.Views;

public partial class BookmarksView : UserControl
{
    public BookmarksView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        await using var db = Db.Context();
        var items = await db.Bookmarks
            .Include(b => b.ProjectLinks).ThenInclude(l => l.Project)
            .OrderBy(b => b.IsRead).ThenByDescending(b => b.CreatedAt)
            .ToListAsync();

        ItemsHost.ItemsSource = items.Select(b => new BookmarkRow
        {
            Id = b.Id, Url = b.Url, Title = b.Title, IsRead = b.IsRead,
            ProjectTags = b.ProjectLinks.Count == 0 ? "" : "🏷 " + string.Join(", ", b.ProjectLinks.Select(l => l.Project!.Name)),
        }).ToList();
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        var url = UrlBox.Text.Trim();
        if (url.Length == 0) return;
        var title = string.IsNullOrWhiteSpace(TitleBox.Text) ? await WebTitle.FetchAsync(url) : TitleBox.Text.Trim();
        await using (var db = Db.Context())
        {
            db.Bookmarks.Add(new Bookmark { Id = Guid.NewGuid(), Url = url, Title = title, CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }
        UrlBox.Text = ""; TitleBox.Text = "";
        ShowAddCard(false);
        await LoadAsync();
    }

    private void ToggleAdd_Click(object sender, RoutedEventArgs e) =>
        ShowAddCard(AddCard.Visibility != Visibility.Visible);

    private void ShowAddCard(bool show)
    {
        AddCard.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        AddToggleBtn.Content = show ? "Close" : "＋ Add bookmark";
        if (show) UrlBox.Focus();
    }

    private async void ToggleRead_Click(object sender, RoutedEventArgs e)
    {
        var row = (BookmarkRow)((FrameworkElement)sender).DataContext;
        await using (var db = Db.Context())
        {
            var b = await db.Bookmarks.FindAsync(row.Id);
            if (b != null) { b.IsRead = !b.IsRead; b.ReadAt = b.IsRead ? DateTimeOffset.UtcNow : null; await db.SaveChangesAsync(); }
        }
        await LoadAsync();
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        var row = (BookmarkRow)((FrameworkElement)sender).DataContext;
        try { Process.Start(new ProcessStartInfo(row.Url) { UseShellExecute = true }); } catch { }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        var row = (BookmarkRow)((FrameworkElement)sender).DataContext;
        await using (var db = Db.Context())
        {
            var b = await db.Bookmarks.FindAsync(row.Id);
            if (b != null) { db.Bookmarks.Remove(b); await db.SaveChangesAsync(); }
        }
        await LoadAsync();
    }

    private async void Projects_Click(object sender, RoutedEventArgs e)
    {
        var row = (BookmarkRow)((FrameworkElement)sender).DataContext;
        var dlg = new ProjectAssignDialog("bookmark", row.Id) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) await LoadAsync();
    }

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        Status.Text = "Checking…";
        var r = await LinkImport.RunAsync(App.Settings.LinkImportFolder);
        Status.Text = !r.FolderOk
            ? "Set a link-import folder in Settings (or it wasn't found)."
            : r.Imported == 0 && r.Duplicates == 0 ? "No new links found."
            : $"Imported {r.Imported}" + (r.Duplicates > 0 ? $", {r.Duplicates} already saved" : "") + ".";
        await LoadAsync();
    }
}
