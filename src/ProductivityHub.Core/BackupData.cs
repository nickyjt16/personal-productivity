using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Core;

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
