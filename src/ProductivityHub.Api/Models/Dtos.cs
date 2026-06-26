using ProductivityHub.Api.Data.Entities;

namespace ProductivityHub.Api.Models;

// Lightweight project reference embedded in todo/note/bookmark responses.
public record ProjectRef(Guid Id, string Name, string Color);

// Sets the full set of projects an item belongs to.
public record SetProjectsRequest(List<Guid> ProjectIds);

// Full snapshot of the database used for export/import (entity nav properties
// are [JsonIgnore]d, so these serialize acyclically).
public record BackupData(
    int Version,
    DateTimeOffset ExportedAt,
    List<TodoItem> Todos,
    List<InboxItem> InboxItems,
    List<Bookmark> Bookmarks,
    List<Note> Notes,
    List<JournalEntry> JournalEntries,
    List<PomodoroSession> PomodoroSessions,
    List<Project> Projects,
    List<TodoProject> TodoProjects,
    List<NoteProject> NoteProjects,
    List<BookmarkProject> BookmarkProjects);
