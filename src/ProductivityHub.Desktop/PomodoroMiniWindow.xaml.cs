using System.Windows;

namespace ProductivityHub.Desktop;

public partial class PomodoroMiniWindow : Window
{
    public event Action? PauseRequested;
    public event Action? ResetRequested;
    public event Action? ReopenRequested;

    public PomodoroMiniWindow()
    {
        InitializeComponent();
    }

    public void SetTime(string text) => TimeText.Text = text;
    public void SetRunning(bool running) => PauseBtn.Content = running ? "Pause" : "Resume";

    private void Pause_Click(object sender, RoutedEventArgs e) => PauseRequested?.Invoke();
    private void Reset_Click(object sender, RoutedEventArgs e) => ResetRequested?.Invoke();
    private void Reopen_Click(object sender, RoutedEventArgs e) => ReopenRequested?.Invoke();
}
