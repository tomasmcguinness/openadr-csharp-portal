using Microsoft.AspNetCore.Http.HttpResults;

namespace OpenAdr.Vtn.Server.Auth;

/// <summary>Requires a well-formed, unexpired bearer token on the request, and makes the
/// client_id it carries available to the endpoint handler via <see cref="GetClientId"/>.
/// Applied to every REST route except POST /auth/token itself.</summary>
public class BearerAuthFilter : IEndpointFilter
{
    private const string ClientIdKey = "ClientId";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var header = context.HttpContext.Request.Headers.Authorization.ToString();

        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Unauthorized("Missing bearer token.");
        }

        var token = header["Bearer ".Length..].Trim();

        if (!TokenService.TryParse(token, DateTimeOffset.UtcNow, out var clientId))
        {
            return Unauthorized("Bearer token is missing, malformed, or expired.");
        }

        context.HttpContext.Items[ClientIdKey] = clientId;

        return await next(context);
    }

    public static string GetClientId(HttpContext httpContext) =>
        (string)httpContext.Items[ClientIdKey]!;

    private static ProblemHttpResult Unauthorized(string detail) =>
        TypedResults.Problem(detail: detail, statusCode: StatusCodes.Status401Unauthorized, title: "Unauthorized");
}
