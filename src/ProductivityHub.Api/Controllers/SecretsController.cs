using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data;
using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Api.Controllers;

[ApiController]
[Route("api/secrets")]
public class SecretsController(AppDbContext db) : ControllerBase
{
    public record SecretDto(Guid Id, string Name, string? ClientId, string? Value,
        DateOnly ExpiresOn, string? Notes, List<string> Notify, int DaysLeft);

    public record SaveSecretRequest(string Name, string? ClientId, string? Value, DateOnly ExpiresOn,
        string? Notes, List<string>? Notify);

    private static SecretDto ToDto(Secret s) =>
        new(s.Id, s.Name, s.ClientId, s.Value, s.ExpiresOn, s.Notes, SplitNotify(s.NotifyList),
            s.ExpiresOn.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber);

    private static List<string> SplitNotify(string? raw) =>
        (raw ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static string? JoinNotify(List<string>? list)
    {
        var clean = (list ?? []).Select(x => x.Trim()).Where(x => x.Length > 0).ToList();
        return clean.Count == 0 ? null : string.Join("\n", clean);
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var items = await db.Secrets.OrderBy(s => s.ExpiresOn).ToListAsync(ct);
        return Ok(items.Select(ToDto));
    }

    // Secrets expiring within a week (or already expired) — used for the alert.
    [HttpGet("expiring")]
    public async Task<IActionResult> Expiring(CancellationToken ct)
    {
        var cutoff = DateOnly.FromDateTime(DateTime.Today.AddDays(7));
        var items = await db.Secrets.Where(s => s.ExpiresOn <= cutoff)
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
        s.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(ToDto(s));
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
