using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Desktop.Views;

public partial class SecretsView : UserControl
{
    private Guid? _editingId;

    public SecretsView()
    {
        InitializeComponent();
        NameBox.ToolTip = "Name";
        ClientBox.ToolTip = "Client ID (optional)";
        ExpiresBox.ToolTip = "Expires on";
        ValueBox.ToolTip = "Secret value (optional)";
        NotesBox.ToolTip = "Notes (optional)";
        NotifyBox.ToolTip = "Who to inform when this changes (one per line)";
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        await using var db = Db.Context();
        var secrets = await db.Secrets.OrderBy(s => s.ExpiresOn).ToListAsync();
        ItemsHost.ItemsSource = secrets.Select(s => new SecretRow
        {
            Id = s.Id, Name = s.Name, ClientId = s.ClientId, Value = s.Value,
            ExpiresOn = s.ExpiresOn, Notes = s.Notes, NotifyRaw = s.NotifyList,
        }).ToList();
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (name.Length == 0 || ExpiresBox.SelectedDate is not DateTime dt)
        {
            MessageBox.Show("Name and an expiry date are required.", "Secrets");
            return;
        }
        var expires = DateOnly.FromDateTime(dt);
        var clientId = string.IsNullOrWhiteSpace(ClientBox.Text) ? null : ClientBox.Text.Trim();
        var value = string.IsNullOrWhiteSpace(ValueBox.Text) ? null : ValueBox.Text;
        var notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim();
        var notifyParts = NotifyBox.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var notify = notifyParts.Length == 0 ? null : string.Join("\n", notifyParts);
        var now = DateTimeOffset.UtcNow;

        await using (var db = Db.Context())
        {
            if (_editingId is Guid id)
            {
                var s = await db.Secrets.FindAsync(id);
                if (s != null)
                {
                    s.Name = name; s.ClientId = clientId; s.Value = value; s.ExpiresOn = expires;
                    s.Notes = notes; s.NotifyList = notify; s.UpdatedAt = now;
                    await db.SaveChangesAsync();
                }
            }
            else
            {
                db.Secrets.Add(new Secret
                {
                    Id = Guid.NewGuid(), Name = name, ClientId = clientId, Value = value,
                    ExpiresOn = expires, Notes = notes, NotifyList = notify, CreatedAt = now, UpdatedAt = now,
                });
                await db.SaveChangesAsync();
            }
        }
        Reset();
        await LoadAsync();
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        var row = (SecretRow)((FrameworkElement)sender).DataContext;
        _editingId = row.Id;
        NameBox.Text = row.Name;
        ClientBox.Text = row.ClientId ?? "";
        ValueBox.Text = row.Value ?? "";
        NotesBox.Text = row.Notes ?? "";
        NotifyBox.Text = row.NotifyRaw ?? "";
        ExpiresBox.SelectedDate = row.ExpiresOn.ToDateTime(TimeOnly.MinValue);
        AddBtn.Content = "Save";
        CancelBtn.Visibility = Visibility.Visible;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Reset();

    private void Reset()
    {
        _editingId = null;
        NameBox.Text = ""; ClientBox.Text = ""; ValueBox.Text = ""; NotesBox.Text = ""; NotifyBox.Text = "";
        ExpiresBox.SelectedDate = null;
        AddBtn.Content = "Add";
        CancelBtn.Visibility = Visibility.Collapsed;
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        var row = (SecretRow)((FrameworkElement)sender).DataContext;
        await using (var db = Db.Context())
        {
            var s = await db.Secrets.FindAsync(row.Id);
            if (s != null) { db.Secrets.Remove(s); await db.SaveChangesAsync(); }
        }
        await LoadAsync();
    }
}
