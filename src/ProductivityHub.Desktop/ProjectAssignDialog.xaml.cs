using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Desktop;

public partial class ProjectAssignDialog : Window
{
    private readonly string _type;
    private readonly Guid _id;

    public ProjectAssignDialog(string type, Guid id)
    {
        InitializeComponent();
        _type = type;
        _id = id;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        await using var db = Db.Context();
        var open = await db.Projects
            .Where(p => p.Status == ProjectStatus.New || p.Status == ProjectStatus.Active)
            .OrderBy(p => p.Name).ToListAsync();

        var linkedIds = await LinkedProjectIds(db);
        // Include already-linked (possibly closed) projects too.
        var linkedClosed = await db.Projects
            .Where(p => linkedIds.Contains(p.Id) && p.Status != ProjectStatus.New && p.Status != ProjectStatus.Active)
            .ToListAsync();

        List.Children.Clear();
        foreach (var p in open.Concat(linkedClosed).DistinctBy(p => p.Id).OrderBy(p => p.Name))
        {
            List.Children.Add(new CheckBox
            {
                Content = p.Name,
                IsChecked = linkedIds.Contains(p.Id),
                Tag = p.Id,
                Margin = new Thickness(0, 4, 0, 4),
            });
        }
        if (List.Children.Count == 0)
            List.Children.Add(new TextBlock { Text = "No projects yet. Create one on the Projects page." });
    }

    private async Task<HashSet<Guid>> LinkedProjectIds(Core.Data.AppDbContext db) => _type switch
    {
        "todo" => (await db.TodoProjects.Where(x => x.TodoItemId == _id).Select(x => x.ProjectId).ToListAsync()).ToHashSet(),
        "note" => (await db.NoteProjects.Where(x => x.NoteId == _id).Select(x => x.ProjectId).ToListAsync()).ToHashSet(),
        "bookmark" => (await db.BookmarkProjects.Where(x => x.BookmarkId == _id).Select(x => x.ProjectId).ToListAsync()).ToHashSet(),
        _ => new HashSet<Guid>(),
    };

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var chosen = List.Children.OfType<CheckBox>()
            .Where(c => c.IsChecked == true)
            .Select(c => (Guid)c.Tag!)
            .ToHashSet();

        await using var db = Db.Context();
        switch (_type)
        {
            case "todo":
                await Sync(db.TodoProjects, x => x.TodoItemId == _id, x => x.ProjectId,
                    pid => new TodoProject { TodoItemId = _id, ProjectId = pid }, chosen, db);
                break;
            case "note":
                await Sync(db.NoteProjects, x => x.NoteId == _id, x => x.ProjectId,
                    pid => new NoteProject { NoteId = _id, ProjectId = pid }, chosen, db);
                break;
            case "bookmark":
                await Sync(db.BookmarkProjects, x => x.BookmarkId == _id, x => x.ProjectId,
                    pid => new BookmarkProject { BookmarkId = _id, ProjectId = pid }, chosen, db);
                break;
        }
        DialogResult = true;
        Close();
    }

    private static async Task Sync<T>(DbSet<T> set, System.Linq.Expressions.Expression<Func<T, bool>> filter,
        Func<T, Guid> projectId, Func<Guid, T> make, HashSet<Guid> chosen, Core.Data.AppDbContext db) where T : class
    {
        var existing = await set.Where(filter).ToListAsync();
        set.RemoveRange(existing.Where(x => !chosen.Contains(projectId(x))));
        var have = existing.Select(projectId).ToHashSet();
        foreach (var pid in chosen.Where(p => !have.Contains(p)))
            set.Add(make(pid));
        await db.SaveChangesAsync();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
