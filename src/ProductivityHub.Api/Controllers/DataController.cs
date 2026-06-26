using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Api.Data;

namespace ProductivityHub.Api.Controllers;

[ApiController]
[Route("api/data")]
public class DataController(AppDbContext db) : ControllerBase
{
    // Deletes every row from every table. Local single-user app, so this is a
    // straightforward reset. Children are cleared before parents to avoid any
    // foreign-key issues.
    [HttpPost("clear")]
    public async Task<IActionResult> ClearAll(CancellationToken ct)
    {
        await db.TodoProjects.ExecuteDeleteAsync(ct);
        await db.NoteProjects.ExecuteDeleteAsync(ct);
        await db.BookmarkProjects.ExecuteDeleteAsync(ct);
        await db.PomodoroSessions.ExecuteDeleteAsync(ct);

        await db.Todos.ExecuteDeleteAsync(ct);
        await db.InboxItems.ExecuteDeleteAsync(ct);
        await db.Bookmarks.ExecuteDeleteAsync(ct);
        await db.Notes.ExecuteDeleteAsync(ct);
        await db.JournalEntries.ExecuteDeleteAsync(ct);
        await db.Projects.ExecuteDeleteAsync(ct);

        return NoContent();
    }
}
