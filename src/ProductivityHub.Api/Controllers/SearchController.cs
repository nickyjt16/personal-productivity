using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data;

namespace ProductivityHub.Api.Controllers;

[ApiController]
[Route("api/search")]
public class SearchController(AppDbContext db) : ControllerBase
{
    public record SearchHit(string Type, Guid Id, string Title, string? Subtitle, string? Url);

    public record SearchResults(string Query, List<SearchHit> Todos, List<SearchHit> Notes, List<SearchHit> Bookmarks);

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string q, CancellationToken ct)
    {
        q = (q ?? "").Trim();
        if (q.Length == 0)
            return Ok(new SearchResults("", [], [], []));

        // LIKE is case-insensitive for ASCII in SQLite. Escape wildcards in the term.
        var pattern = "%" + q.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
        const int limit = 25;

        var todos = await db.Todos
            .Where(t => EF.Functions.Like(t.Title, pattern, "\\")
                || (t.Notes != null && EF.Functions.Like(t.Notes, pattern, "\\")))
            .OrderByDescending(t => t.CreatedAt)
            .Take(limit)
            .Select(t => new SearchHit("todo", t.Id, t.Title, t.Notes, null))
            .ToListAsync(ct);

        var notes = await db.Notes
            .Where(n => (n.Title != null && EF.Functions.Like(n.Title, pattern, "\\"))
                || EF.Functions.Like(n.Body, pattern, "\\"))
            .OrderByDescending(n => n.UpdatedAt)
            .Take(limit)
            .Select(n => new SearchHit("note", n.Id, n.Title ?? "Untitled", n.Body, null))
            .ToListAsync(ct);

        var bookmarks = await db.Bookmarks
            .Where(b => EF.Functions.Like(b.Url, pattern, "\\")
                || (b.Title != null && EF.Functions.Like(b.Title, pattern, "\\"))
                || (b.Notes != null && EF.Functions.Like(b.Notes, pattern, "\\")))
            .OrderByDescending(b => b.CreatedAt)
            .Take(limit)
            .Select(b => new SearchHit("bookmark", b.Id, b.Title ?? b.Url, b.Url, b.Url))
            .ToListAsync(ct);

        return Ok(new SearchResults(q, todos, notes, bookmarks));
    }
}
