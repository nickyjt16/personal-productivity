using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Desktop.Views;

public partial class EnvironmentsView : UserControl
{
    private Guid? _editingId;

    public EnvironmentsView()
    {
        InitializeComponent();
        TypeBox.ItemsSource = Enum.GetValues<EnvironmentType>();
        TypeBox.SelectedItem = EnvironmentType.Dev;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        await using var db = Db.Context();
        var envs = await db.Environments
            .Include(e => e.Configs)
            .Include(e => e.SecretLinks).ThenInclude(l => l.Secret)
            .OrderBy(e => e.Type).ThenBy(e => e.Name).ToListAsync();
        ItemsHost.ItemsSource = envs.Select(e => new EnvRow
        {
            Id = e.Id, Name = e.Name, Type = e.Type, Region = e.Region,
            PpEnvironmentId = e.PpEnvironmentId, Url = e.Url, TenantId = e.TenantId, Notes = e.Notes,
            ConfigTotal = e.Configs.Count, ConfigSet = e.Configs.Count(c => c.IsSet),
            SecretTags = e.SecretLinks.Count == 0 ? "" : "🔑 " + string.Join(", ", e.SecretLinks.Select(l => l.Secret!.Name)),
        }).ToList();
    }

    private void ToggleAdd_Click(object sender, RoutedEventArgs e)
    {
        if (AddCard.Visibility == Visibility.Visible) Reset();
        else ShowAddCard(true);
    }

    private void ShowAddCard(bool show)
    {
        AddCard.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        AddToggleBtn.Content = show ? "Close" : "＋ Add environment";
        if (show) NameBox.Focus();
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (name.Length == 0) { MessageBox.Show("Name is required.", "Environments"); return; }
        var type = TypeBox.SelectedItem is EnvironmentType t ? t : EnvironmentType.Dev;
        string? Clean(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
        var now = DateTimeOffset.UtcNow;

        await using (var db = Db.Context())
        {
            if (_editingId is Guid id)
            {
                var env = await db.Environments.FindAsync(id);
                if (env != null)
                {
                    env.Name = name; env.Type = type; env.Region = Clean(RegionBox.Text);
                    env.PpEnvironmentId = Clean(EnvIdBox.Text); env.Url = Clean(UrlBox.Text);
                    env.TenantId = Clean(TenantBox.Text); env.Notes = Clean(NotesBox.Text);
                    env.UpdatedAt = now;
                    await db.SaveChangesAsync();
                }
            }
            else
            {
                db.Environments.Add(new PowerPlatformEnvironment
                {
                    Id = Guid.NewGuid(), Name = name, Type = type, Region = Clean(RegionBox.Text),
                    PpEnvironmentId = Clean(EnvIdBox.Text), Url = Clean(UrlBox.Text),
                    TenantId = Clean(TenantBox.Text), Notes = Clean(NotesBox.Text),
                    CreatedAt = now, UpdatedAt = now,
                });
                await db.SaveChangesAsync();
            }
        }
        Reset();
        await LoadAsync();
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        var row = (EnvRow)((FrameworkElement)sender).DataContext;
        _editingId = row.Id;
        ShowAddCard(true);
        NameBox.Text = row.Name;
        TypeBox.SelectedItem = row.Type;
        RegionBox.Text = row.Region ?? "";
        EnvIdBox.Text = row.PpEnvironmentId ?? "";
        UrlBox.Text = row.Url ?? "";
        TenantBox.Text = row.TenantId ?? "";
        NotesBox.Text = row.Notes ?? "";
        AddBtn.Content = "Save";
        CancelBtn.Visibility = Visibility.Visible;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Reset();

    private void Reset()
    {
        _editingId = null;
        NameBox.Text = ""; RegionBox.Text = ""; EnvIdBox.Text = ""; UrlBox.Text = ""; TenantBox.Text = ""; NotesBox.Text = "";
        TypeBox.SelectedItem = EnvironmentType.Dev;
        AddBtn.Content = "Add";
        CancelBtn.Visibility = Visibility.Collapsed;
        ShowAddCard(false);
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        var row = (EnvRow)((FrameworkElement)sender).DataContext;
        if (MessageBox.Show($"Delete environment \"{row.Name}\"?", "Environments", MessageBoxButton.YesNo)
            != MessageBoxResult.Yes) return;
        await using (var db = Db.Context())
        {
            var env = await db.Environments.FindAsync(row.Id);
            if (env != null) { db.Environments.Remove(env); await db.SaveChangesAsync(); }
        }
        await LoadAsync();
    }

    private void Link_Click(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is string url && !string.IsNullOrWhiteSpace(url))
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
    }

    private async void Checklist_Click(object sender, RoutedEventArgs e)
    {
        var row = (EnvRow)((FrameworkElement)sender).DataContext;
        var dlg = new EnvConfigDialog(row.Id, row.Name) { Owner = Window.GetWindow(this) };
        dlg.ShowDialog();
        await LoadAsync();
    }
}
