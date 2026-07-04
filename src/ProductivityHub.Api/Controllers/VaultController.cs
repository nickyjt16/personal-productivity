using Microsoft.AspNetCore.Mvc;
using ProductivityHub.Api.Services;
using ProductivityHub.Core;
using ProductivityHub.Core.Data;

namespace ProductivityHub.Api.Controllers;

// Master-password vault: set it once, then unlock per session to view/save secret
// values. The password is never stored or returned — only a hint.
[ApiController]
[Route("api/vault")]
public class VaultController(AppDbContext db, VaultSession session) : ControllerBase
{
    public record VaultStatus(bool Configured, bool Unlocked, string? Hint);
    public record SetVaultRequest(string Password, string? Hint);
    public record UnlockRequest(string Password);

    [HttpGet]
    public async Task<IActionResult> Status(CancellationToken ct)
    {
        var config = await VaultService.GetConfigAsync(db, ct);
        return Ok(new VaultStatus(config is not null, session.IsUnlocked, config?.Hint));
    }

    [HttpPost("set")]
    public async Task<IActionResult> Set(SetVaultRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < 4)
            return BadRequest("Password must be at least 4 characters.");
        if (await VaultService.IsConfiguredAsync(db, ct))
            return Conflict("A master password is already set.");

        var key = await VaultService.CreateAsync(db, req.Password, req.Hint, ct);
        session.Unlock(key);
        return Ok(new VaultStatus(true, true, string.IsNullOrWhiteSpace(req.Hint) ? null : req.Hint.Trim()));
    }

    [HttpPost("unlock")]
    public async Task<IActionResult> Unlock(UnlockRequest req, CancellationToken ct)
    {
        var key = await VaultService.UnlockAsync(db, req.Password, ct);
        if (key is null) return BadRequest("Wrong password.");
        session.Unlock(key);
        return Ok(new VaultStatus(true, true, (await VaultService.GetConfigAsync(db, ct))?.Hint));
    }

    [HttpPost("lock")]
    public IActionResult Lock()
    {
        session.Lock();
        return NoContent();
    }
}
