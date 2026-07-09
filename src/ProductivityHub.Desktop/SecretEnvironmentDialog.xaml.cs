using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Desktop;

// Links one secret to any number of environments (checkbox list).
public partial class SecretEnvironmentDialog : Window
{
    private readonly Guid _secretId;

    public SecretEnvironmentDialog(Guid secretId)
    {
        InitializeComponent();
        _secretId = secretId;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        await using var db = Db.Context();
        var envs = await db.Environments.OrderBy(e => e.Type).ThenBy(e => e.Name).ToListAsync();
        var linked = (await db.SecretEnvironments.Where(x => x.SecretId == _secretId)
            .Select(x => x.EnvironmentId).ToListAsync()).ToHashSet();

        List.Children.Clear();
        foreach (var env in envs)
        {
            List.Children.Add(new CheckBox
            {
                Content = $"{env.Name}  ({env.Type})",
                IsChecked = linked.Contains(env.Id),
                Tag = env.Id,
                Margin = new Thickness(0, 4, 0, 4),
            });
        }
        if (envs.Count == 0)
            List.Children.Add(new TextBlock { Text = "No environments yet. Add some on the Environments page." });
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var chosen = List.Children.OfType<CheckBox>()
            .Where(c => c.IsChecked == true)
            .Select(c => (Guid)c.Tag!)
            .ToHashSet();

        await using var db = Db.Context();
        var existing = await db.SecretEnvironments.Where(x => x.SecretId == _secretId).ToListAsync();
        db.SecretEnvironments.RemoveRange(existing.Where(x => !chosen.Contains(x.EnvironmentId)));
        var have = existing.Select(x => x.EnvironmentId).ToHashSet();
        foreach (var eid in chosen.Where(id => !have.Contains(id)))
            db.SecretEnvironments.Add(new SecretEnvironment { SecretId = _secretId, EnvironmentId = eid });
        await db.SaveChangesAsync();

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
