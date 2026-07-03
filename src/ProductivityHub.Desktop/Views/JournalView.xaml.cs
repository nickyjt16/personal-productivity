using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Desktop.Views;

public partial class JournalView : UserControl
{
    public JournalView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            DateBox.SelectedDate = DateTime.Today; // triggers Date_Changed -> LoadEntry
            await LoadRecentAsync();
        };
    }

    private DateOnly CurrentDate =>
        DateOnly.FromDateTime(DateBox.SelectedDate ?? DateTime.Today);

    private async void Date_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (DateBox.SelectedDate is null) return;
        await LoadEntryAsync();
    }

    private async Task LoadEntryAsync()
    {
        var date = CurrentDate;
        await using var db = Db.Context();
        var entry = await db.JournalEntries.FirstOrDefaultAsync(j => j.EntryDate == date);
        BodyBox.Text = entry?.Body ?? "";
        MoodBox.Text = entry?.Mood ?? "";
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        var date = CurrentDate;
        var mood = string.IsNullOrWhiteSpace(MoodBox.Text) ? null : MoodBox.Text.Trim();
        var now = DateTimeOffset.UtcNow;

        await using (var db = Db.Context())
        {
            var entry = await db.JournalEntries.FirstOrDefaultAsync(j => j.EntryDate == date);
            if (entry is null)
            {
                db.JournalEntries.Add(new JournalEntry
                {
                    Id = Guid.NewGuid(), EntryDate = date, Body = BodyBox.Text, Mood = mood,
                    CreatedAt = now, UpdatedAt = now,
                });
            }
            else
            {
                entry.Body = BodyBox.Text; entry.Mood = mood; entry.UpdatedAt = now;
            }
            await db.SaveChangesAsync();
        }
        await LoadRecentAsync();
    }

    private async Task LoadRecentAsync()
    {
        await using var db = Db.Context();
        var entries = await db.JournalEntries.OrderByDescending(j => j.EntryDate).Take(30).ToListAsync();
        RecentHost.ItemsSource = entries.Select(j => new JournalRow
        {
            Date = j.EntryDate,
            Title = j.EntryDate.ToString("d"),
            Preview = j.Body.Length > 40 ? j.Body[..40] : j.Body,
        }).ToList();
    }

    private void Recent_Click(object sender, RoutedEventArgs e)
    {
        var row = (JournalRow)((FrameworkElement)sender).DataContext;
        DateBox.SelectedDate = row.Date.ToDateTime(TimeOnly.MinValue);
    }
}
