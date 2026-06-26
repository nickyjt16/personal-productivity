using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Api.Data;
using ProductivityHub.Api.Data.Entities;

namespace ProductivityHub.Api.Controllers;

[ApiController]
[Route("api/notes")]
public class NotesController(AppDbContext db) : ControllerBase
{
    public record NoteDto(Guid Id, string? Title, string Body,
        DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

    public record SaveNoteRequest(string? Title, string Body);

    private static NoteDto ToDto(Note n) =>
        new(n.Id, n.Title, n.Body, n.CreatedAt, n.UpdatedAt);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var items = await db.Notes
            .OrderByDescending(n => n.UpdatedAt)
            .Select(n => ToDto(n))
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var note = await db.Notes.FindAsync([id], ct);
        return note is null ? NotFound() : Ok(ToDto(note));
    }

    [HttpPost]
    public async Task<IActionResult> Create(SaveNoteRequest req, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var note = new Note
        {
            Id = Guid.NewGuid(),
            Title = string.IsNullOrWhiteSpace(req.Title) ? null : req.Title.Trim(),
            Body = req.Body ?? "",
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Notes.Add(note);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = note.Id }, ToDto(note));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SaveNoteRequest req, CancellationToken ct)
    {
        var note = await db.Notes.FindAsync([id], ct);
        if (note is null) return NotFound();

        note.Title = string.IsNullOrWhiteSpace(req.Title) ? null : req.Title.Trim();
        note.Body = req.Body ?? "";
        note.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(ToDto(note));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var note = await db.Notes.FindAsync([id], ct);
        if (note is null) return NotFound();

        db.Notes.Remove(note);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
