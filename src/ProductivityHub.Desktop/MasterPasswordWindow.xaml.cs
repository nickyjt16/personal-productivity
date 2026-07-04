using System.Windows;
using System.Windows.Input;
using ProductivityHub.Core;

namespace ProductivityHub.Desktop;

// Dual-purpose vault dialog: sets the master password on first run, or unlocks it
// on later runs. On success, Key holds the derived master key.
public partial class MasterPasswordWindow : Window
{
    public byte[]? Key { get; private set; }

    private readonly bool _configured;

    public MasterPasswordWindow(bool configured, string? hint = null)
    {
        InitializeComponent();
        _configured = configured;

        if (configured)
        {
            SetupPanel.Visibility = Visibility.Collapsed;
            UnlockPanel.Visibility = Visibility.Visible;
            if (!string.IsNullOrWhiteSpace(hint))
            {
                HintText.Text = "Hint: " + hint;
                HintText.Visibility = Visibility.Visible;
            }
            Loaded += (_, _) => UnlockPwd.Focus();
            UnlockPwd.KeyDown += (_, e) => { if (e.Key == System.Windows.Input.Key.Enter) Unlock_Click(this, e); };
        }
        else
        {
            Loaded += (_, _) => Pwd1.Focus();
        }
    }

    private async void SetPassword_Click(object sender, RoutedEventArgs e)
    {
        var p1 = Pwd1.Password;
        var p2 = Pwd2.Password;
        if (p1.Length < 4) { ShowSetupError("Password must be at least 4 characters."); return; }
        if (p1 != p2) { ShowSetupError("The passwords don't match."); return; }

        try
        {
            await using var db = Db.Context();
            Key = await VaultService.CreateAsync(db, p1, HintBox.Text);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ShowSetupError("Couldn't set the password: " + ex.Message);
        }
    }

    private async void Unlock_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await using var db = Db.Context();
            var key = await VaultService.UnlockAsync(db, UnlockPwd.Password);
            if (key is null) { ShowUnlockError("Wrong password. Try again."); UnlockPwd.SelectAll(); return; }
            Key = key;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            ShowUnlockError("Couldn't unlock: " + ex.Message);
        }
    }

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ShowSetupError(string msg) { SetupError.Text = msg; SetupError.Visibility = Visibility.Visible; }
    private void ShowUnlockError(string msg) { UnlockError.Text = msg; UnlockError.Visibility = Visibility.Visible; }
}
