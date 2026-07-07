using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ProductivityHub.Desktop.Views;

namespace ProductivityHub.Desktop;

public partial class MainWindow : Window
{
    private record NavItem(string Key, string Label);

    private static readonly NavItem[] AllSections =
    {
        new("dashboard", "🏠  Dashboard"),
        new("todos", "✅  Todos"),
        new("inbox", "📥  Inbox"),
        new("bookmarks", "🔖  Bookmarks"),
        new("notes", "📝  Notes"),
        new("journal", "📔  Journal"),
        new("projects", "📁  Projects"),
        new("secrets", "🔑  Secrets"),
        new("environments", "🌐  Environments"),
        new("pomodoro", "🍅  Pomodoro"),
    };

    private Button? _activeButton;

    public MainWindow()
    {
        InitializeComponent();
        BuildNav();
        Navigate("dashboard");
    }

    public void BuildNav()
    {
        NavPanel.Children.Clear();
        foreach (var s in AllSections)
        {
            if (s.Key != "dashboard" && !App.Settings.IsVisible(s.Key)) continue;
            var btn = new Button
            {
                // Explicit white TextBlock so the text colour is never lost to
                // WPF's Foreground-through-template quirks.
                Content = new System.Windows.Controls.TextBlock
                {
                    Text = s.Label,
                    Foreground = System.Windows.Media.Brushes.White,
                },
                Style = (Style)FindResource("NavButton"),
                Tag = s.Key,
            };
            btn.Click += (_, _) => Navigate(s.Key, btn);
            NavPanel.Children.Add(btn);
        }
    }

    // Public so views (e.g. the dashboard cards) can navigate between sections.
    public void Navigate(string key, Button? source = null)
    {
        MainContent.Content = CreateView(key);
        Highlight(source ?? NavPanel.Children.OfType<Button>().FirstOrDefault(b => (string?)b.Tag == key));
    }

    private void Highlight(Button? button)
    {
        if (_activeButton != null) _activeButton.Background = Brushes.Transparent;
        _activeButton = button;
        if (button != null) button.Background = (Brush)FindResource("SidebarActiveBrush");
    }

    private UserControl CreateView(string key) => key switch
    {
        "todos" => new TodosView(),
        "inbox" => new InboxView(),
        "bookmarks" => new BookmarksView(),
        "notes" => new NotesView(),
        "journal" => new JournalView(),
        "projects" => new ProjectsView(),
        "secrets" => new SecretsView(),
        "environments" => new EnvironmentsView(),
        "pomodoro" => new PomodoroView(),
        _ => new DashboardView(),
    };

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        var q = SearchBox.Text.Trim();
        if (q.Length == 0) return;
        MainContent.Content = new SearchView(q);
        Highlight(null);
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        MainContent.Content = new SettingsView(this);
        Highlight(null);
    }

    // Called by SettingsView when section visibility changes.
    public void OnSectionsChanged() => BuildNav();
}
