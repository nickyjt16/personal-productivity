using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data;
using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Api.Controllers;

[ApiController]
[Route("api/pomodoro")]
public class PomodoroController(AppDbContext db) : ControllerBase
{
    public record SessionDto(Guid Id, Guid? TodoItemId, string? TodoTitle, DateTimeOffset StartedAt,
        int DurationMinutes, DateTimeOffset? CompletedAt, PomodoroKind Kind);

    public record StartRequest(Guid? TodoItemId, int DurationMinutes, PomodoroKind? Kind);

    // Sessions started today, most recent first.
    [HttpGet]
    public async Task<IActionResult> Today(CancellationToken ct)
    {
        var startOfDay = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);

        var items = await db.PomodoroSessions
            .Where(s => s.StartedAt >= startOfDay)
            .OrderByDescending(s => s.StartedAt)
            .Select(s => new SessionDto(s.Id, s.TodoItemId, s.TodoItem != null ? s.TodoItem.Title : null,
                s.StartedAt, s.DurationMinutes, s.CompletedAt, s.Kind))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Start(StartRequest req, CancellationToken ct)
    {
        if (req.DurationMinutes <= 0)
            return BadRequest("DurationMinutes must be positive.");

        if (req.TodoItemId is not null && !await db.Todos.AnyAsync(t => t.Id == req.TodoItemId, ct))
            return BadRequest("Linked todo does not exist.");

        var session = new PomodoroSession
        {
            Id = Guid.NewGuid(),
            TodoItemId = req.TodoItemId,
            DurationMinutes = req.DurationMinutes,
            Kind = req.Kind ?? PomodoroKind.Focus,
            StartedAt = DateTimeOffset.UtcNow,
        };
        db.PomodoroSessions.Add(session);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Today), new { id = session.Id },
            new SessionDto(session.Id, session.TodoItemId, null, session.StartedAt,
                session.DurationMinutes, session.CompletedAt, session.Kind));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        var session = await db.PomodoroSessions.FindAsync([id], ct);
        if (session is null) return NotFound();

        session.CompletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(new SessionDto(session.Id, session.TodoItemId, null, session.StartedAt,
            session.DurationMinutes, session.CompletedAt, session.Kind));
    }
}
