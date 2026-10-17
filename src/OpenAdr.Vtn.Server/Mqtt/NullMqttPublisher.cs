namespace OpenAdr.Vtn.Server.Mqtt;

/// <summary>Stands in for IMqttPublisher when Vtn:MqttEnabled is false, so NotificationService
/// (and anything else that publishes notifications) doesn't need to know MQTT is off.</summary>
public class NullMqttPublisher : IMqttPublisher
{
    public Task PublishAsync(string topic, object payload, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
