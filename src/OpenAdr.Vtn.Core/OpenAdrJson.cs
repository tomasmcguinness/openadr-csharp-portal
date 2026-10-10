using System.Text.Encodings.Web;
using System.Text.Json;

namespace OpenAdr.Vtn.Core;

/// <summary>The one JSON configuration shared by the server and the fake VEN, so wire bodies
/// are produced and parsed identically on both ends. camelCase matches the OpenADR 3.1
/// schema property names (auth token bodies are the one exception, handled by explicit
/// [JsonPropertyName] attributes for their snake_case fields). Encoder is relaxed so MQTT
/// topic strings like "programs/+" print as themselves instead of "programs/+" — this
/// is a demo whose bodies are meant to be read and copied, not sent to a browser.</summary>
public static class OpenAdrJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
