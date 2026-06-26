namespace ProductivityHub.Api.Data.Entities;

public enum Priority
{
    Low,
    Medium,
    High
}

public enum PomodoroKind
{
    Focus,
    ShortBreak,
    LongBreak
}

public class TodoItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string? Notes { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    public bool IsDone { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}

public class InboxItem
{
    public Guid Id { get; set; }
    public string Text { get; set; } = "";
    public bool IsProcessed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}

public class Bookmark
{
    public Guid Id { get; set; }
    public string Url { get; set; } = "";
    public string? Title { get; set; }
    public string? Notes { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadAt { get; set; }
}

public class Note
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string Body { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class JournalEntry
{
    public Guid Id { get; set; }
    public DateOnly EntryDate { get; set; }
    public string Body { get; set; } = "";
    public string? Mood { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class PomodoroSession
{
    public Guid Id { get; set; }
    public Guid? TodoItemId { get; set; }
    public TodoItem? TodoItem { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public int DurationMinutes { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public PomodoroKind Kind { get; set; } = PomodoroKind.Focus;
}
