using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Api.Data;
using ProductivityHub.Api.Data.Entities;
using ProductivityHub.Api.Models;

namespace ProductivityHub.Api.Controllers;

[ApiController]
[Route("api/notes")]
public class NotesController(AppDbContext db) : ControllerBase
{
    public record NoteDto(Guid Id, string? Title, string Body,
        DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, List<ProjectRef> Projects);

    public record SaveNoteRequest(string? Title, string Body);

    private static NoteDto ToDto(Note n) =>
        new(n.Id, n.Title, n.Body, n.CreatedAt, n.UpdatedAt,
            n.ProjectLinks.Select(l => new ProjectRef(l.Project.Id, l.Project.Name, l.Project.Color)).ToList());

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? projectId, CancellationToken ct)
    {
        var query = db.Notes
            .Include(n => n.ProjectLinks).ThenInclude(l => l.Project)
            .AsQueryable();
        if (projectId is not null)
            query = query.Where(n => n.ProjectLinks.Any(l => l.ProjectId == projectId));

        var items = await query
            .OrderByDescending(n => n.UpdatedAt)
            .ToListAsync(ct);
        return Ok(items.Select(ToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var note = await db.Notes
            .Include(n => n.ProjectLinks).ThenInclude(l => l.Project)
            .FirstOrDefaultAsync(n => n.Id == id, ct);
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

    [HttpPut("{id:guid}/projects")]
    public async Task<IActionResult> SetProjects(Guid id, SetProjectsRequest req, CancellationToken ct)
    {
        if (!await db.Notes.AnyAsync(n => n.Id == id, ct)) return NotFound();

        var desired = (req.ProjectIds ?? []).Distinct().ToHashSet();
        var valid = (await db.Projects.Where(p => desired.Contains(p.Id))
            .Select(p => p.Id).ToListAsync(ct)).ToHashSet();

        var existing = await db.NoteProjects.Where(x => x.NoteId == id).ToListAsync(ct);
        db.NoteProjects.RemoveRange(existing.Where(x => !valid.Contains(x.ProjectId)));

        var existingIds = existing.Select(x => x.ProjectId).ToHashSet();
        foreach (var pid in valid.Where(pid => !existingIds.Contains(pid)))
            db.NoteProjects.Add(new NoteProject { NoteId = id, ProjectId = pid });

        await db.SaveChangesAsync(ct);
        return NoContent();
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
