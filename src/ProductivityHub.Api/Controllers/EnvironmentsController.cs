using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data;
using ProductivityHub.Core.Data.Entities;
using ProductivityHub.Api.Models;

namespace ProductivityHub.Api.Controllers;

[ApiController]
[Route("api/environments")]
public class EnvironmentsController(AppDbContext db) : ControllerBase
{
    public record ConfigDto(Guid Id, EnvConfigKind Kind, string Name, string? Value,
        string? Solution, bool IsSet, string? Notes);

    public record EnvDto(Guid Id, string Name, EnvironmentType Type, string? PpEnvironmentId,
        string? Url, string? TenantId, string? Region, string? Notes, List<ConfigDto> Configs,
        List<SecretRef> Secrets);

    public record SaveEnvRequest(string Name, EnvironmentType Type, string? PpEnvironmentId,
        string? Url, string? TenantId, string? Region, string? Notes);

    public record SaveConfigRequest(EnvConfigKind Kind, string Name, string? Value, string? Solution, string? Notes);

    private static ConfigDto ToDto(EnvironmentConfig c) =>
        new(c.Id, c.Kind, c.Name, c.Value, c.Solution, c.IsSet, c.Notes);

    private static EnvDto ToDto(PowerPlatformEnvironment e) =>
        new(e.Id, e.Name, e.Type, e.PpEnvironmentId, e.Url, e.TenantId, e.Region, e.Notes,
            e.Configs.OrderBy(c => c.Kind).ThenBy(c => c.Name).Select(ToDto).ToList(),
            e.SecretLinks.Select(l => l.Secret!).OrderBy(s => s.Name).Select(s => new SecretRef(s.Id, s.Name)).ToList());

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var items = await db.Environments
            .Include(e => e.Configs)
            .Include(e => e.SecretLinks).ThenInclude(l => l.Secret)
            .OrderBy(e => e.Type).ThenBy(e => e.Name).ToListAsync(ct);
        return Ok(items.Select(ToDto));
    }

    [HttpPost]
    public async Task<IActionResult> Create(SaveEnvRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name is required.");
        var now = DateTimeOffset.UtcNow;
        var env = new PowerPlatformEnvironment
        {
            Id = Guid.NewGuid(),
            Name = req.Name.Trim(),
            Type = req.Type,
            PpEnvironmentId = Clean(req.PpEnvironmentId),
            Url = Clean(req.Url),
            TenantId = Clean(req.TenantId),
            Region = Clean(req.Region),
            Notes = Clean(req.Notes),
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.Environments.Add(env);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new { id = env.Id }, ToDto(env));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, SaveEnvRequest req, CancellationToken ct)
    {
        var env = await db.Environments.Include(e => e.Configs).FirstOrDefaultAsync(e => e.Id == id, ct);
        if (env is null) return NotFound();
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name is required.");
        env.Name = req.Name.Trim();
        env.Type = req.Type;
        env.PpEnvironmentId = Clean(req.PpEnvironmentId);
        env.Url = Clean(req.Url);
        env.TenantId = Clean(req.TenantId);
        env.Region = Clean(req.Region);
        env.Notes = Clean(req.Notes);
        env.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Ok(ToDto(env));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var env = await db.Environments.FindAsync([id], ct);
        if (env is null) return NotFound();
        db.Environments.Remove(env);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/configs")]
    public async Task<IActionResult> AddConfig(Guid id, SaveConfigRequest req, CancellationToken ct)
    {
        if (!await db.Environments.AnyAsync(e => e.Id == id, ct)) return NotFound();
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name is required.");
        var config = new EnvironmentConfig
        {
            Id = Guid.NewGuid(),
            EnvironmentId = id,
            Kind = req.Kind,
            Name = req.Name.Trim(),
            Value = Clean(req.Value),
            Solution = Clean(req.Solution),
            Notes = Clean(req.Notes),
        };
        db.EnvironmentConfigs.Add(config);
        await db.SaveChangesAsync(ct);
        return Ok(ToDto(config));
    }

    [HttpPut("{id:guid}/configs/{configId:guid}")]
    public async Task<IActionResult> UpdateConfig(Guid id, Guid configId, SaveConfigRequest req, CancellationToken ct)
    {
        var c = await db.EnvironmentConfigs.FirstOrDefaultAsync(x => x.Id == configId && x.EnvironmentId == id, ct);
        if (c is null) return NotFound();
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name is required.");
        c.Kind = req.Kind;
        c.Name = req.Name.Trim();
        c.Value = Clean(req.Value);
        c.Solution = Clean(req.Solution);
        c.Notes = Clean(req.Notes);
        await db.SaveChangesAsync(ct);
        return Ok(ToDto(c));
    }

    [HttpPost("{id:guid}/configs/{configId:guid}/toggle")]
    public async Task<IActionResult> ToggleConfig(Guid id, Guid configId, CancellationToken ct)
    {
        var c = await db.EnvironmentConfigs.FirstOrDefaultAsync(x => x.Id == configId && x.EnvironmentId == id, ct);
        if (c is null) return NotFound();
        c.IsSet = !c.IsSet;
        await db.SaveChangesAsync(ct);
        return Ok(ToDto(c));
    }

    [HttpDelete("{id:guid}/configs/{configId:guid}")]
    public async Task<IActionResult> DeleteConfig(Guid id, Guid configId, CancellationToken ct)
    {
        var c = await db.EnvironmentConfigs.FirstOrDefaultAsync(x => x.Id == configId && x.EnvironmentId == id, ct);
        if (c is null) return NotFound();
        db.EnvironmentConfigs.Remove(c);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
