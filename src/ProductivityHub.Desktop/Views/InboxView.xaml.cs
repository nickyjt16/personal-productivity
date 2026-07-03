using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Desktop.Views;

public partial class InboxView : UserControl
{
    public InboxView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        await using var db = Db.Context();
        ItemsHost.ItemsSource = await db.InboxItems
            .Where(i => !i.IsProcessed)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    private void TextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) Capture_Click(sender, e);
    }

    private async void Capture_Click(object sender, RoutedEventArgs e)
    {
        var text = TextBox.Text.Trim();
        if (text.Length == 0) return;
        await using (var db = Db.Context())
        {
            db.InboxItems.Add(new InboxItem { Id = Guid.NewGuid(), Text = text, CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }
        TextBox.Text = "";
        await LoadAsync();
    }

    private async void ToTodo_Click(object sender, RoutedEventArgs e)
    {
        var item = (InboxItem)((FrameworkElement)sender).DataContext;
        await using (var db = Db.Context())
        {
            db.Todos.Add(new TodoItem { Id = Guid.NewGuid(), Title = item.Text, CreatedAt = DateTimeOffset.UtcNow });
            var i = await db.InboxItems.FindAsync(item.Id);
            if (i != null) { i.IsProcessed = true; i.ProcessedAt = DateTimeOffset.UtcNow; }
            await db.SaveChangesAsync();
        }
        await LoadAsync();
    }

    private async void Process_Click(object sender, RoutedEventArgs e)
    {
        var item = (InboxItem)((FrameworkElement)sender).DataContext;
        await using (var db = Db.Context())
        {
            var i = await db.InboxItems.FindAsync(item.Id);
            if (i != null) { i.IsProcessed = true; i.ProcessedAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(); }
        }
        await LoadAsync();
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        var item = (InboxItem)((FrameworkElement)sender).DataContext;
        await using (var db = Db.Context())
        {
            var i = await db.InboxItems.FindAsync(item.Id);
            if (i != null) { db.InboxItems.Remove(i); await db.SaveChangesAsync(); }
        }
        await LoadAsync();
    }
}
