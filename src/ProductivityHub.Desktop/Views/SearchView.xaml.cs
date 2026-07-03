using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

namespace ProductivityHub.Desktop.Views;

public partial class SearchView : UserControl
{
    private readonly string _query;

    public SearchView(string query)
    {
        InitializeComponent();
        _query = query;
        Heading.Text = $"🔎 Results for “{query}”";
        Loaded += async (_, _) => await SearchAsync();
    }

    private async Task SearchAsync()
    {
        var q = _query.Trim();
        bool Match(string? s) => s != null && s.Contains(q, StringComparison.OrdinalIgnoreCase);

        await using var db = Db.Context();
        var todos = (await db.Todos.ToListAsync()).Where(t => Match(t.Title) || Match(t.Notes)).ToList();
        var notes = (await db.Notes.ToListAsync()).Where(n => Match(n.Title) || Match(n.Body)).ToList();
        var bookmarks = (await db.Bookmarks.ToListAsync()).Where(b => Match(b.Title) || Match(b.Url) || Match(b.Notes)).ToList();

        Results.Children.Clear();
        var total = todos.Count + notes.Count + bookmarks.Count;
        Empty.Visibility = total == 0 ? Visibility.Visible : Visibility.Collapsed;

        AddSection("✅ Todos", todos.Select(t => (t.Title, (string?)null)));
        AddSection("📝 Notes", notes.Select(n => (string.IsNullOrWhiteSpace(n.Title) ? "Untitled" : n.Title!, (string?)null)));
        AddSection("🔖 Bookmarks", bookmarks.Select(b => (b.Title ?? b.Url, (string?)b.Url)));
    }

    private void AddSection(string title, IEnumerable<(string label, string? url)> items)
    {
        var list = items.ToList();
        if (list.Count == 0) return;

        Results.Children.Add(new TextBlock
        {
            Text = $"{title} ({list.Count})",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 12, 0, 6),
        });

        foreach (var (label, url) in list)
        {
            var card = new Border
            {
                Style = (Style)FindResource("Card"),
                Margin = new Thickness(0, 0, 0, 6),
                Padding = new Thickness(12, 8, 12, 8),
            };
            if (url is null)
            {
                card.Child = new TextBlock { Text = label };
            }
            else
            {
                var dp = new DockPanel();
                var open = new Button { Content = "Open", Padding = new Thickness(7, 4, 7, 4) };
                var u = url;
                open.Click += (_, _) => { try { Process.Start(new ProcessStartInfo(u) { UseShellExecute = true }); } catch { } };
                DockPanel.SetDock(open, Dock.Right);
                dp.Children.Add(open);
                dp.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
                card.Child = dp;
            }
            Results.Children.Add(card);
        }
    }
}
