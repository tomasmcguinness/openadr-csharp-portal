using System.Collections.Concurrent;

namespace OpenAdr.Vtn.Server.Mqtt;

/// <summary>Tracks which VENs currently hold an MQTT connection, keyed by MQTT client ID
/// (which, by convention, every VEN sets equal to its venName). In-memory only — fine for a
/// single-VEN demo; a restart simply forgets who was connected until they reconnect.</summary>
public class VenConnectionTracker
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _connectedSince = new();

    public event Action<string, bool>? ConnectionChanged;

    public void SetConnected(string mqttClientId, bool connected)
    {
        if (connected)
        {
            _connectedSince[mqttClientId] = DateTimeOffset.UtcNow;
        }
        else
        {
            _connectedSince.TryRemove(mqttClientId, out _);
        }

        ConnectionChanged?.Invoke(mqttClientId, connected);
    }

    public bool IsConnected(string mqttClientId) => _connectedSince.ContainsKey(mqttClientId);
}
