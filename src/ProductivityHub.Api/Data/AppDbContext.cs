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
    }
}
