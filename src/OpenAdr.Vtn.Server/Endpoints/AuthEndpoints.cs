using OpenAdr.Vtn.Core.Models;
using OpenAdr.Vtn.Server.Auth;

namespace OpenAdr.Vtn.Server.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/token", async (HttpRequest request) =>
        {
            var form = await request.ReadFormAsync();

            var grantType = form["grant_type"].ToString();
            var clientId = form["client_id"].ToString();
            var clientSecret = form["client_secret"].ToString();

            if (grantType != "client_credentials")
            {
                return Results.BadRequest(new { error = "unsupported_grant_type" });
            }

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                return Results.BadRequest(new { error = "invalid_request", error_description = "client_id and client_secret are required." });
            }

            var now = DateTimeOffset.UtcNow;
            var response = new ClientCredentialResponse
            {
                AccessToken = TokenService.Issue(clientId, now),
                TokenType = "Bearer",
                ExpiresIn = (int)TokenService.Ttl.TotalSeconds,
                Scope = form["scope"].ToString() is { Length: > 0 } scope ? scope : null,
            };

            return Results.Json(response, Core.OpenAdrJson.Options);
        });
    }
}
