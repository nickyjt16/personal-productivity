using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProductivityHub.Core;
using Xunit;

namespace ProductivityHub.Api.Tests;

// Uses its own ApiFactory instance (fresh DB + fresh VaultSession singleton).
public class VaultTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client;

    public VaultTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Secret_value_is_encrypted_at_rest_and_hidden_when_locked()
    {
        // Before a vault exists, secrets can't be added at all.
        var status0 = await _client.GetFromJsonAsync<JsonElement>("/api/vault", Json);
        Assert.False(status0.GetProperty("configured").GetBoolean());

        var blockedNoVault = await _client.PostAsJsonAsync("/api/secrets",
            new { name = "AAD app", value = "s3cr3t-value", expiresOn = "2027-01-01" });
        Assert.Equal(HttpStatusCode.Conflict, blockedNoVault.StatusCode);

        // Set the master password, then the secret can be created (encrypted).
        var set = await _client.PostAsJsonAsync("/api/vault/set",
            new { password = "correct horse", hint = "the animal" });
        Assert.Equal(HttpStatusCode.OK, set.StatusCode);
        var setBody = await set.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.True(setBody.GetProperty("configured").GetBoolean());
        Assert.True(setBody.GetProperty("unlocked").GetBoolean());

        var created = await _client.PostAsJsonAsync("/api/secrets",
            new { name = "AAD app", value = "s3cr3t-value", expiresOn = "2027-01-01" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);

        // Unlocked: the value round-trips back to the original plaintext.
        var unlockedList = await _client.GetFromJsonAsync<JsonElement>("/api/secrets", Json);
        var secret = unlockedList[0];
        Assert.Equal("s3cr3t-value", secret.GetProperty("value").GetString());
        Assert.True(secret.GetProperty("hasValue").GetBoolean());
        Assert.False(secret.GetProperty("locked").GetBoolean());

        // Lock: value is withheld but the app still knows one exists.
        Assert.Equal(HttpStatusCode.NoContent, (await _client.PostAsync("/api/vault/lock", null)).StatusCode);
        var lockedList = await _client.GetFromJsonAsync<JsonElement>("/api/secrets", Json);
        Assert.Equal(JsonValueKind.Null, lockedList[0].GetProperty("value").ValueKind);
        Assert.True(lockedList[0].GetProperty("hasValue").GetBoolean());
        Assert.True(lockedList[0].GetProperty("locked").GetBoolean());

        // Saving a value while locked is refused.
        var blocked = await _client.PostAsJsonAsync("/api/secrets",
            new { name = "blocked", value = "nope", expiresOn = "2027-01-01" });
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);

        // Wrong password stays locked; correct one reveals again.
        Assert.Equal(HttpStatusCode.BadRequest,
            (await _client.PostAsJsonAsync("/api/vault/unlock", new { password = "wrong" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await _client.PostAsJsonAsync("/api/vault/unlock", new { password = "correct horse" })).StatusCode);
        var reList = await _client.GetFromJsonAsync<JsonElement>("/api/secrets", Json);
        Assert.Equal("s3cr3t-value", reList[0].GetProperty("value").GetString());

        // The hint is exposed (for the unlock prompt) but never the password.
        var status = await _client.GetFromJsonAsync<JsonElement>("/api/vault", Json);
        Assert.Equal("the animal", status.GetProperty("hint").GetString());
    }

    [Fact]
    public void Crypto_round_trips_and_wrong_key_fails()
    {
        var salt = SecretCrypto.NewSalt();
        var key = SecretCrypto.DeriveKey("hunter2", salt, 1000);
        var envelope = SecretCrypto.Encrypt("my-secret", key);

        Assert.True(SecretCrypto.IsEncrypted(envelope));
        Assert.DoesNotContain("my-secret", envelope);          // not stored in the clear
        Assert.Equal("my-secret", SecretCrypto.Decrypt(envelope, key));

        var wrong = SecretCrypto.DeriveKey("hunter3", salt, 1000);
        Assert.ThrowsAny<System.Security.Cryptography.CryptographicException>(
            () => SecretCrypto.Decrypt(envelope, wrong));

        // A verifier confirms the right key and rejects the wrong one.
        var verifier = SecretCrypto.MakeVerifier(key);
        Assert.True(SecretCrypto.VerifyKey(key, verifier));
        Assert.False(SecretCrypto.VerifyKey(wrong, verifier));
    }
}
