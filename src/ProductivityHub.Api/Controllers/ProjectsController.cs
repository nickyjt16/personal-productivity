using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data;
using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Api.Controllers;

[ApiController]
[Route("api/projects")]
public class ProjectsController(AppDbContext db) : ControllerBase
{
    public record ProjectDto(Guid Id, string Name, string? Description, string Color,
        ProjectStatus Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
        int TodosTotal, int TodosDone, int NoteCount, int BookmarkCount, int SecretCount);

    public record SaveProjectRequest(string Name, string? Description, string? Color, ProjectStatus? Status);

    // status: open (default, New+Active) | new | active | complete | archived | all
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string status = "open", CancellationToken ct = default)
    {
        var query = status.ToLowerInvariant() switch
        {
            "new" => db.Projects.Where(p => p.Status == ProjectStatus.New),
            "active" => db.Projects.Where(p => p.Status == ProjectStatus.Active),
            "complete" => db.Projects.Where(p => p.Status == ProjectStatus.Complete),
            "archived" => db.Projects.Where(p => p.Status == ProjectStatus.Archived),
            "all" => db.Projects,
            _ => db.Projects.Where(p => p.Status == ProjectStatus.New || p.Status == ProjectStatus.Active),
        };

        var projects = await query
            .OrderBy(p => p.Status)
            .ThenBy(p => p.Name)
            .Select(p => new ProjectDto(
                p.Id, p.Name, p.Description, p.Color, p.Status, p.CreatedAt, p.UpdatedAt,
                db.TodoProjects.Count(tp => tp.ProjectId == p.Id),
                db.TodoProjects.Count(tp => tp.ProjectId == p.Id && tp.TodoItem!.IsDone),
                db.NoteProjects.Count(np => np.ProjectId == p.Id),
                db.BookmarkProjects.Count(bp => bp.ProjectId == p.Id),
                db.SecretProjects.Count(sp => sp.ProjectId == p.Id)))
            .ToListAsync(ct);

        return Ok(projects);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var p = await db.Projects.FindAsync([id], ct);
        return p is null ? NotFound() : Ok(await ToDto(p, ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create(SaveProjectRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest("Name is required.");

        var now = DateTimeOffset.UtcNow;
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = req.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim(),
            Color = string.IsNullOrWhiteSpace(req.Color) ? "#0d6efd" : req.Color!.Trim(),
            Status = req.Status ?? ProjectStatus.New,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(Get), new { id = project.Id }, await ToDto(project, ct));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SaveProjectRequest req, CancellationToken ct)
    {
        var project = await db.Projects.FindAsync([id], ct);
        if (project is null) return NotFound();
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name is required.");

        project.Name = req.Name.Trim();
        project.Description = string.IsNullOrWhiteSpace(req.Description) ? null : req.Description.Trim();
        if (!string.IsNullOrWhiteSpace(req.Color)) project.Color = req.Color!.Trim();
        if (req.Status.HasValue) project.Status = req.Status.Value;
        project.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Ok(await ToDto(project, ct));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var project = await db.Projects.FindAsync([id], ct);
        if (project is null) return NotFound();

        // Remove link rows explicitly (works regardless of FK cascade support).
        await db.TodoProjects.Where(x => x.ProjectId == id).ExecuteDeleteAsync(ct);
        await db.NoteProjects.Where(x => x.ProjectId == id).ExecuteDeleteAsync(ct);
        await db.BookmarkProjects.Where(x => x.ProjectId == id).ExecuteDeleteAsync(ct);
        await db.SecretProjects.Where(x => x.ProjectId == id).ExecuteDeleteAsync(ct);

        db.Projects.Remove(project);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private async Task<ProjectDto> ToDto(Project p, CancellationToken ct) =>
        new(p.Id, p.Name, p.Description, p.Color, p.Status, p.CreatedAt, p.UpdatedAt,
            await db.TodoProjects.CountAsync(tp => tp.ProjectId == p.Id, ct),
            await db.TodoProjects.CountAsync(tp => tp.ProjectId == p.Id && tp.TodoItem!.IsDone, ct),
            await db.NoteProjects.CountAsync(np => np.ProjectId == p.Id, ct),
            await db.BookmarkProjects.CountAsync(bp => bp.ProjectId == p.Id, ct),
            await db.SecretProjects.CountAsync(sp => sp.ProjectId == p.Id, ct));
}
