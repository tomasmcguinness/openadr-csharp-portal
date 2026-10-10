namespace OpenAdr.Vtn.Server.Mqtt;

/// <summary>Publishes a JSON payload to one MQTT topic at QoS 1, not retained. Implemented by
/// the in-process broker's own publish path (see MqttBrokerService); kept as an interface so
/// REST endpoint code never depends on MQTTnet types directly.</summary>
public interface IMqttPublisher
{
    Task PublishAsync(string topic, object payload, CancellationToken cancellationToken = default);
}
