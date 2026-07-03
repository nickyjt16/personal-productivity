using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data;
using ProductivityHub.Core.Data.Entities;
using ProductivityHub.Api.Models;

namespace ProductivityHub.Api.Controllers;

[ApiController]
[Route("api/secrets")]
public class SecretsController(AppDbContext db) : ControllerBase
{
    public record SecretDto(Guid Id, string Name, string? ClientId, string? Value,
        DateOnly ExpiresOn, string? Notes, List<string> Notify, string? Link,
        List<ProjectRef> Projects, int DaysLeft);

    public record SaveSecretRequest(string Name, string? ClientId, string? Value, DateOnly ExpiresOn,
        string? Notes, List<string>? Notify, string? Link);

    private static SecretDto ToDto(Secret s) =>
        new(s.Id, s.Name, s.ClientId, s.Value, s.ExpiresOn, s.Notes, SplitNotify(s.NotifyList), s.Link,
            s.ProjectLinks.Select(l => l.Project!).Select(p => new ProjectRef(p.Id, p.Name, p.Color)).ToList(),
            s.ExpiresOn.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber);

    private static List<string> SplitNotify(string? raw) =>
        (raw ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static string? JoinNotify(List<string>? list)
    {
        var clean = (list ?? []).Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
        return clean.Count == 0 ? null : string.Join("\n", clean);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? projectId, CancellationToken ct)
    {
        var query = db.Secrets.Include(s => s.ProjectLinks).ThenInclude(l => l.Project).AsQueryable();
        if (projectId is not null)
            query = query.Where(s => s.ProjectLinks.Any(l => l.ProjectId == projectId));
        var items = await query.OrderBy(s => s.ExpiresOn).ToListAsync(ct);
        return Ok(items.Select(ToDto));
    }

    // Secrets expiring within a week (or already expired) — used for the alert.
    [HttpGet("expiring")]
    public async Task<IActionResult> Expiring(CancellationToken ct)
    {
        var cutoff = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        var items = await db.Secrets.Include(s => s.ProjectLinks).ThenInclude(l => l.Project)
            .Where(s => s.ExpiresOn <= cutoff)
            .OrderBy(s => s.ExpiresOn).ToListAsync(ct);
        return Ok(items.Select(ToDto));
    }

    [HttpPost]
    public async Task<IActionResult> Create(SaveSecretRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name is required.");
        var now = DateTimeOffset.UtcNow;
        var secret = new Secret
        {
            Id = Guid.NewGuid(),
            Name = req.Name.Trim(),
            ClientId = string.IsNullOrWhiteSpace(req.ClientId) ? null : req.ClientId.Trim(),
            Value = string.IsNullOrWhiteSpace(req.Value) ? null : req.Value,
            ExpiresOn = req.ExpiresOn,
            Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim(),
            NotifyList = JoinNotify(req.Notify),
            Link = string.IsNullOrWhiteSpace(req.Link) ? null : req.Link.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Secrets.Add(secret);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { id = secret.Id }, ToDto(secret));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SaveSecretRequest req, CancellationToken ct)
    {
        var s = await db.Secrets.FindAsync([id], ct);
        if (s is null) return NotFound();
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name is required.");
        s.Name = req.Name.Trim();
        s.ClientId = string.IsNullOrWhiteSpace(req.ClientId) ? null : req.ClientId.Trim();
        s.Value = string.IsNullOrWhiteSpace(req.Value) ? null : req.Value;
        s.ExpiresOn = req.ExpiresOn;
        s.Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim();
        s.NotifyList = JoinNotify(req.Notify);
        s.Link = string.IsNullOrWhiteSpace(req.Link) ? null : req.Link.Trim();
        s.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(ToDto(s));
    }

    [HttpPut("{id:guid}/projects")]
    public async Task<IActionResult> SetProjects(Guid id, SetProjectsRequest req, CancellationToken ct)
    {
        if (!await db.Secrets.AnyAsync(s => s.Id == id, ct)) return NotFound();

        var desired = (req.ProjectIds ?? []).Distinct().ToHashSet();
        var valid = (await db.Projects.Where(p => desired.Contains(p.Id))
            .Select(p => p.Id).ToListAsync(ct)).ToHashSet();

        var existing = await db.SecretProjects.Where(x => x.SecretId == id).ToListAsync(ct);
        db.SecretProjects.RemoveRange(existing.Where(x => !valid.Contains(x.ProjectId)));

        var existingIds = existing.Select(x => x.ProjectId).ToHashSet();
        foreach (var pid in valid.Where(pid => !existingIds.Contains(pid)))
            db.SecretProjects.Add(new SecretProject { SecretId = id, ProjectId = pid });

        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var s = await db.Secrets.FindAsync([id], ct);
        if (s is null) return NotFound();
        db.Secrets.Remove(s);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
