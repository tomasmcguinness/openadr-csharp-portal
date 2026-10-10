namespace OpenAdr.Vtn.Core.Models;

/// <summary>Well-known values for objectType discriminators. Servers/clients may also see
/// private/unknown values; nothing in this project rejects a value simply for not being
/// one of these constants.</summary>
public static class ObjectTypes
{
    public const string Program = "PROGRAM";
    public const string Event = "EVENT";
    public const string Report = "REPORT";
    public const string Ven = "VEN";
    public const string Resource = "RESOURCE";
    public const string Subscription = "SUBSCRIPTION";
}

/// <summary>Metadata every VTN-addressable object carries, written by the VTN on create/update.</summary>
public class ObjectMetadata
{
    public string Id { get; set; } = "";
    public DateTimeOffset CreatedDateTime { get; set; }
    public DateTimeOffset ModificationDateTime { get; set; }
    public string ObjectType { get; set; } = "";
}

/// <summary>Defines the temporal span of an event/program/interval/report resource.</summary>
public class IntervalPeriod
{
    public DateTimeOffset? Start { get; set; }

    /// <summary>ISO 8601 duration, e.g. "PT1H".</summary>
    public string? Duration { get; set; }

    /// <summary>ISO 8601 duration describing the randomization window applied to Start.</summary>
    public string? RandomizeStart { get; set; }
}

/// <summary>A single data point on a 2-D grid, used as an alternative valuesMap value shape.</summary>
public class Point
{
    public float X { get; set; }
    public float Y { get; set; }
}

/// <summary>One or more values associated with a type, e.g. {"type":"PRICE","values":[0.17]}.
/// Values are stored as plain objects (number/string/bool/Point) since the spec allows any of them.</summary>
public class ValuesMap
{
    public string Type { get; set; } = "";
    public List<object> Values { get; set; } = [];
}

/// <summary>A temporal window plus a list of valuesMap payloads.</summary>
public class Interval
{
    public int Id { get; set; }
    public IntervalPeriod? IntervalPeriod { get; set; }
    public List<ValuesMap> Payloads { get; set; } = [];
}

/// <summary>Contextual information used to interpret event payload values (e.g. units/currency for PRICE).</summary>
public class EventPayloadDescriptor
{
    public string ObjectType { get; set; } = "EVENT_PAYLOAD_DESCRIPTOR";
    public string PayloadType { get; set; } = "";
    public string? Units { get; set; }
    public string? Currency { get; set; }
}

/// <summary>Contextual information used to interpret report payload values (e.g. units for USAGE).</summary>
public class ReportPayloadDescriptor
{
    public string ObjectType { get; set; } = "REPORT_PAYLOAD_DESCRIPTOR";
    public string PayloadType { get; set; } = "";
    public string? ReadingType { get; set; }
    public string? Units { get; set; }
    public float? Accuracy { get; set; }
    public int? Confidence { get; set; }
}

/// <summary>An object a VTN uses to request a report from a VEN (carried on an event).</summary>
public class ReportDescriptor
{
    public string PayloadType { get; set; } = "";
    public string? ReadingType { get; set; }
    public string? Units { get; set; }
    public List<string>? Targets { get; set; }
    public bool Aggregate { get; set; }
    public int StartInterval { get; set; } = -1;
    public int NumIntervals { get; set; } = -1;
    public bool Historical { get; set; } = true;
    public int Frequency { get; set; } = -1;
    public int Repeat { get; set; } = 1;

    /// <summary>Interworking/extension field accepted leniently: INTERVALS | SUB_INTERVALS | OPEN_INTERVALS.</summary>
    public string? ReportIntervals { get; set; } = "INTERVALS";
}

/// <summary>A human- or machine-readable description of a program.</summary>
public class ProgramDescription
{
    public string Url { get; set; } = "";
}
