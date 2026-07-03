using System.Text.Json.Serialization;

namespace ProductivityHub.Core.Data.Entities;

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
    [JsonIgnore] public List<TodoProject> ProjectLinks { get; set; } = [];
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
    [JsonIgnore] public List<BookmarkProject> ProjectLinks { get; set; } = [];
}

public class Note
{
    public Guid Id { get; set; }
    public string? Title { get; set; }
    public string Body { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    [JsonIgnore] public List<NoteProject> ProjectLinks { get; set; } = [];
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
    [JsonIgnore] public TodoItem? TodoItem { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public int DurationMinutes { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public PomodoroKind Kind { get; set; } = PomodoroKind.Focus;
}

// A client secret / API key to keep an eye on before it expires.
public class Secret
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? ClientId { get; set; }
    public string? Value { get; set; }
    public DateOnly ExpiresOn { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public enum ProjectStatus
{
    New,
    Active,
    Complete,
    Archived
}

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Color { get; set; } = "#0d6efd";
    public ProjectStatus Status { get; set; } = ProjectStatus.New;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

// Join entities — a todo/note/bookmark can belong to many projects and vice versa.
// Navigation properties are nullable so the API's model validation doesn't treat
// them as required when binding a backup for import (they're [JsonIgnore]d anyway).
// The relationships themselves are required, configured in AppDbContext via the
// non-nullable FK columns.
public class TodoProject
{
    public Guid TodoItemId { get; set; }
    [JsonIgnore] public TodoItem? TodoItem { get; set; }
    public Guid ProjectId { get; set; }
    [JsonIgnore] public Project? Project { get; set; }
}

public class NoteProject
{
    public Guid NoteId { get; set; }
    [JsonIgnore] public Note? Note { get; set; }
    public Guid ProjectId { get; set; }
    [JsonIgnore] public Project? Project { get; set; }
}

public class BookmarkProject
{
    public Guid BookmarkId { get; set; }
    [JsonIgnore] public Bookmark? Bookmark { get; set; }
    public Guid ProjectId { get; set; }
    [JsonIgnore] public Project? Project { get; set; }
}
