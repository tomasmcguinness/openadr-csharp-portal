namespace OpenAdr.Vtn.Core.Models;

/// <summary>Response body for GET /notifiers.</summary>
public class NotifiersResponse
{
    public bool Webhook { get; set; } = true;
    public MqttNotifierBindingObject? Mqtt { get; set; }
}

/// <summary>Details of the MQTT binding for messaging-protocol notifications.</summary>
public class MqttNotifierBindingObject
{
    public List<string> Uris { get; set; } = [];
    public string Serialization { get; set; } = "JSON";
    public MqttNotifierAuthentication Authentication { get; set; } = new();
}

/// <summary>This demo VTN only ever reports anonymous MQTT authentication.</summary>
public class MqttNotifierAuthentication
{
    public string Method { get; set; } = "ANONYMOUS";
}

/// <summary>Topic names for CREATE/UPDATE/DELETE/ALL operations on one object scope.
/// Matches notifierOperationsTopics: CREATE/ALL are omitted for object-ID-scoped requests.</summary>
public class NotifierOperationsTopics
{
    public string? Create { get; set; }
    public string Update { get; set; } = "";
    public string Delete { get; set; } = "";
    public string? All { get; set; }
}

/// <summary>Response body for every GET /notifiers/mqtt/topics/... endpoint.</summary>
public class NotifierTopicsResponse
{
    public NotifierOperationsTopics Topics { get; set; } = new();
}

/// <summary>VTN-generated object sent to subscription callback URLs and published to MQTT
/// topics on every create/update/delete of a subscribable object.</summary>
public class Notification
{
    public string ObjectType { get; set; } = "";
    public string Operation { get; set; } = "";
    public object Object { get; set; } = new();
    public List<string>? Targets { get; set; }
}
