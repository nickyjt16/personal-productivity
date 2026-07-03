using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data;
using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Api.Controllers;

[ApiController]
[Route("api/journal")]
public class JournalController(AppDbContext db) : ControllerBase
{
    public record JournalDto(Guid Id, DateOnly EntryDate, string Body, string? Mood,
        DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

    public record SaveJournalRequest(DateOnly EntryDate, string Body, string? Mood);

    private static JournalDto ToDto(JournalEntry e) =>
        new(e.Id, e.EntryDate, e.Body, e.Mood, e.CreatedAt, e.UpdatedAt);

    // Recent entries, most recent first.
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int take = 30, CancellationToken ct = default)
    {
        var items = await db.JournalEntries
            .OrderByDescending(e => e.EntryDate)
            .Take(Math.Clamp(take, 1, 365))
            .Select(e => ToDto(e))
            .ToListAsync(ct);
        return Ok(items);
    }

    [HttpGet("{date}")]
    public async Task<IActionResult> GetByDate(DateOnly date, CancellationToken ct)
    {
        var entry = await db.JournalEntries.FirstOrDefaultAsync(e => e.EntryDate == date, ct);
        return entry is null ? NotFound() : Ok(ToDto(entry));
    }

    // Upsert by date: one entry per calendar day.
    [HttpPut]
    public async Task<IActionResult> Save(SaveJournalRequest req, CancellationToken ct)
    {
        var entry = await db.JournalEntries.FirstOrDefaultAsync(e => e.EntryDate == req.EntryDate, ct);
        var now = DateTimeOffset.UtcNow;

        if (entry is null)
        {
            entry = new JournalEntry
            {
                Id = Guid.NewGuid(),
                EntryDate = req.EntryDate,
                Body = req.Body ?? "",
                Mood = req.Mood,
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.JournalEntries.Add(entry);
        }
        else
        {
            entry.Body = req.Body ?? "";
            entry.Mood = req.Mood;
            entry.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
        return Ok(ToDto(entry));
    }
}
