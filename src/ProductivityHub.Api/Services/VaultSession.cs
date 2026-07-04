namespace ProductivityHub.Api.Services;

// Holds the unlocked master key in memory for the API process. Registered as a
// singleton — this is a single-user local app, so one unlocked session per
// running server is exactly right. The key is never persisted or sent to clients.
public class VaultSession
{
    private byte[]? _key;

    public bool IsUnlocked => _key is not null;
    public byte[]? Key => _key;

    public void Unlock(byte[] key) => _key = key;
    public void Lock() => _key = null;
}
