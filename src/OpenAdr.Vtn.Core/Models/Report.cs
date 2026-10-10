namespace OpenAdr.Vtn.Core.Models;

/// <summary>Report data associated with one resource (or the AGGREGATED_REPORT pseudo-resource).</summary>
public class ReportResource
{
    public string ResourceName { get; set; } = "";
    public IntervalPeriod? IntervalPeriod { get; set; }
    public List<Interval> Intervals { get; set; } = [];
}

/// <summary>Client-provided body for POST/PUT /reports.</summary>
public class ReportRequest
{
    public string EventId { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string? ReportName { get; set; }
    public List<ReportPayloadDescriptor>? PayloadDescriptors { get; set; }
    public List<ReportResource> Resources { get; set; } = [];
}

/// <summary>Server representation of a report: objectMetadata + ReportRequest fields + the
/// clientID the VTN resolved from the submitter's bearer token.</summary>
public class ReportDto : ObjectMetadata
{
    public string ClientId { get; set; } = "";
    public string EventId { get; set; } = "";
    public string ClientName { get; set; } = "";
    public string? ReportName { get; set; }
    public List<ReportPayloadDescriptor>? PayloadDescriptors { get; set; }
    public List<ReportResource> Resources { get; set; } = [];
}
