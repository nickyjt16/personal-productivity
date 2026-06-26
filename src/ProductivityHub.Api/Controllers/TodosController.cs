using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Api.Data;
using ProductivityHub.Api.Data.Entities;

namespace ProductivityHub.Api.Controllers;

[ApiController]
[Route("api/todos")]
public class TodosController(AppDbContext db) : ControllerBase
{
    public record TodoDto(Guid Id, string Title, string? Notes, Priority Priority, bool IsDone,
        DateTimeOffset? DueDate, DateTimeOffset CreatedAt, DateTimeOffset? CompletedAt);

    public record CreateTodoRequest(string Title, string? Notes, Priority? Priority, DateTimeOffset? DueDate);

    public record UpdateTodoRequest(string Title, string? Notes, Priority Priority,
        bool IsDone, DateTimeOffset? DueDate);

    private static TodoDto ToDto(TodoItem t) =>
        new(t.Id, t.Title, t.Notes, t.Priority, t.IsDone, t.DueDate, t.CreatedAt, t.CompletedAt);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool? done, CancellationToken ct)
    {
        var query = db.Todos.AsQueryable();
        if (done is not null)
            query = query.Where(t => t.IsDone == done);

        var items = await query
            .OrderBy(t => t.IsDone)
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .ThenByDescending(t => t.CreatedAt)
            .Select(t => ToDto(t))
            .ToListAsync(ct);

        return Ok(items);
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
        SetDone(todo, req.IsDone);

        await db.SaveChangesAsync(ct);
        return Ok(ToDto(todo));
    }

    [HttpPost("{id:guid}/toggle")]
    public async Task<IActionResult> Toggle(Guid id, CancellationToken ct)
    {
        var todo = await db.Todos.FindAsync([id], ct);
        if (todo is null) return NotFound();

        SetDone(todo, !todo.IsDone);
        await db.SaveChangesAsync(ct);
        return Ok(ToDto(todo));
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
