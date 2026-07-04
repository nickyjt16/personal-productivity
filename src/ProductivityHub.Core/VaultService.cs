using Microsoft.EntityFrameworkCore;
using ProductivityHub.Core.Data;
using ProductivityHub.Core.Data.Entities;

namespace ProductivityHub.Core;

// Shared master-password vault operations used by both the desktop app and the
// API, so the two front-ends stay compatible against the same database.
public static class VaultService
{
    public static Task<VaultConfig?> GetConfigAsync(AppDbContext db, CancellationToken ct = default) =>
        db.VaultConfig.AsNoTracking().FirstOrDefaultAsync(v => v.Id == "vault", ct);

    public static async Task<bool> IsConfiguredAsync(AppDbContext db, CancellationToken ct = default) =>
        await GetConfigAsync(db, ct) is not null;

    // Creates the vault (fails if one already exists), encrypts any existing
    // plaintext secret values, and returns the derived key so the caller can
    // hold it for the session.
    public static async Task<byte[]> CreateAsync(AppDbContext db, string password, string? hint,
        CancellationToken ct = default)
    {
        if (await IsConfiguredAsync(db, ct))
            throw new InvalidOperationException("Vault is already configured.");

        var salt = SecretCrypto.NewSalt();
        var key = SecretCrypto.DeriveKey(password, salt, SecretCrypto.DefaultIterations);

        db.VaultConfig.Add(new VaultConfig
        {
            Id = "vault",
            Salt = Convert.ToBase64String(salt),
            Iterations = SecretCrypto.DefaultIterations,
            Verifier = SecretCrypto.MakeVerifier(key),
            Hint = string.IsNullOrWhiteSpace(hint) ? null : hint.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);

        await EncryptExistingAsync(db, key, ct);
        return key;
    }

    // Derives the key from the password and checks it against the stored verifier.
    // Returns the key on success, null on a wrong password (or if no vault exists).
    public static async Task<byte[]?> UnlockAsync(AppDbContext db, string password, CancellationToken ct = default)
    {
        var config = await GetConfigAsync(db, ct);
        if (config is null) return null;

        var salt = Convert.FromBase64String(config.Salt);
        var key = SecretCrypto.DeriveKey(password, salt, config.Iterations);
        return SecretCrypto.VerifyKey(key, config.Verifier) ? key : null;
    }

    // Encrypts every secret value that is still stored as plaintext. Idempotent:
    // already-encrypted values are skipped.
    public static async Task EncryptExistingAsync(AppDbContext db, byte[] key, CancellationToken ct = default)
    {
        var secrets = await db.Secrets.ToListAsync(ct);
        var changed = false;
        foreach (var s in secrets)
        {
            if (!string.IsNullOrEmpty(s.Value) && !SecretCrypto.IsEncrypted(s.Value))
            {
                s.Value = SecretCrypto.Encrypt(s.Value, key);
                changed = true;
            }
        }
        if (changed) await db.SaveChangesAsync(ct);
    }
}
