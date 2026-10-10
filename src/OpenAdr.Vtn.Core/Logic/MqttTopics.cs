using OpenAdr.Vtn.Core.Models;

namespace OpenAdr.Vtn.Core.Logic;

/// <summary>Builds the MQTT topic names this VTN uses, in both places that need them: the
/// GET /notifiers/mqtt/topics/... endpoints (which advertise topic names to clients) and the
/// publisher that writes notifications to those same topics. Keeping both callers going
/// through this one class is what guarantees they can't drift apart.
///
/// Scheme, following the spec's own example (programs/create, programs/update, .../+):
///   programs/{create|update|delete|+}
///   programs/{programId}/{update|delete|+}                 (no CREATE for a specific id)
///   events/{create|update|delete|+}
///   programs/{programId}/events/{create|update|delete|+}
///   vens/{venId}/{update|delete|+}
///   vens/{venId}/events/{create|update|delete|+}
///   vens/{venId}/programs/{create|update|delete|+}
///   reports/{create|update|delete|+}
/// </summary>
public static class MqttTopics
{
    public static NotifierOperationsTopics Programs() => AllScope("programs");

    public static NotifierOperationsTopics Program(string programId) => IdScope($"programs/{programId}");

    public static NotifierOperationsTopics Events() => AllScope("events");

    public static NotifierOperationsTopics ProgramEvents(string programId) => AllScope($"programs/{programId}/events");

    public static NotifierOperationsTopics VensAll() => AllScope("vens");

    public static NotifierOperationsTopics Ven(string venId) => IdScope($"vens/{venId}");

    public static NotifierOperationsTopics VenEvents(string venId) => AllScope($"vens/{venId}/events");

    public static NotifierOperationsTopics VenPrograms(string venId) => AllScope($"vens/{venId}/programs");

    public static NotifierOperationsTopics Reports() => AllScope("reports");

    /// <summary>The literal topic a given operation is published to for "all objects of this
    /// type" (e.g. programs/create). Used by the publisher; always available (CREATE included).</summary>
    public static string ForOperation(string scope, string operation) => $"{scope}/{operation.ToLowerInvariant()}";

    /// <summary>Picks the CREATE/UPDATE/DELETE topic matching one operation out of a
    /// discovered topic set. Returns null for CREATE on an object-ID-scoped set (there isn't
    /// one — see the class remarks).</summary>
    public static string? TopicFor(NotifierOperationsTopics topics, string operation) => operation switch
    {
        "CREATE" => topics.Create,
        "UPDATE" => topics.Update,
        "DELETE" => topics.Delete,
        _ => null,
    };

    private static NotifierOperationsTopics AllScope(string scope) => new()
    {
        Create = $"{scope}/create",
        Update = $"{scope}/update",
        Delete = $"{scope}/delete",
        All = $"{scope}/+",
    };

    /// <summary>Per the spec: topic sets scoped to one object ID never include CREATE (you
    /// can't be notified of an object's creation by its ID before it exists).</summary>
    private static NotifierOperationsTopics IdScope(string scope) => new()
    {
        Update = $"{scope}/update",
        Delete = $"{scope}/delete",
        All = $"{scope}/+",
    };
}
