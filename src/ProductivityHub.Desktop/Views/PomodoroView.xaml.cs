using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Desktop.Views;

public partial class PomodoroView : UserControl
{
    private const int FocusMinutes = 25;
    private const int BreakMinutes = 5;

    private readonly DispatcherTimer _timer;
    private int _remaining = FocusMinutes * 60;
    private bool _running;
    private Guid? _sessionId;
    private PomodoroMiniWindow? _mini;

    public PomodoroView()
    {
        InitializeComponent();
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += Tick;
        Loaded += async (_, _) => { await LoadTasksAsync(); await UpdateSessionsAsync(); UpdateDisplay(); };
        Unloaded += (_, _) => { _timer.Stop(); _mini?.Close(); };
    }

    private bool IsBreak => BreakRadio.IsChecked == true;
    private int TotalSeconds => (IsBreak ? BreakMinutes : FocusMinutes) * 60;

    private async Task LoadTasksAsync()
    {
        await using var db = Db.Context();
        var todos = await db.Todos.Where(t => !t.IsDone).OrderByDescending(t => t.Priority).ToListAsync();
        var items = new List<LinkRow> { new() { Id = Guid.Empty, Label = "(no task linked)" } };
        items.AddRange(todos.Select(t => new LinkRow { Id = t.Id, Label = t.Title }));
        TaskCombo.ItemsSource = items;
        TaskCombo.SelectedIndex = 0;
    }

    private void Mode_Changed(object sender, RoutedEventArgs e)
    {
        // The RadioButton's initial IsChecked fires this during InitializeComponent,
        // before the other named controls exist — ignore until the view is ready.
        if (!IsLoaded || _running) return;
        _remaining = TotalSeconds;
        UpdateDisplay();
    }

    private void Tick(object? sender, EventArgs e)
    {
        _remaining--;
        UpdateDisplay();
        if (_remaining > 0) return;

        // Finished.
        _timer.Stop();
        _running = false;
        _ = CompleteSessionAsync();
        _mini?.Close();
        _mini = null;

        var main = Window.GetWindow(this);
        main?.Activate();
        try { SystemSounds.Exclamation.Play(); } catch { }
        StartBtn.Content = "Start";
        MessageBox.Show(IsBreak ? "Break over — back to it!" : "Nice work. Time for a break.",
            "🍅 Pomodoro complete");
        _remaining = TotalSeconds;
        UpdateDisplay();
    }

    private async void StartPause_Click(object sender, RoutedEventArgs e)
    {
        if (_running)
        {
            _running = false;
            _timer.Stop();
            StartBtn.Content = "Resume";
            _mini?.SetRunning(false);
            return;
        }

        // Starting a fresh session (log it) if the clock is at full.
        if (_remaining == TotalSeconds)
            await StartSessionAsync();

        _running = true;
        _timer.Start();
        StartBtn.Content = "Pause";
        _mini?.SetRunning(true);
    }

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        _running = false;
        _timer.Stop();
        _sessionId = null;
        _remaining = TotalSeconds;
        StartBtn.Content = "Start";
        UpdateDisplay();
        _mini?.SetRunning(false);
    }

    private void PopOut_Click(object sender, RoutedEventArgs e)
    {
        if (_mini != null) { _mini.Activate(); return; }
        _mini = new PomodoroMiniWindow { Owner = Window.GetWindow(this) };
        _mini.PauseRequested += () => StartPause_Click(this, new RoutedEventArgs());
        _mini.ResetRequested += () => Reset_Click(this, new RoutedEventArgs());
        _mini.ReopenRequested += () => { Window.GetWindow(this)?.Activate(); _mini?.Close(); _mini = null; };
        _mini.Closed += (_, _) => _mini = null;
        _mini.SetRunning(_running);
        _mini.SetTime(Format(_remaining));
        _mini.Show();
    }

    private async Task StartSessionAsync()
    {
        var todoId = TaskCombo.SelectedValue is Guid g && g != Guid.Empty ? (Guid?)g : null;
        var session = new PomodoroSession
        {
            Id = Guid.NewGuid(),
            TodoItemId = IsBreak ? null : todoId,
            DurationMinutes = IsBreak ? BreakMinutes : FocusMinutes,
            Kind = IsBreak ? PomodoroKind.ShortBreak : PomodoroKind.Focus,
            StartedAt = DateTimeOffset.UtcNow,
        };
        _sessionId = session.Id;
        await using var db = Db.Context();
        db.PomodoroSessions.Add(session);
        await db.SaveChangesAsync();
    }

    private async Task CompleteSessionAsync()
    {
        if (_sessionId is not Guid id) return;
        await using (var db = Db.Context())
        {
            var s = await db.PomodoroSessions.FindAsync(id);
            if (s != null) { s.CompletedAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(); }
        }
        _sessionId = null;
        await UpdateSessionsAsync();
    }

    private async Task UpdateSessionsAsync()
    {
        var startOfDay = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        await using var db = Db.Context();
        var count = await db.PomodoroSessions.CountAsync(
            s => s.StartedAt >= startOfDay && s.Kind == PomodoroKind.Focus && s.CompletedAt != null);
        SessionsText.Text = $"{count} focus session{(count == 1 ? "" : "s")} completed today";
    }

    private void UpdateDisplay()
    {
        var text = Format(_remaining);
        TimeText.Text = text;
        _mini?.SetTime(text);
    }

    private static string Format(int seconds) => $"{seconds / 60:00}:{seconds % 60:00}";
}
