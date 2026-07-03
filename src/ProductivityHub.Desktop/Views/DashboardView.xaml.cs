using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

namespace ProductivityHub.Desktop.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        var hour = DateTime.Now.Hour;
        Greeting.Text = (hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening") + " 👋";
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        await using var db = Db.Context();
        var openTodos = await db.Todos.Where(t => !t.IsDone)
            .OrderByDescending(t => t.Priority).ThenBy(t => t.DueDate).ToListAsync();

        OpenCount.Text = openTodos.Count.ToString();
        InboxCount.Text = (await db.InboxItems.CountAsync(i => !i.IsProcessed)).ToString();
        UnreadCount.Text = (await db.Bookmarks.CountAsync(b => !b.IsRead)).ToString();

        var rows = openTodos.Take(8).Select(t => new TodoRow
        {
            Id = t.Id, Title = t.Title, IsDone = t.IsDone, Priority = t.Priority, DueDate = t.DueDate,
        }).ToList();
        TasksHost.ItemsSource = rows;
        NoTasks.Visibility = rows.Count == 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

        var overdue = openTodos.Count(t => TodoRow.DueInfo(t.DueDate, false)?.label == "Overdue");
        OverdueBadge.Text = overdue > 0 ? $"{overdue} overdue" : "";
        OverdueBadge.Visibility = overdue > 0 ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }

    private async void Toggle_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        var row = (TodoRow)((System.Windows.FrameworkElement)sender).DataContext;
        await using (var db = Db.Context())
        {
            var t = await db.Todos.FindAsync(row.Id);
            if (t != null) { t.IsDone = true; t.CompletedAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(); }
        }
        await LoadAsync();
    }
}
