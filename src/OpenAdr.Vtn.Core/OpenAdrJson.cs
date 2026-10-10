using System.Text.Json;

namespace OpenAdr.Vtn.Core;

/// <summary>The one JSON configuration shared by the server and the fake VEN, so wire bodies
/// are produced and parsed identically on both ends. camelCase matches the OpenADR 3.1
/// schema property names (auth token bodies are the one exception, handled by explicit
/// [JsonPropertyName] attributes for their snake_case fields).</summary>
public static class OpenAdrJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };
}
