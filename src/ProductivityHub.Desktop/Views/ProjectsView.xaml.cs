using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Desktop.Views;

public partial class ProjectsView : UserControl
{
    private static readonly string[] Colors =
    {
        "#0d6efd", "#6610f2", "#6f42c1", "#d63384", "#dc3545", "#fd7e14",
        "#ffc107", "#198754", "#20c997", "#0dcaf0", "#6c757d", "#343a40",
    };

    private string _filter = "open";
    private string _newColor = Colors[0];
    private Guid? _editingId;
    private Guid? _selectedId;
    private ProjectStatus _selectedStatus;
    private bool _loadingDetail;

    public ProjectsView()
    {
        InitializeComponent();
        BuildSwatches();
        FilterCombo.ItemsSource = new[] { "Open", "Complete", "Archived", "All" };
        FilterCombo.SelectedIndex = 0;
        Loaded += async (_, _) => await LoadCardsAsync();
    }

    private void BuildSwatches()
    {
        foreach (var hex in Colors)
        {
            var btn = new Button
            {
                Width = 24, Height = 24, Margin = new Thickness(0, 0, 6, 6),
                Background = Palette.FromHex(hex), Tag = hex,
                BorderBrush = Brushes.Transparent, BorderThickness = new Thickness(3),
            };
            btn.Click += (_, _) => { _newColor = hex; HighlightSwatches(); };
            ColorPanel.Children.Add(btn);
        }
        HighlightSwatches();
    }

    private void HighlightSwatches()
    {
        foreach (var b in ColorPanel.Children.OfType<Button>())
            b.BorderBrush = (string)b.Tag! == _newColor ? Brushes.Gray : Brushes.Transparent;
    }

    private async Task LoadCardsAsync()
    {
        await using var db = Db.Context();
        IQueryable<Project> q = _filter switch
        {
            "complete" => db.Projects.Where(p => p.Status == ProjectStatus.Complete),
            "archived" => db.Projects.Where(p => p.Status == ProjectStatus.Archived),
            "all" => db.Projects,
            _ => db.Projects.Where(p => p.Status == ProjectStatus.New || p.Status == ProjectStatus.Active),
        };
        var projects = await q.OrderBy(p => p.Status).ThenBy(p => p.Name).ToListAsync();

        var rows = new List<ProjectRow>();
        foreach (var p in projects)
        {
            var todoTotal = await db.TodoProjects.CountAsync(x => x.ProjectId == p.Id);
            var todoDone = await db.TodoProjects.CountAsync(x => x.ProjectId == p.Id && x.TodoItem!.IsDone);
            var notes = await db.NoteProjects.CountAsync(x => x.ProjectId == p.Id);
            var bms = await db.BookmarkProjects.CountAsync(x => x.ProjectId == p.Id);
            rows.Add(new ProjectRow
            {
                Id = p.Id, Name = p.Name, Description = p.Description, Status = p.Status,
                ColorBrush = Palette.FromHex(p.Color),
                CountsText = $"{todoTotal} todos · {notes} notes · {bms} links",
                ProgressPct = todoTotal > 0 ? (double)todoDone / todoTotal * 100 : 0,
                HasProgress = todoTotal > 0,
            });
        }
        CardsHost.ItemsSource = rows;
    }

    private void Filter_Changed(object sender, SelectionChangedEventArgs e)
    {
        _filter = ((string?)FilterCombo.SelectedItem ?? "Open").ToLowerInvariant();
        Detail.Visibility = Visibility.Collapsed;
        _selectedId = null;
        if (IsLoaded) _ = LoadCardsAsync();
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (name.Length == 0) return;
        var desc = string.IsNullOrWhiteSpace(DescBox.Text) ? null : DescBox.Text.Trim();
        var now = DateTimeOffset.UtcNow;

        await using (var db = Db.Context())
        {
            if (_editingId is Guid id)
            {
                var p = await db.Projects.FindAsync(id);
                if (p != null) { p.Name = name; p.Description = desc; p.Color = _newColor; p.UpdatedAt = now; await db.SaveChangesAsync(); }
            }
            else
            {
                db.Projects.Add(new Project
                {
                    Id = Guid.NewGuid(), Name = name, Description = desc, Color = _newColor,
                    Status = ProjectStatus.New, CreatedAt = now, UpdatedAt = now,
                });
                await db.SaveChangesAsync();
            }
        }
        ResetInput();
        await LoadCardsAsync();
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e) => ResetInput();

    private void ResetInput()
    {
        _editingId = null;
        NameBox.Text = "";
        DescBox.Text = "";
        _newColor = Colors[0];
        HighlightSwatches();
        AddBtn.Content = "Add";
        CancelBtn.Visibility = Visibility.Collapsed;
    }

    private async void Card_Click(object sender, RoutedEventArgs e)
    {
        var row = (ProjectRow)((FrameworkElement)sender).DataContext;
        _selectedId = row.Id;
        await ShowDetailAsync();
    }

    private async Task ShowDetailAsync()
    {
        if (_selectedId is not Guid id) return;
        await using var db = Db.Context();
        var p = await db.Projects.FindAsync(id);
        if (p == null) { Detail.Visibility = Visibility.Collapsed; return; }

        _selectedStatus = p.Status;
        DetailName.Text = p.Name;
        Detail.Visibility = Visibility.Visible;

        StartBtn.Visibility = Vis(p.Status == ProjectStatus.New);
        CompleteBtn.Visibility = Vis(p.Status is ProjectStatus.New or ProjectStatus.Active);
        ArchiveBtn.Visibility = Vis(p.Status != ProjectStatus.Archived);
        ReactivateBtn.Visibility = Vis(p.Status is ProjectStatus.Complete or ProjectStatus.Archived);

        await LoadLinksAsync(db, id);
    }

    private static Visibility Vis(bool v) => v ? Visibility.Visible : Visibility.Collapsed;

    private async Task LoadLinksAsync(Core.Data.AppDbContext db, Guid pid)
    {
        _loadingDetail = true;

        var linkedTodoIds = await db.TodoProjects.Where(x => x.ProjectId == pid).Select(x => x.TodoItemId).ToListAsync();
        var linkedNoteIds = await db.NoteProjects.Where(x => x.ProjectId == pid).Select(x => x.NoteId).ToListAsync();
        var linkedBmIds = await db.BookmarkProjects.Where(x => x.ProjectId == pid).Select(x => x.BookmarkId).ToListAsync();

        var todos = await db.Todos.ToListAsync();
        var notes = await db.Notes.ToListAsync();
        var bookmarks = await db.Bookmarks.ToListAsync();

        FillLinked(LinkedTodos, todos.Where(t => linkedTodoIds.Contains(t.Id)).Select(t => new LinkRow { Id = t.Id, Label = t.Title }),
            tid => RemoveLink("todo", tid));
        FillLinked(LinkedNotes, notes.Where(n => linkedNoteIds.Contains(n.Id)).Select(n => new LinkRow { Id = n.Id, Label = string.IsNullOrWhiteSpace(n.Title) ? "Untitled" : n.Title! }),
            nid => RemoveLink("note", nid));
        FillLinked(LinkedBookmarks, bookmarks.Where(b => linkedBmIds.Contains(b.Id)).Select(b => new LinkRow { Id = b.Id, Label = b.Title ?? b.Url }),
            bid => RemoveLink("bookmark", bid));

        FillAddCombo(AddTodoCombo, todos.Where(t => !linkedTodoIds.Contains(t.Id)).Select(t => new LinkRow { Id = t.Id, Label = t.Title }));
        FillAddCombo(AddNoteCombo, notes.Where(n => !linkedNoteIds.Contains(n.Id)).Select(n => new LinkRow { Id = n.Id, Label = string.IsNullOrWhiteSpace(n.Title) ? "Untitled" : n.Title! }));
        FillAddCombo(AddBookmarkCombo, bookmarks.Where(b => !linkedBmIds.Contains(b.Id)).Select(b => new LinkRow { Id = b.Id, Label = b.Title ?? b.Url }));

        _loadingDetail = false;
    }

    private static void FillLinked(StackPanel host, IEnumerable<LinkRow> rows, Action<Guid> onRemove)
    {
        host.Children.Clear();
        foreach (var row in rows)
        {
            var dp = new DockPanel { Margin = new Thickness(0, 2, 0, 2) };
            var btn = new Button { Content = "✕", FontSize = 10, Padding = new Thickness(5, 1, 5, 1) };
            var id = row.Id;
            btn.Click += (_, _) => onRemove(id);
            DockPanel.SetDock(btn, Dock.Right);
            dp.Children.Add(btn);
            dp.Children.Add(new TextBlock { Text = row.Label, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center });
            host.Children.Add(dp);
        }
        if (host.Children.Count == 0)
            host.Children.Add(new TextBlock { Text = "None yet.", Foreground = Brushes.Gray, FontSize = 12 });
    }

    private static void FillAddCombo(ComboBox combo, IEnumerable<LinkRow> rows)
    {
        var items = new List<LinkRow> { new() { Id = Guid.Empty, Label = "+ Add existing…" } };
        items.AddRange(rows);
        combo.ItemsSource = items;
        combo.SelectedIndex = 0;
    }

    private async void AddTodo_Changed(object sender, SelectionChangedEventArgs e) => await AddLink("todo", AddTodoCombo);
    private async void AddNote_Changed(object sender, SelectionChangedEventArgs e) => await AddLink("note", AddNoteCombo);
    private async void AddBookmark_Changed(object sender, SelectionChangedEventArgs e) => await AddLink("bookmark", AddBookmarkCombo);

    private async Task AddLink(string type, ComboBox combo)
    {
        if (_loadingDetail || _selectedId is not Guid pid) return;
        if (combo.SelectedValue is not Guid itemId || itemId == Guid.Empty) return;

        await using (var db = Db.Context())
        {
            switch (type)
            {
                case "todo": db.TodoProjects.Add(new TodoProject { TodoItemId = itemId, ProjectId = pid }); break;
                case "note": db.NoteProjects.Add(new NoteProject { NoteId = itemId, ProjectId = pid }); break;
                case "bookmark": db.BookmarkProjects.Add(new BookmarkProject { BookmarkId = itemId, ProjectId = pid }); break;
            }
            await db.SaveChangesAsync();
        }
        await ShowDetailAsync();
        await LoadCardsAsync();
    }

    private async void RemoveLink(string type, Guid itemId)
    {
        if (_selectedId is not Guid pid) return;
        await using (var db = Db.Context())
        {
            switch (type)
            {
                case "todo": await db.TodoProjects.Where(x => x.ProjectId == pid && x.TodoItemId == itemId).ExecuteDeleteAsync(); break;
                case "note": await db.NoteProjects.Where(x => x.ProjectId == pid && x.NoteId == itemId).ExecuteDeleteAsync(); break;
                case "bookmark": await db.BookmarkProjects.Where(x => x.ProjectId == pid && x.BookmarkId == itemId).ExecuteDeleteAsync(); break;
            }
        }
        await ShowDetailAsync();
        await LoadCardsAsync();
    }

    private async void Start_Click(object sender, RoutedEventArgs e) => await SetStatus(ProjectStatus.Active);
    private async void Complete_Click(object sender, RoutedEventArgs e) => await SetStatus(ProjectStatus.Complete);
    private async void Archive_Click(object sender, RoutedEventArgs e) => await SetStatus(ProjectStatus.Archived);
    private async void Reactivate_Click(object sender, RoutedEventArgs e) => await SetStatus(ProjectStatus.Active);

    private async Task SetStatus(ProjectStatus status)
    {
        if (_selectedId is not Guid id) return;
        await using (var db = Db.Context())
        {
            var p = await db.Projects.FindAsync(id);
            if (p != null) { p.Status = status; p.UpdatedAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(); }
        }
        await LoadCardsAsync();
        // If the project left the current filter, hide detail; otherwise refresh it.
        if (_filter == "open" && status is ProjectStatus.Complete or ProjectStatus.Archived)
            Detail.Visibility = Visibility.Collapsed;
        else
            await ShowDetailAsync();
    }

    private async void EditProject_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedId is not Guid id) return;
        await using var db = Db.Context();
        var p = await db.Projects.FindAsync(id);
        if (p == null) return;
        _editingId = id;
        NameBox.Text = p.Name;
        DescBox.Text = p.Description ?? "";
        _newColor = p.Color;
        HighlightSwatches();
        AddBtn.Content = "Save";
        CancelBtn.Visibility = Visibility.Visible;
    }

    private async void DeleteProject_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedId is not Guid id) return;
        if (MessageBox.Show("Delete this project? Items stay; links are removed.", "Delete project",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;

        await using (var db = Db.Context())
        {
            await db.TodoProjects.Where(x => x.ProjectId == id).ExecuteDeleteAsync();
            await db.NoteProjects.Where(x => x.ProjectId == id).ExecuteDeleteAsync();
            await db.BookmarkProjects.Where(x => x.ProjectId == id).ExecuteDeleteAsync();
            var p = await db.Projects.FindAsync(id);
            if (p != null) { db.Projects.Remove(p); await db.SaveChangesAsync(); }
        }
        _selectedId = null;
        Detail.Visibility = Visibility.Collapsed;
        await LoadCardsAsync();
    }
}
