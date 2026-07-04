using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data;
using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Api.Controllers;

[ApiController]
[Route("api/inbox")]
public class InboxController(AppDbContext db) : ControllerBase
{
    public record InboxDto(Guid Id, string Text, bool IsProcessed,
        DateTimeOffset CreatedAt, DateTimeOffset? ProcessedAt);

    public record CaptureRequest(string Text);

    private static InboxDto ToDto(InboxItem i) =>
        new(i.Id, i.Text, i.IsProcessed, i.CreatedAt, i.ProcessedAt);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? processed, CancellationToken ct)
    {
        var query = db.InboxItems.AsQueryable();
        if (processed is not null)
            query = query.Where(i => i.IsProcessed == processed);

        var items = await query
            .OrderBy(i => i.IsProcessed)
            .ThenByDescending(i => i.CreatedAt)
            .Select(i => ToDto(i))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Capture(CaptureRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Text))
            return BadRequest("Text is required.");

        var item = new InboxItem
        {
            Id = Guid.NewGuid(),
            Text = req.Text.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.InboxItems.Add(item);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { id = item.Id }, ToDto(item));
    }

    [HttpPost("{id:guid}/process")]
    public async Task<IActionResult> MarkProcessed(Guid id, CancellationToken ct)
    {
        var item = await db.InboxItems.FindAsync([id], ct);
        if (item is null) return NotFound();

        item.IsProcessed = true;
        item.ProcessedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(ToDto(item));
    }

    // Triage an inbox item into a todo, marking the inbox item processed.
    [HttpPost("{id:guid}/to-todo")]
    public async Task<IActionResult> ConvertToTodo(Guid id, CancellationToken ct)
    {
        var item = await db.InboxItems.FindAsync([id], ct);
        if (item is null) return NotFound();

        var todo = new TodoItem
        {
            Id = Guid.NewGuid(),
            Title = item.Text,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Todos.Add(todo);

        item.IsProcessed = true;
        item.ProcessedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(new { todoId = todo.Id });
    }

    // Triage an inbox item into a note for long-term storage, marking it processed.
    [HttpPost("{id:guid}/to-note")]
    public async Task<IActionResult> ConvertToNote(Guid id, CancellationToken ct)
    {
        var item = await db.InboxItems.FindAsync([id], ct);
        if (item is null) return NotFound();

        var now = DateTimeOffset.UtcNow;
        var note = new Note
        {
            Id = Guid.NewGuid(),
            Body = item.Text,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Notes.Add(note);

        item.IsProcessed = true;
        item.ProcessedAt = now;

        await db.SaveChangesAsync(ct);
        return Ok(new { noteId = note.Id });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var item = await db.InboxItems.FindAsync([id], ct);
        if (item is null) return NotFound();

        db.InboxItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
