using System.Security.Cryptography;
using System.Text;

namespace OpenMyo.Yoto.Auth;

/// <summary>PKCE (RFC 7636) and OAuth <c>state</c> helpers.</summary>
internal static class Pkce
{
    private const int VerifierLength = 64;
    private const int StateLength = 32;
    private const string UrlSafeAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";

    public static string CreateVerifier() => RandomNumberGenerator.GetString(UrlSafeAlphabet, VerifierLength);

    public static string CreateState() => RandomNumberGenerator.GetString(UrlSafeAlphabet, StateLength);

    /// <summary>S256 challenge: base64url(SHA-256(ASCII verifier)) without padding.</summary>
    public static string CreateChallenge(string verifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(verifier);
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
