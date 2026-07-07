using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core;
using ProductivityHub.Core.Data;

namespace ProductivityHub.Api.Controllers;

[ApiController]
[Route("api/data")]
public class DataController(AppDbContext db) : ControllerBase
{
    // Full snapshot of every table, for download as a backup file.
    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var backup = new BackupData(
            Version: 1,
            ExportedAt: DateTimeOffset.UtcNow,
            Todos: await db.Todos.AsNoTracking().ToListAsync(ct),
            InboxItems: await db.InboxItems.AsNoTracking().ToListAsync(ct),
            Bookmarks: await db.Bookmarks.AsNoTracking().ToListAsync(ct),
            Notes: await db.Notes.AsNoTracking().ToListAsync(ct),
            JournalEntries: await db.JournalEntries.AsNoTracking().ToListAsync(ct),
            PomodoroSessions: await db.PomodoroSessions.AsNoTracking().ToListAsync(ct),
            Projects: await db.Projects.AsNoTracking().ToListAsync(ct),
            TodoProjects: await db.TodoProjects.AsNoTracking().ToListAsync(ct),
            NoteProjects: await db.NoteProjects.AsNoTracking().ToListAsync(ct),
            BookmarkProjects: await db.BookmarkProjects.AsNoTracking().ToListAsync(ct),
            Secrets: await db.Secrets.AsNoTracking().ToListAsync(ct),
            SecretProjects: await db.SecretProjects.AsNoTracking().ToListAsync(ct),
            Environments: await db.Environments.AsNoTracking().ToListAsync(ct),
            EnvironmentConfigs: await db.EnvironmentConfigs.AsNoTracking().ToListAsync(ct));
        return Ok(backup);
    }

    // Replace ALL current data with the contents of a backup file.
    [HttpPost("import")]
    public async Task<IActionResult> Import([FromBody] BackupData data, CancellationToken ct)
    {
        if (data is null) return BadRequest("No backup data provided.");

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await db.TodoProjects.ExecuteDeleteAsync(ct);
        await db.NoteProjects.ExecuteDeleteAsync(ct);
        await db.BookmarkProjects.ExecuteDeleteAsync(ct);
        await db.PomodoroSessions.ExecuteDeleteAsync(ct);
        await db.Todos.ExecuteDeleteAsync(ct);
        await db.InboxItems.ExecuteDeleteAsync(ct);
        await db.Bookmarks.ExecuteDeleteAsync(ct);
        await db.Notes.ExecuteDeleteAsync(ct);
        await db.JournalEntries.ExecuteDeleteAsync(ct);
        await db.SecretProjects.ExecuteDeleteAsync(ct);
        await db.EnvironmentConfigs.ExecuteDeleteAsync(ct);
        await db.Environments.ExecuteDeleteAsync(ct);
        await db.Projects.ExecuteDeleteAsync(ct);
        await db.Secrets.ExecuteDeleteAsync(ct);

        db.Projects.AddRange(data.Projects ?? []);
        db.Secrets.AddRange(data.Secrets ?? []);
        db.SecretProjects.AddRange(data.SecretProjects ?? []);
        db.Environments.AddRange(data.Environments ?? []);
        db.EnvironmentConfigs.AddRange(data.EnvironmentConfigs ?? []);
        db.Todos.AddRange(data.Todos ?? []);
        db.Notes.AddRange(data.Notes ?? []);
        db.Bookmarks.AddRange(data.Bookmarks ?? []);
        db.InboxItems.AddRange(data.InboxItems ?? []);
        db.JournalEntries.AddRange(data.JournalEntries ?? []);
        db.PomodoroSessions.AddRange(data.PomodoroSessions ?? []);
        db.TodoProjects.AddRange(data.TodoProjects ?? []);
        db.NoteProjects.AddRange(data.NoteProjects ?? []);
        db.BookmarkProjects.AddRange(data.BookmarkProjects ?? []);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return Ok(new
        {
            todos = data.Todos?.Count ?? 0,
            notes = data.Notes?.Count ?? 0,
            bookmarks = data.Bookmarks?.Count ?? 0,
            projects = data.Projects?.Count ?? 0,
        });
    }

    // Deletes every row from every table. Local single-user app, so this is a
    // straightforward reset. Children are cleared before parents to avoid any
    // foreign-key issues.
    [HttpPost("clear")]
    public async Task<IActionResult> ClearAll(CancellationToken ct)
    {
        await db.TodoProjects.ExecuteDeleteAsync(ct);
        await db.NoteProjects.ExecuteDeleteAsync(ct);
        await db.BookmarkProjects.ExecuteDeleteAsync(ct);
        await db.SecretProjects.ExecuteDeleteAsync(ct);
        await db.EnvironmentConfigs.ExecuteDeleteAsync(ct);
        await db.PomodoroSessions.ExecuteDeleteAsync(ct);

        await db.Environments.ExecuteDeleteAsync(ct);
        await db.Todos.ExecuteDeleteAsync(ct);
        await db.InboxItems.ExecuteDeleteAsync(ct);
        await db.Bookmarks.ExecuteDeleteAsync(ct);
        await db.Notes.ExecuteDeleteAsync(ct);
        await db.JournalEntries.ExecuteDeleteAsync(ct);
        await db.Projects.ExecuteDeleteAsync(ct);
        await db.Secrets.ExecuteDeleteAsync(ct);

        return NoContent();
    }
}
