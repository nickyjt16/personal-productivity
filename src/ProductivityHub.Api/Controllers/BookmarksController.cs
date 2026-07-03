using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data;
using ProductivityHub.Core.Data.Entities;
using ProductivityHub.Api.Models;
using ProductivityHub.Api.Services;

namespace ProductivityHub.Api.Controllers;

[ApiController]
[Route("api/bookmarks")]
public class BookmarksController(AppDbContext db) : ControllerBase
{
    public record BookmarkDto(Guid Id, string Url, string? Title, string? Notes, bool IsRead,
        DateTimeOffset CreatedAt, DateTimeOffset? ReadAt, List<ProjectRef> Projects);

    public record CreateBookmarkRequest(string Url, string? Title, string? Notes);

    private static BookmarkDto ToDto(Bookmark b) =>
        new(b.Id, b.Url, b.Title, b.Notes, b.IsRead, b.CreatedAt, b.ReadAt,
            b.ProjectLinks.Select(l => l.Project!).Select(p => new ProjectRef(p.Id, p.Name, p.Color)).ToList());

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? read, [FromQuery] Guid? projectId, CancellationToken ct)
    {
        var query = db.Bookmarks
            .Include(b => b.ProjectLinks).ThenInclude(l => l.Project)
            .AsQueryable();
        if (read is not null)
            query = query.Where(b => b.IsRead == read);
        if (projectId is not null)
            query = query.Where(b => b.ProjectLinks.Any(l => l.ProjectId == projectId));

        var items = await query
            .OrderBy(b => b.IsRead)
            .ThenByDescending(b => b.CreatedAt)
            .ToListAsync(ct);

        return Ok(items.Select(ToDto));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateBookmarkRequest req,
        [FromServices] TitleFetcher titles, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Url))
            return BadRequest("Url is required.");

        var url = req.Url.Trim();
        var title = string.IsNullOrWhiteSpace(req.Title) ? null : req.Title.Trim();
        // No title supplied — try to read the page's <title>.
        title ??= await titles.FetchAsync(url, ct);

        var bookmark = new Bookmark
        {
            Id = Guid.NewGuid(),
            Url = url,
            Title = title,
            Notes = req.Notes,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Bookmarks.Add(bookmark);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { id = bookmark.Id }, ToDto(bookmark));
    }

    // Pull any links forwarded from Teams (via the OneDrive-synced folder) into bookmarks.
    [HttpPost("import")]
    public async Task<IActionResult> ImportFromTeams([FromServices] LinkImportService importer, CancellationToken ct)
    {
        return Ok(await importer.ImportAsync(ct));
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

    [HttpPut("{id:guid}/projects")]
    public async Task<IActionResult> SetProjects(Guid id, SetProjectsRequest req, CancellationToken ct)
    {
        if (!await db.Bookmarks.AnyAsync(b => b.Id == id, ct)) return NotFound();

        var desired = (req.ProjectIds ?? []).Distinct().ToHashSet();
        var valid = (await db.Projects.Where(p => desired.Contains(p.Id))
            .Select(p => p.Id).ToListAsync(ct)).ToHashSet();

        var existing = await db.BookmarkProjects.Where(x => x.BookmarkId == id).ToListAsync(ct);
        db.BookmarkProjects.RemoveRange(existing.Where(x => !valid.Contains(x.ProjectId)));

        var existingIds = existing.Select(x => x.ProjectId).ToHashSet();
        foreach (var pid in valid.Where(pid => !existingIds.Contains(pid)))
            db.BookmarkProjects.Add(new BookmarkProject { BookmarkId = id, ProjectId = pid });

        await db.SaveChangesAsync(ct);
        return NoContent();
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
