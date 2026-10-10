namespace OpenAdr.Vtn.Core.Models;

/// <summary>Client-provided body for POST/PUT /events.</summary>
public class EventRequest
{
    public string ProgramId { get; set; } = "";
    public string? EventName { get; set; }

    /// <summary>Interworking extension: ISO 8601 overall event duration. Accepted but not
    /// enforced by this VTN.</summary>
    public string? Duration { get; set; }

    public int? Priority { get; set; }
    public List<string>? Targets { get; set; }
    public List<ReportDescriptor>? ReportDescriptors { get; set; }
    public List<EventPayloadDescriptor>? PayloadDescriptors { get; set; }
    public IntervalPeriod? IntervalPeriod { get; set; }
    public List<Interval>? Intervals { get; set; }
}

/// <summary>Server representation of an event: objectMetadata + EventRequest fields.</summary>
public class EventDto : ObjectMetadata
{
    public string ProgramId { get; set; } = "";
    public string? EventName { get; set; }
    public string? Duration { get; set; }
    public int? Priority { get; set; }
    public List<string>? Targets { get; set; }
    public List<ReportDescriptor>? ReportDescriptors { get; set; }
    public List<EventPayloadDescriptor>? PayloadDescriptors { get; set; }
    public IntervalPeriod? IntervalPeriod { get; set; }
    public List<Interval>? Intervals { get; set; }
}
