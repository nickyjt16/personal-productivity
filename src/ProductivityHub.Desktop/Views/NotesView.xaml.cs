using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Desktop.Views;

public partial class NotesView : UserControl
{
    private Guid? _selectedId;

    public NotesView()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadListAsync();
    }

    private async Task LoadListAsync()
    {
        await using var db = Db.Context();
        var notes = await db.Notes.OrderByDescending(n => n.UpdatedAt).ToListAsync();
        ListHost.ItemsSource = notes.Select(n => new NoteListRow
        {
            Id = n.Id,
            Title = string.IsNullOrWhiteSpace(n.Title) ? "Untitled" : n.Title!,
            Preview = n.Body.Length > 60 ? n.Body[..60] : n.Body,
        }).ToList();
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        _selectedId = null;
        TitleBox.Text = "";
        BodyBox.Text = "";
        DeleteBtn.Visibility = Visibility.Collapsed;
    }

    private async void Select_Click(object sender, RoutedEventArgs e)
    {
        var row = (NoteListRow)((FrameworkElement)sender).DataContext;
        await using var db = Db.Context();
        var note = await db.Notes.FindAsync(row.Id);
        if (note == null) return;
        _selectedId = note.Id;
        TitleBox.Text = note.Title ?? "";
        BodyBox.Text = note.Body;
        DeleteBtn.Visibility = Visibility.Visible;
    }

    private async Task<Guid?> SaveInternalAsync()
    {
        var title = string.IsNullOrWhiteSpace(TitleBox.Text) ? null : TitleBox.Text.Trim();
        var body = BodyBox.Text;
        if (title is null && string.IsNullOrWhiteSpace(body)) return null;

        await using var db = Db.Context();
        if (_selectedId is Guid id)
        {
            var n = await db.Notes.FindAsync(id);
            if (n != null) { n.Title = title; n.Body = body; n.UpdatedAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(); }
            return id;
        }
        var now = DateTimeOffset.UtcNow;
        var note = new Note { Id = Guid.NewGuid(), Title = title, Body = body, CreatedAt = now, UpdatedAt = now };
        db.Notes.Add(note);
        await db.SaveChangesAsync();
        _selectedId = note.Id;
        DeleteBtn.Visibility = Visibility.Visible;
        return note.Id;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        await SaveInternalAsync();
        await LoadListAsync();
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedId is not Guid id) return;
        await using (var db = Db.Context())
        {
            var n = await db.Notes.FindAsync(id);
            if (n != null) { db.Notes.Remove(n); await db.SaveChangesAsync(); }
        }
        New_Click(sender, e);
        await LoadListAsync();
    }

    private async void Projects_Click(object sender, RoutedEventArgs e)
    {
        var id = await SaveInternalAsync();
        if (id is null) return;
        var dlg = new ProjectAssignDialog("note", id.Value) { Owner = Window.GetWindow(this) };
        dlg.ShowDialog();
        await LoadListAsync();
    }
}
