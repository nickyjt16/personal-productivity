using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ProductivityHub.Core;
using ProductivityHub.Core.Data;

namespace ProductivityHub.Desktop;

// In-process data access — no server. Each operation uses a short-lived context.
public static class Db
{
    private static DbContextOptions<AppDbContext> Options() =>
        new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(AppPaths.ConnectionString)
            .Options;

    public static AppDbContext Context() => new(Options());

    public static async Task InitAsync()
    {
        DbBackup.CreateStartupBackup(AppPaths.ConnectionString, NullLogger.Instance);
        await using var db = Context();
        await db.Database.EnsureCreatedAsync();
        await SchemaUpdater.ApplyAsync(db);
    }
}
