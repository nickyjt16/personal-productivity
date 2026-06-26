using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ProductivityHub.Api.Data.Entities;

namespace ProductivityHub.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> Todos => Set<TodoItem>();
    public DbSet<InboxItem> InboxItems => Set<InboxItem>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<PomodoroSession> PomodoroSessions => Set<PomodoroSession>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TodoProject> TodoProjects => Set<TodoProject>();
    public DbSet<NoteProject> NoteProjects => Set<NoteProject>();
    public DbSet<BookmarkProject> BookmarkProjects => Set<BookmarkProject>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SQLite cannot ORDER BY / compare DateTimeOffset natively. Store every
        // DateTimeOffset as its binary form, which round-trips exactly and — since
        // all timestamps are written in UTC — sorts chronologically.
        var converter = new DateTimeOffsetToBinaryConverter();
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset) ||
                    property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetValueConverter(converter);
                }
            }
        }

        // One journal entry per calendar day.
        modelBuilder.Entity<JournalEntry>()
            .HasIndex(j => j.EntryDate)
            .IsUnique();

        // A Pomodoro session may optionally be attached to a todo; clearing the
        // todo should not delete the logged session.
        modelBuilder.Entity<PomodoroSession>()
            .HasOne(p => p.TodoItem)
            .WithMany()
            .HasForeignKey(p => p.TodoItemId)
            .OnDelete(DeleteBehavior.SetNull);

        // Many-to-many between projects and todos/notes/bookmarks via explicit join
        // entities. Deleting either side removes the link row (cascade).
        modelBuilder.Entity<TodoProject>(e =>
        {
            e.HasKey(x => new { x.TodoItemId, x.ProjectId });
            e.HasOne(x => x.TodoItem).WithMany(t => t.ProjectLinks)
                .HasForeignKey(x => x.TodoItemId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Project).WithMany()
                .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<NoteProject>(e =>
        {
            e.HasKey(x => new { x.NoteId, x.ProjectId });
            e.HasOne(x => x.Note).WithMany(n => n.ProjectLinks)
                .HasForeignKey(x => x.NoteId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Project).WithMany()
                .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<BookmarkProject>(e =>
        {
            e.HasKey(x => new { x.BookmarkId, x.ProjectId });
            e.HasOne(x => x.Bookmark).WithMany(b => b.ProjectLinks)
                .HasForeignKey(x => x.BookmarkId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Project).WithMany()
                .HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
