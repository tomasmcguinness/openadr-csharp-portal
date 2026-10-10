using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using OpenAdr.Vtn.Core;
using OpenAdr.Vtn.Core.Models;

namespace OpenAdr.Vtn.FakeVen;

/// <summary>Result of one REST call: the status code and raw response body. Parsing into a
/// DTO is left to the caller — mirrors the shape an ESP-IDF esp_http_client call naturally
/// takes (status code + buffer, then cJSON_Parse on demand).</summary>
public record ApiResponse(HttpStatusCode StatusCode, string Body)
{
    public bool IsSuccess => (int)StatusCode is >= 200 and < 300;
    public T Json<T>() => System.Text.Json.JsonSerializer.Deserialize<T>(Body, OpenAdrJson.Options)!;
}

/// <summary>Thin REST client for the VTN. Holds the current bearer token and transparently
/// fetches a new one and retries once on a 401 — the only "recovery" logic that belongs at
/// this layer; 404-driven rediscovery is a FakeVenRunner concern, not a transport concern.</summary>
public class VtnClient(HttpClient http, string clientId, string clientSecret)
{
    private string? _accessToken;

    public async Task<ClientCredentialResponse> FetchTokenAsync(CancellationToken ct)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
        });

        Logger.Request("POST", "/auth/token", $"grant_type=client_credentials&client_id={clientId}&client_secret={clientSecret}");

        using var response = await http.PostAsync("/auth/token", form, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        Logger.Response(response.StatusCode, body);
        response.EnsureSuccessStatusCode();

        var token = JsonSerializer.Deserialize<ClientCredentialResponse>(body, OpenAdrJson.Options)!;
        _accessToken = token.AccessToken;
        return token;
    }

    public Task<ApiResponse> GetAsync(string path, CancellationToken ct) =>
        SendWithRetryAsync(HttpMethod.Get, path, null, ct);

    public Task<ApiResponse> PostAsync(string path, object body, CancellationToken ct) =>
        SendWithRetryAsync(HttpMethod.Post, path, body, ct);

    private async Task<ApiResponse> SendWithRetryAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var response = await SendOnceAsync(method, path, body, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            Logger.Info("Got 401 — fetching a fresh token and retrying once.");
            await FetchTokenAsync(ct);
            response = await SendOnceAsync(method, path, body, ct);
        }

        return response;
    }

    private async Task<ApiResponse> SendOnceAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

        string? requestJson = null;
        if (body is not null)
        {
            requestJson = JsonSerializer.Serialize(body, OpenAdrJson.Options);
            request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        }

        Logger.Request(method.Method, path, requestJson);

        using var response = await http.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);
        Logger.Response(response.StatusCode, responseBody);

        return new ApiResponse(response.StatusCode, responseBody);
    }
}
