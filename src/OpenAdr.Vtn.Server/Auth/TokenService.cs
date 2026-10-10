namespace OpenAdr.Vtn.Server.Auth;

/// <summary>Issues and parses opaque bearer tokens. This is a demo VTN: any client_id/secret
/// is accepted at /auth/token, and tokens are never cryptographically verified — the only
/// thing read back out of one is the client_id it was issued for, and whether it has expired.
/// Expiry is still enforced so the fake VEN's "refresh on 401" recovery path has something
/// real to exercise.</summary>
public static class TokenService
{
    private const string Separator = "|";
    public static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    public static string Issue(string clientId, DateTimeOffset now)
    {
        var expiresAt = now.Add(Ttl).ToUnixTimeSeconds();
        var nonce = Guid.NewGuid().ToString("N");
        return string.Join(Separator, clientId, expiresAt, nonce);
    }

    public static bool TryParse(string? token, DateTimeOffset now, out string clientId)
    {
        clientId = "";

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var parts = token.Split(Separator, 3);
        if (parts.Length != 3)
        {
            return false;
        }

        if (!long.TryParse(parts[1], out var expiresAtUnixSeconds))
        {
            return false;
        }

        if (DateTimeOffset.FromUnixTimeSeconds(expiresAtUnixSeconds) < now)
        {
            return false;
        }

        clientId = parts[0];
        return !string.IsNullOrWhiteSpace(clientId);
    }
}
