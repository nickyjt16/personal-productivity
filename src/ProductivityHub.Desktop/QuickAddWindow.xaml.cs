using System.Windows;
using System.Windows.Input;
using ProductivityHub.Core;
using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Desktop;

// A tiny always-on-top popup for jotting something straight into the Inbox,
// summoned by the global hotkey or the tray menu.
public partial class QuickAddWindow : Window
{
    public QuickAddWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => { TextBox.Focus(); };
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private async void Save_Click(object sender, RoutedEventArgs e) => await SaveAsync();

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape) { Close(); return; }
        if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            e.Handled = true;
            _ = SaveAsync();
        }
    }

    private async Task SaveAsync()
    {
        var text = TextBox.Text.Trim();
        if (text.Length == 0) { Close(); return; }

        SaveBtn.IsEnabled = false;
        try
        {
            await using var db = Db.Context();
            db.InboxItems.Add(new InboxItem { Id = Guid.NewGuid(), Text = text, CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }
        finally
        {
            Close();
        }
    }
}
