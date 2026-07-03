using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data;
using ProductivityHub.Core.Data.Entities;
using ProductivityHub.Api.Models;

namespace ProductivityHub.Api.Controllers;

[ApiController]
[Route("api/todos")]
public class TodosController(AppDbContext db) : ControllerBase
{
    public record TodoDto(Guid Id, string Title, string? Notes, Priority Priority, bool IsDone,
        DateTimeOffset? DueDate, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt,
        RecurUnit RecurUnit, int RecurInterval, List<ProjectRef> Projects);

    public record CreateTodoRequest(string Title, string? Notes, Priority? Priority, DateTimeOffset? DueDate,
        RecurUnit? RecurUnit, int? RecurInterval);

    public record UpdateTodoRequest(string Title, string? Notes, Priority Priority,
        bool IsDone, DateTimeOffset? DueDate, RecurUnit? RecurUnit, int? RecurInterval);

    private static TodoDto ToDto(TodoItem t) =>
        new(t.Id, t.Title, t.Notes, t.Priority, t.IsDone, t.DueDate, t.CreatedAt, t.CompletedAt,
            t.RecurUnit, t.RecurInterval,
            t.ProjectLinks.Select(l => l.Project!).Select(p => new ProjectRef(p.Id, p.Name, p.Color)).ToList());

    private static DateTimeOffset Advance(DateTimeOffset from, RecurUnit unit, int interval) => unit switch
    {
        RecurUnit.Day => from.AddDays(interval),
        RecurUnit.Week => from.AddDays(7 * interval),
        RecurUnit.Month => from.AddMonths(interval),
        _ => from,
    };

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? done, [FromQuery] Guid? projectId, CancellationToken ct)
    {
        var query = db.Todos
            .Include(t => t.ProjectLinks).ThenInclude(l => l.Project)
            .AsQueryable();
        if (done is not null)
            query = query.Where(t => t.IsDone == done);
        if (projectId is not null)
            query = query.Where(t => t.ProjectLinks.Any(l => l.ProjectId == projectId));

        var items = await query
            .OrderBy(t => t.IsDone)
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        return Ok(items.Select(ToDto));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTodoRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return BadRequest("Title is required.");

        var todo = new TodoItem
        {
            Id = Guid.NewGuid(),
            Title = req.Title.Trim(),
            Notes = req.Notes,
            Priority = req.Priority ?? Priority.Medium,
            DueDate = req.DueDate,
            RecurUnit = req.RecurUnit ?? RecurUnit.None,
            RecurInterval = req.RecurInterval ?? 0,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.Todos.Add(todo);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { id = todo.Id }, ToDto(todo));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTodoRequest req, CancellationToken ct)
    {
        var todo = await db.Todos.FindAsync([id], ct);
        if (todo is null) return NotFound();

        todo.Title = req.Title.Trim();
        todo.Notes = req.Notes;
        todo.Priority = req.Priority;
        todo.DueDate = req.DueDate;
        todo.RecurUnit = req.RecurUnit ?? RecurUnit.None;
        todo.RecurInterval = req.RecurInterval ?? 0;
        SetDone(todo, req.IsDone);

        await db.SaveChangesAsync(ct);
        return Ok(ToDto(todo));
    }

    [HttpPost("{id:guid}/toggle")]
    public async Task<IActionResult> Toggle(Guid id, CancellationToken ct)
    {
        var todo = await db.Todos.FindAsync([id], ct);
        if (todo is null) return NotFound();

        // Completing a recurring todo advances its due date instead of closing it.
        if (!todo.IsDone && todo.RecurUnit != RecurUnit.None && todo.RecurInterval > 0)
        {
            var baseDate = todo.DueDate ?? DateTimeOffset.UtcNow;
            todo.DueDate = Advance(baseDate, todo.RecurUnit, todo.RecurInterval);
        }
        else
        {
            SetDone(todo, !todo.IsDone);
        }

        await db.SaveChangesAsync(ct);
        return Ok(ToDto(todo));
    }

    // Replace the full set of projects this todo belongs to.
    [HttpPut("{id:guid}/projects")]
    public async Task<IActionResult> SetProjects(Guid id, SetProjectsRequest req, CancellationToken ct)
    {
        if (!await db.Todos.AnyAsync(t => t.Id == id, ct)) return NotFound();

        var desired = (req.ProjectIds ?? []).Distinct().ToHashSet();
        var valid = (await db.Projects.Where(p => desired.Contains(p.Id))
            .Select(p => p.Id).ToListAsync(ct)).ToHashSet();

        var existing = await db.TodoProjects.Where(x => x.TodoItemId == id).ToListAsync(ct);
        db.TodoProjects.RemoveRange(existing.Where(x => !valid.Contains(x.ProjectId)));

        var existingIds = existing.Select(x => x.ProjectId).ToHashSet();
        foreach (var pid in valid.Where(pid => !existingIds.Contains(pid)))
            db.TodoProjects.Add(new TodoProject { TodoItemId = id, ProjectId = pid });

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var todo = await db.Todos.FindAsync([id], ct);
        if (todo is null) return NotFound();

        db.Todos.Remove(todo);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    private static void SetDone(TodoItem todo, bool isDone)
    {
        todo.IsDone = isDone;
        todo.CompletedAt = isDone ? DateTimeOffset.UtcNow : null;
    }
}
