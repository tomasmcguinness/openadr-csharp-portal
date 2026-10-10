using MQTTnet;

namespace OpenAdr.Vtn.FakeVen;

/// <summary>Wraps the MQTT connection: connects with client ID = venName (the convention the
/// VTN uses to track per-VEN connection status), logs every message received, and reconnects
/// + re-subscribes + re-syncs on disconnect — unprompted, the way the real firmware will need
/// to behave on flaky Wi-Fi.</summary>
public class MqttClientWrapper(string host, int port, string clientId)
{
    private readonly MqttClientFactory _factory = new();
    private readonly List<string> _subscribedTopics = [];
    private IMqttClient? _client;

    /// <summary>Raised after a reconnect completes, so the caller can re-subscribe and
    /// re-sync its own state (program/event discovery) via REST, same as a cold start.</summary>
    public event Func<CancellationToken, Task>? Reconnected;

    public async Task ConnectAsync(CancellationToken ct)
    {
        _client = _factory.CreateMqttClient();

        _client.ApplicationMessageReceivedAsync += args =>
        {
            var payload = args.ApplicationMessage.ConvertPayloadToString();
            Logger.Mqtt(args.ApplicationMessage.Topic, payload);
            return Task.CompletedTask;
        };

        _client.DisconnectedAsync += async args =>
        {
            if (ct.IsCancellationRequested)
            {
                return;
            }

            Logger.Info($"MQTT disconnected ({args.Reason}) — reconnecting in 2s...");
            await Task.Delay(TimeSpan.FromSeconds(2), ct);

            await ConnectOnceAsync(ct);
            foreach (var topic in _subscribedTopics)
            {
                await SubscribeOnceAsync(topic, ct);
            }

            Logger.Info("MQTT reconnected and re-subscribed; resyncing via REST.");
            if (Reconnected is not null)
            {
                await Reconnected.Invoke(ct);
            }
        };

        await ConnectOnceAsync(ct);
    }

    public async Task SubscribeAsync(string topic, CancellationToken ct)
    {
        _subscribedTopics.Add(topic);
        await SubscribeOnceAsync(topic, ct);
    }

    private async Task ConnectOnceAsync(CancellationToken ct)
    {
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(host, port)
            .WithClientId(clientId)
            .Build();

        await _client!.ConnectAsync(options, ct);
        Logger.Info($"MQTT connected to {host}:{port} as client '{clientId}'.");
    }

    private async Task SubscribeOnceAsync(string topic, CancellationToken ct)
    {
        await _client!.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter(topic).Build(), ct);
        Logger.Info($"Subscribed to {topic}");
    }
}
