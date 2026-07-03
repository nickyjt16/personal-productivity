using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using WinForms = System.Windows.Forms;

namespace ProductivityHub.Desktop;

// Owns the notification-area icon, the global quick-capture hotkey (Ctrl+Alt+N),
// and minimise-to-tray behaviour. The app runs with ShutdownMode=OnExplicitShutdown,
// so closing the main window just hides it here — only "Quit" ends the process.
public sealed class TrayManager : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HOTKEY_ID = 0xB001;
    private const uint MOD_ALT = 0x0001, MOD_CONTROL = 0x0002, MOD_NOREPEAT = 0x4000;
    private const uint VK_N = 0x4E;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly MainWindow _main;
    private readonly WinForms.NotifyIcon _icon;
    private HwndSource? _source;
    private bool _hotkeyRegistered;

    public TrayManager(MainWindow main)
    {
        _main = main;

        _icon = new WinForms.NotifyIcon
        {
            Text = "Productivity Hub",
            Icon = LoadIcon(),
            Visible = true,
        };
        _icon.DoubleClick += (_, _) => ShowMain();

        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => ShowMain());
        menu.Items.Add("Quick capture  (Ctrl+Alt+N)", null, (_, _) => ShowQuickAdd());
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => ExitApp());
        _icon.ContextMenuStrip = menu;

        // The main window already has an HWND by the time we're constructed (it's shown
        // in OnStartup); hook its message loop so the hotkey works even while hidden.
        var handle = new WindowInteropHelper(_main).EnsureHandle();
        _source = HwndSource.FromHwnd(handle);
        _source?.AddHook(WndProc);
        _hotkeyRegistered = RegisterHotKey(handle, HOTKEY_ID, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, VK_N);

        // Closing the window hides it to the tray instead of exiting.
        _main.Closing += (_, e) =>
        {
            if (_disposed) return;      // real shutdown in progress — let it close
            e.Cancel = true;
            HideMain();
        };
        _main.StateChanged += (_, _) =>
        {
            if (_main.WindowState == WindowState.Minimized) HideMain();
        };
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            ShowQuickAdd();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void ShowMain()
    {
        _main.Show();
        _main.WindowState = WindowState.Normal;
        _main.Activate();
        _main.Topmost = true;   // nudge to front, then release
        _main.Topmost = false;
    }

    private bool _toldAboutTray;
    private void HideMain()
    {
        _main.Hide();
        if (!_toldAboutTray)
        {
            _toldAboutTray = true;
            _icon.ShowBalloonTip(3000, "Still running",
                "Productivity Hub is in the tray. Press Ctrl+Alt+N to capture a note anytime.",
                WinForms.ToolTipIcon.Info);
        }
    }

    // Tear down the tray first (so the window's Closing handler stops cancelling),
    // then end the process.
    public void ExitApp()
    {
        Dispose();
        Application.Current.Shutdown();
    }

    private void ShowQuickAdd()
    {
        // Reuse an open popup rather than stacking several.
        foreach (var w in Application.Current.Windows.OfType<QuickAddWindow>())
        {
            w.Activate();
            return;
        }
        var quick = new QuickAddWindow();
        quick.Show();
        quick.Activate();
    }

    private static Icon LoadIcon()
    {
        try
        {
            var path = Environment.ProcessPath;
            if (path != null)
            {
                var extracted = Icon.ExtractAssociatedIcon(path);
                if (extracted != null) return extracted;
            }
        }
        catch { /* fall through to a stock icon */ }
        return SystemIcons.Application;
    }

    private bool _disposed;
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_hotkeyRegistered && _source != null)
            UnregisterHotKey(_source.Handle, HOTKEY_ID);
        _source?.RemoveHook(WndProc);
        _icon.Visible = false;
        _icon.Dispose();
    }
}
