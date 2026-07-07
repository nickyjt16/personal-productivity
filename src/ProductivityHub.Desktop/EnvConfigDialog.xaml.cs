using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Desktop;

// Manages the connection-reference / environment-variable checklist for one
// environment. Talks to the shared SQLite DB in-process.
public partial class EnvConfigDialog : Window
{
    private readonly Guid _envId;

    public EnvConfigDialog(Guid envId, string envName)
    {
        InitializeComponent();
        _envId = envId;
        HeaderText.Text = $"🌐 {envName} — setup checklist";
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        await using var db = Db.Context();
        var rows = await db.EnvironmentConfigs.Where(c => c.EnvironmentId == _envId)
            .OrderBy(c => c.Kind).ThenBy(c => c.Name).ToListAsync();
        ItemsHost.ItemsSource = rows.Select(c => new EnvConfigRow
        {
            Id = c.Id, Kind = c.Kind, Name = c.Name, Value = c.Value, Solution = c.Solution, IsSet = c.IsSet,
        }).ToList();
        EmptyText.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (name.Length == 0) { MessageBox.Show("Name is required.", "Checklist"); return; }
        var tag = (KindBox.SelectedItem as ComboBoxItem)?.Tag as string;
        var kind = Enum.TryParse<EnvConfigKind>(tag, out var k) ? k : EnvConfigKind.ConnectionReference;
        string? Clean(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        await using (var db = Db.Context())
        {
            db.EnvironmentConfigs.Add(new EnvironmentConfig
            {
                Id = Guid.NewGuid(), EnvironmentId = _envId, Kind = kind, Name = name,
                Value = Clean(ValueBox.Text), Solution = Clean(SolutionBox.Text),
            });
            await db.SaveChangesAsync();
        }
        NameBox.Text = ""; ValueBox.Text = ""; SolutionBox.Text = "";
        await LoadAsync();
    }

    private async void Toggle_Click(object sender, RoutedEventArgs e)
    {
        var row = (EnvConfigRow)((FrameworkElement)sender).DataContext;
        await using (var db = Db.Context())
        {
            var c = await db.EnvironmentConfigs.FindAsync(row.Id);
            if (c != null) { c.IsSet = !c.IsSet; await db.SaveChangesAsync(); }
        }
        await LoadAsync();
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        var row = (EnvConfigRow)((FrameworkElement)sender).DataContext;
        await using (var db = Db.Context())
        {
            var c = await db.EnvironmentConfigs.FindAsync(row.Id);
            if (c != null) { db.EnvironmentConfigs.Remove(c); await db.SaveChangesAsync(); }
        }
        await LoadAsync();
    }
}
