using System.Text.Json;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
using OpenAdr.Vtn.Core;

namespace OpenAdr.Vtn.Server.Mqtt;

/// <summary>Hosts the demo's MQTT broker in-process: plain TCP on the configured port (1883
/// by default), anonymous connections allowed, no TLS — this is a demo with security
/// explicitly out of scope. Also implements IMqttPublisher so REST endpoints can publish
/// notifications through the same broker instance, and tracks VEN connect/disconnect for the
/// dashboard's "MQTT connected" status.</summary>
public class MqttBrokerService(IOptions<VtnOptions> options, VenConnectionTracker venTracker) : IHostedService, IMqttPublisher
{
    private readonly MqttServerFactory _factory = new();
    private MqttServer? _server;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var serverOptions = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(options.Value.MqttPort)
            .Build();

        _server = _factory.CreateMqttServer(serverOptions);

        _server.ValidatingConnectionAsync += args =>
        {
            // Anonymous connections allowed by design — this is a demo, not production.
            args.ReasonCode = MqttConnectReasonCode.Success;
            return Task.CompletedTask;
        };

        _server.ClientConnectedAsync += args =>
        {
            venTracker.SetConnected(args.ClientId, true);
            return Task.CompletedTask;
        };

        _server.ClientDisconnectedAsync += args =>
        {
            venTracker.SetConnected(args.ClientId, false);
            return Task.CompletedTask;
        };

        await _server.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_server is not null)
        {
            await _server.StopAsync();
        }
    }

    public async Task PublishAsync(string topic, object payload, CancellationToken cancellationToken = default)
    {
        if (_server is null)
        {
            return;
        }

        var json = JsonSerializer.Serialize(payload, OpenAdrJson.Options);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(json)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(false)
            .Build();

        await _server.InjectApplicationMessage(new InjectedMqttApplicationMessage(message));
    }
}
