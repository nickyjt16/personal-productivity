using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Desktop.Views;

public partial class TodosView : UserControl
{
    private Guid? _editingId;
    private Guid? _filterProjectId;

    public TodosView()
    {
        InitializeComponent();
        PriorityBox.ItemsSource = new[] { "Low", "Medium", "High" };
        PriorityBox.SelectedItem = "Medium";
        Loaded += async (_, _) => { await LoadFilterAsync(); await LoadAsync(); };
    }

    private async Task LoadFilterAsync()
    {
        await using var db = Db.Context();
        var projects = await db.Projects
            .Where(p => p.Status == ProjectStatus.New || p.Status == ProjectStatus.Active)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name }).ToListAsync();

        var items = new List<ComboItem> { new() { Id = null, Label = "All projects" } };
        items.AddRange(projects.Select(p => new ComboItem { Id = p.Id, Label = p.Name }));
        FilterCombo.ItemsSource = items;
        FilterCombo.SelectedIndex = 0;
    }

    private async Task LoadAsync()
    {
        await using var db = Db.Context();
        var q = db.Todos.Include(t => t.ProjectLinks).ThenInclude(l => l.Project).AsQueryable();
        if (_filterProjectId is Guid pid) q = q.Where(t => t.ProjectLinks.Any(l => l.ProjectId == pid));

        var todos = await q
            .OrderBy(t => t.IsDone)
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync();

        ItemsHost.ItemsSource = todos.Select(t => new TodoRow
        {
            Id = t.Id,
            Title = t.Title,
            Notes = t.Notes,
            IsDone = t.IsDone,
            Priority = t.Priority,
            DueDate = t.DueDate,
            ProjectTags = t.ProjectLinks.Count == 0 ? "" : "🏷 " + string.Join(", ", t.ProjectLinks.Select(l => l.Project!.Name)),
        }).ToList();
    }

    private void Filter_Changed(object sender, SelectionChangedEventArgs e)
    {
        _filterProjectId = FilterCombo.SelectedValue as Guid?;
        if (IsLoaded) _ = LoadAsync();
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        var title = TitleBox.Text.Trim();
        if (title.Length == 0) return;

        var priority = Enum.Parse<Priority>((string?)PriorityBox.SelectedItem ?? "Medium");
        DateTimeOffset? due = DueBox.SelectedDate is DateTime d ? new DateTimeOffset(d) : null;
        var notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim();

        await using (var db = Db.Context())
        {
            if (_editingId is Guid id)
            {
                var t = await db.Todos.FindAsync(id);
                if (t != null)
                {
                    t.Title = title; t.Notes = notes; t.Priority = priority; t.DueDate = due;
                    await db.SaveChangesAsync();
                }
            }
            else
            {
                db.Todos.Add(new TodoItem
                {
                    Id = Guid.NewGuid(), Title = title, Notes = notes, Priority = priority,
                    DueDate = due, CreatedAt = DateTimeOffset.UtcNow,
                });
                await db.SaveChangesAsync();
            }
        }
        ResetInput();
        await LoadAsync();
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        var row = (TodoRow)((FrameworkElement)sender).DataContext;
        _editingId = row.Id;
        TitleBox.Text = row.Title;
        NotesBox.Text = row.Notes ?? "";
        PriorityBox.SelectedItem = row.Priority.ToString();
        DueBox.SelectedDate = row.DueDate?.DateTime;
        AddBtn.Content = "Save";
        CancelBtn.Visibility = Visibility.Visible;
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e) => ResetInput();

    private void ResetInput()
    {
        _editingId = null;
        TitleBox.Text = "";
        NotesBox.Text = "";
        PriorityBox.SelectedItem = "Medium";
        DueBox.SelectedDate = null;
        AddBtn.Content = "Add";
        CancelBtn.Visibility = Visibility.Collapsed;
    }

    private async void Toggle_Click(object sender, RoutedEventArgs e)
    {
        var row = (TodoRow)((FrameworkElement)sender).DataContext;
        await using (var db = Db.Context())
        {
            var t = await db.Todos.FindAsync(row.Id);
            if (t != null)
            {
                t.IsDone = !t.IsDone;
                t.CompletedAt = t.IsDone ? DateTimeOffset.UtcNow : null;
                await db.SaveChangesAsync();
            }
        }
        await LoadAsync();
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        var row = (TodoRow)((FrameworkElement)sender).DataContext;
        await using (var db = Db.Context())
        {
            var t = await db.Todos.FindAsync(row.Id);
            if (t != null) { db.Todos.Remove(t); await db.SaveChangesAsync(); }
        }
        await LoadAsync();
    }

    private async void Projects_Click(object sender, RoutedEventArgs e)
    {
        var row = (TodoRow)((FrameworkElement)sender).DataContext;
        var dlg = new ProjectAssignDialog("todo", row.Id) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) await LoadAsync();
    }
}
