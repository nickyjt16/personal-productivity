using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Api.Data;
using ProductivityHub.Api.Data.Entities;

namespace ProductivityHub.Api.Controllers;

[ApiController]
[Route("api/bookmarks")]
public class BookmarksController(AppDbContext db) : ControllerBase
{
    public record BookmarkDto(Guid Id, string Url, string? Title, string? Notes, bool IsRead,
        DateTimeOffset CreatedAt, DateTimeOffset? ReadAt);

    public record CreateBookmarkRequest(string Url, string? Title, string? Notes);

    private static BookmarkDto ToDto(Bookmark b) =>
        new(b.Id, b.Url, b.Title, b.Notes, b.IsRead, b.CreatedAt, b.ReadAt);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? read, CancellationToken ct)
    {
        var query = db.Bookmarks.AsQueryable();
        if (read is not null)
            query = query.Where(b => b.IsRead == read);

        var items = await query
            .OrderBy(b => b.IsRead)
            .ThenByDescending(b => b.CreatedAt)
            .Select(b => ToDto(b))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateBookmarkRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Url))
            return BadRequest("Url is required.");

        var bookmark = new Bookmark
        {
            Id = Guid.NewGuid(),
            Url = req.Url.Trim(),
            Title = string.IsNullOrWhiteSpace(req.Title) ? null : req.Title.Trim(),
            Notes = req.Notes,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Bookmarks.Add(bookmark);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { id = bookmark.Id }, ToDto(bookmark));
    }

    [HttpPost("{id:guid}/toggle-read")]
    public async Task<IActionResult> ToggleRead(Guid id, CancellationToken ct)
    {
        var bookmark = await db.Bookmarks.FindAsync([id], ct);
        if (bookmark is null) return NotFound();

        bookmark.IsRead = !bookmark.IsRead;
        bookmark.ReadAt = bookmark.IsRead ? DateTimeOffset.UtcNow : null;
        await db.SaveChangesAsync(ct);
        return Ok(ToDto(bookmark));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var bookmark = await db.Bookmarks.FindAsync([id], ct);
        if (bookmark is null) return NotFound();

        db.Bookmarks.Remove(bookmark);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
