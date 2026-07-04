using System.Security.Cryptography;
using System.Text;

namespace ProductivityHub.Core;

// Authenticated encryption for secret values, keyed by a PBKDF2-derived master key.
//
// A stored value is either legacy plaintext or an "enc:v1:" envelope:
//   enc:v1:<base64( nonce[12] | tag[16] | ciphertext )>
// AES-GCM gives us tamper detection for free — a wrong key (wrong password) makes
// Decrypt throw, which is how we verify the password against the stored verifier.
public static class SecretCrypto
{
    public const int DefaultIterations = 210_000;
    private const string Prefix = "enc:v1:";
    private const int NonceLen = 12;
    private const int TagLen = 16;
    private const int KeyLen = 32;
    // A fixed known string; encrypting it under the key gives the vault verifier.
    private const string VerifierToken = "productivity-hub-vault-v1";

    public static byte[] NewSalt() => RandomNumberGenerator.GetBytes(16);

    public static byte[] DeriveKey(string password, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA256, KeyLen);

    public static bool IsEncrypted(string? value) =>
        value is not null && value.StartsWith(Prefix, StringComparison.Ordinal);

    public static string Encrypt(string plaintext, byte[] key)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceLen);
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagLen];
        using var aes = new AesGcm(key, TagLen);
        aes.Encrypt(nonce, plain, cipher, tag);

        var blob = new byte[NonceLen + TagLen + cipher.Length];
        Buffer.BlockCopy(nonce, 0, blob, 0, NonceLen);
        Buffer.BlockCopy(tag, 0, blob, NonceLen, TagLen);
        Buffer.BlockCopy(cipher, 0, blob, NonceLen + TagLen, cipher.Length);
        return Prefix + Convert.ToBase64String(blob);
    }

    // Returns the value unchanged if it isn't an encrypted envelope (legacy
    // plaintext or null). Throws (CryptographicException) if the key is wrong.
    public static string? Decrypt(string? value, byte[] key)
    {
        if (!IsEncrypted(value)) return value;
        var blob = Convert.FromBase64String(value!.Substring(Prefix.Length));
        var nonce = blob[..NonceLen];
        var tag = blob[NonceLen..(NonceLen + TagLen)];
        var cipher = blob[(NonceLen + TagLen)..];
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(key, TagLen);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }

    public static string MakeVerifier(byte[] key) => Encrypt(VerifierToken, key);

    public static bool VerifyKey(byte[] key, string verifier)
    {
        try { return Decrypt(verifier, key) == VerifierToken; }
        catch { return false; }
    }
}
