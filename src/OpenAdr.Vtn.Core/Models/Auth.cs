using System.Text.Json.Serialization;

namespace OpenAdr.Vtn.Core.Models;

/// <summary>Body of POST /auth/token, submitted as application/x-www-form-urlencoded.
/// Snake-case per RFC 6749, not the camelCase used by the rest of the API.</summary>
public class ClientCredentialRequest
{
    [JsonPropertyName("grant_type")]
    public string GrantType { get; set; } = "client_credentials";

    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = "";

    [JsonPropertyName("client_secret")]
    public string ClientSecret { get; set; } = "";

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}

/// <summary>Body of the response from POST /auth/token. Snake-case per RFC 6749.</summary>
public class ClientCredentialResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }
}
