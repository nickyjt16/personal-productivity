using System.Text.Json.Serialization;

namespace ProductivityHub.Core.Data.Entities;

public enum Priority
{
    Low,
    Medium,
    High
}

public enum RecurUnit
{
    None,
    Day,
    Week,
    Month
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
    // Recurrence: when set, "completing" advances the due date instead of closing.
    public RecurUnit RecurUnit { get; set; } = RecurUnit.None;
    public int RecurInterval { get; set; }
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
    // Archived notes are kept for reference but hidden from the default view.
    public bool IsArchived { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
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
    // People/teams to inform when this secret changes — stored newline-separated.
    public string? NotifyList { get; set; }
    // Optional link to where the secret lives (portal page, docs, etc.).
    public string? Link { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    [JsonIgnore] public List<SecretProject> ProjectLinks { get; set; } = [];
    [JsonIgnore] public List<SecretEnvironment> EnvironmentLinks { get; set; } = [];
}

public enum EnvironmentType
{
    Dev,
    Test,
    UAT,
    Prod,
    Default,
    Sandbox,
    Other
}

// A Power Platform / Dataverse environment's reference details.
public class PowerPlatformEnvironment
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public EnvironmentType Type { get; set; } = EnvironmentType.Dev;
    // The Power Platform environment ID (GUID), as shown in the admin centre.
    public string? PpEnvironmentId { get; set; }
    // The org / environment URL, e.g. https://contoso.crm11.dynamics.com
    public string? Url { get; set; }
    public string? TenantId { get; set; }
    public string? Region { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<EnvironmentConfig> Configs { get; set; } = [];
    [JsonIgnore] public List<SecretEnvironment> SecretLinks { get; set; } = [];
}

// Join entity — a secret (e.g. an app-registration client secret) can apply to
// many environments and an environment can hold many secrets.
public class SecretEnvironment
{
    public Guid SecretId { get; set; }
    [JsonIgnore] public Secret? Secret { get; set; }
    public Guid EnvironmentId { get; set; }
    [JsonIgnore] public PowerPlatformEnvironment? Environment { get; set; }
}

public enum EnvConfigKind
{
    ConnectionReference,
    EnvironmentVariable
}

// A value to set in a specific environment after importing a solution —
// a connection reference's connection or an environment variable's value.
public class EnvironmentConfig
{
    public Guid Id { get; set; }
    public Guid EnvironmentId { get; set; }
    [JsonIgnore] public PowerPlatformEnvironment? Environment { get; set; }
    public EnvConfigKind Kind { get; set; }
    public string Name { get; set; } = "";
    public string? Value { get; set; }
    public string? Solution { get; set; }
    public bool IsSet { get; set; }
    public string? Notes { get; set; }
}

// Single-row table holding the master-password vault parameters. The password
// itself is never stored — only the PBKDF2 salt/iterations and a verifier token
// (a known string encrypted with the derived key) used to check the password.
public class VaultConfig
{
    public string Id { get; set; } = "vault";
    public string Salt { get; set; } = "";        // base64
    public int Iterations { get; set; }
    public string Verifier { get; set; } = "";     // enc:v1: envelope of a known token
    public string? Hint { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
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

public class SecretProject
{
    public Guid SecretId { get; set; }
    [JsonIgnore] public Secret? Secret { get; set; }
    public Guid ProjectId { get; set; }
    [JsonIgnore] public Project? Project { get; set; }
}
