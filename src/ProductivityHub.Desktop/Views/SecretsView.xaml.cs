using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core;
using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Desktop.Views;

public partial class SecretsView : UserControl
{
    private Guid? _editingId;
    private bool _configured;
    // Projects chosen in the add form, applied when a new secret is created.
    private HashSet<Guid> _pendingProjectIds = [];

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
        List<Secret> secrets;
        await using (var db = Db.Context())
        {
            secrets = await db.Secrets
                .Include(s => s.ProjectLinks).ThenInclude(l => l.Project)
                .Include(s => s.EnvironmentLinks).ThenInclude(l => l.Environment)
                .OrderBy(s => s.ExpiresOn).ToListAsync();
            _configured = await VaultService.IsConfiguredAsync(db);
        }
        ItemsHost.ItemsSource = secrets.Select(s =>
        {
            var (shown, locked) = RevealValue(s.Value);
            return new SecretRow
            {
                Id = s.Id, Name = s.Name, ClientId = s.ClientId, Value = shown, Locked = locked,
                ExpiresOn = s.ExpiresOn, Notes = s.Notes, NotifyRaw = s.NotifyList, Link = s.Link,
                ProjectTags = s.ProjectLinks.Count == 0 ? "" : "🏷 " + string.Join(", ", s.ProjectLinks.Select(l => l.Project!.Name)),
                EnvTags = s.EnvironmentLinks.Count == 0 ? "" : "🌐 " + string.Join(", ", s.EnvironmentLinks.Select(l => l.Environment!.Name)),
            };
        }).ToList();
        UpdateLockUi();
    }

    // Turns the stored value into (plaintext-to-show, isLocked). Encrypted values
    // are only revealed when the vault is unlocked.
    private static (string? shown, bool locked) RevealValue(string? stored)
    {
        if (string.IsNullOrEmpty(stored)) return (null, false);
        if (!SecretCrypto.IsEncrypted(stored)) return (stored, false);   // legacy plaintext
        if (!App.VaultUnlocked) return (null, true);
        try { return (SecretCrypto.Decrypt(stored, App.VaultKey!), false); }
        catch { return (null, true); }
    }

    private void UpdateLockUi()
    {
        if (App.VaultUnlocked)
        {
            LockStatusText.Text = "🔓 Secrets unlocked — values are visible.";
            LockToggleBtn.Content = "Lock";
        }
        else if (_configured)
        {
            LockStatusText.Text = "🔒 Secret values are locked.";
            LockToggleBtn.Content = "Unlock";
        }
        else
        {
            LockStatusText.Text = "Set a master password before adding secrets.";
            LockToggleBtn.Content = "Set master password";
        }
        // Adding a secret requires a master password to be set first.
        AddToggleBtn.IsEnabled = _configured;
        AddToggleBtn.ToolTip = _configured ? null : "Set a master password first";
    }

    private async void LockToggle_Click(object sender, RoutedEventArgs e)
    {
        if (App.VaultUnlocked)
        {
            App.LockVault();
            // Locking must hide everything immediately, including a value sitting
            // in the add/edit form — otherwise the open secret stays exposed.
            Reset();
        }
        else
        {
            await App.SetupOrUnlockVaultAsync(Window.GetWindow(this));
        }
        await LoadAsync();
    }

    // Prepares a submitted value for storage: encrypts when unlocked, blocks when a
    // vault exists but is locked, or stores plaintext when no vault is set up yet.
    private async Task<(bool ok, string? stored)> PrepareValueAsync(string? submitted)
    {
        if (string.IsNullOrEmpty(submitted)) return (true, null);
        if (App.VaultUnlocked) return (true, SecretCrypto.Encrypt(submitted, App.VaultKey!));
        await using var db = Db.Context();
        if (await VaultService.IsConfiguredAsync(db))
        {
            MessageBox.Show("Unlock the vault (button at the top) before saving a secret value.", "Secrets");
            return (false, null);
        }
        return (true, submitted);
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (name.Length == 0 || ExpiresBox.SelectedDate is not DateTime dt)
        {
            MessageBox.Show("Name and an expiry date are required.", "Secrets");
            return;
        }
        if (_editingId is null && !_configured)
        {
            MessageBox.Show("Set a master password first (button at the top) before adding secrets.", "Secrets");
            return;
        }
        var expires = DateOnly.FromDateTime(dt);
        var clientId = string.IsNullOrWhiteSpace(ClientBox.Text) ? null : ClientBox.Text.Trim();
        var submittedValue = string.IsNullOrWhiteSpace(ValueBox.Text) ? null : ValueBox.Text;
        var notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim();
        var notifyParts = NotifyBox.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var notify = notifyParts.Length == 0 ? null : string.Join("\n", notifyParts);
        var link = string.IsNullOrWhiteSpace(LinkBox.Text) ? null : LinkBox.Text.Trim();
        var now = DateTimeOffset.UtcNow;

        var (ok, storedValue) = await PrepareValueAsync(submittedValue);
        if (!ok) return;

        await using (var db = Db.Context())
        {
            if (_editingId is Guid id)
            {
                var s = await db.Secrets.FindAsync(id);
                if (s != null)
                {
                    s.Name = name; s.ClientId = clientId; s.ExpiresOn = expires;
                    s.Notes = notes; s.NotifyList = notify; s.Link = link; s.UpdatedAt = now;
                    // Only replace the value when a new one was entered, so editing
                    // other fields while locked can't wipe the stored secret.
                    if (submittedValue is not null) s.Value = storedValue;
                    await db.SaveChangesAsync();
                }
            }
            else
            {
                var newId = Guid.NewGuid();
                db.Secrets.Add(new Secret
                {
                    Id = newId, Name = name, ClientId = clientId, Value = storedValue,
                    ExpiresOn = expires, Notes = notes, NotifyList = notify, Link = link,
                    CreatedAt = now, UpdatedAt = now,
                });
                foreach (var pid in _pendingProjectIds)
                    db.SecretProjects.Add(new SecretProject { SecretId = newId, ProjectId = pid });
                await db.SaveChangesAsync();
            }
        }
        Reset();
        await LoadAsync();
    }

    private void ToggleAdd_Click(object sender, RoutedEventArgs e)
    {
        if (AddCard.Visibility == Visibility.Visible) Reset();
        else ShowAddCard(true);
    }

    private void ShowAddCard(bool show)
    {
        AddCard.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        AddToggleBtn.Content = show ? "Close" : "＋ Add secret";
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        var row = (SecretRow)((FrameworkElement)sender).DataContext;
        _editingId = row.Id;
        _pendingProjectIds = [];
        UpdatePickBtn();
        ShowAddCard(true);
        NameBox.Text = row.Name;
        ClientBox.Text = row.ClientId ?? "";
        ValueBox.Text = row.Value ?? "";
        NotesBox.Text = row.Notes ?? "";
        NotifyBox.Text = row.NotifyRaw ?? "";
        LinkBox.Text = row.Link ?? "";
        ExpiresBox.SelectedDate = row.ExpiresOn.ToDateTime(TimeOnly.MinValue);
        AddBtn.Content = "Save";
        CancelBtn.Visibility = Visibility.Visible;
    }

    private async void Projects_Click(object sender, RoutedEventArgs e)
    {
        var row = (SecretRow)((FrameworkElement)sender).DataContext;
        var dlg = new ProjectAssignDialog("secret", row.Id) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) await LoadAsync();
    }

    private async void Environments_Click(object sender, RoutedEventArgs e)
    {
        var row = (SecretRow)((FrameworkElement)sender).DataContext;
        var dlg = new SecretEnvironmentDialog(row.Id) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true) await LoadAsync();
    }

    // The "🏷 Projects" button on the add/edit form. When editing an existing
    // secret it assigns directly; when adding a new one it just collects the choice.
    private async void PickProjects_Click(object sender, RoutedEventArgs e)
    {
        if (_editingId is Guid id)
        {
            var dlg = new ProjectAssignDialog("secret", id) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true) await LoadAsync();
            return;
        }
        var pick = new ProjectAssignDialog(_pendingProjectIds) { Owner = Window.GetWindow(this) };
        if (pick.ShowDialog() == true)
        {
            _pendingProjectIds = pick.SelectedIds.ToHashSet();
            UpdatePickBtn();
        }
    }

    private void UpdatePickBtn() =>
        PickProjectsBtn.Content = _pendingProjectIds.Count == 0 ? "🏷 Projects" : $"🏷 Projects ({_pendingProjectIds.Count})";

    private void Cancel_Click(object sender, RoutedEventArgs e) => Reset();

    private void Reset()
    {
        _editingId = null;
        _pendingProjectIds = [];
        UpdatePickBtn();
        NameBox.Text = ""; ClientBox.Text = ""; ValueBox.Text = ""; NotesBox.Text = ""; NotifyBox.Text = ""; LinkBox.Text = "";
        ExpiresBox.SelectedDate = null;
        AddBtn.Content = "Add";
        CancelBtn.Visibility = Visibility.Collapsed;
        ShowAddCard(false);
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
