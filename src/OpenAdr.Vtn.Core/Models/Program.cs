namespace OpenAdr.Vtn.Core.Models;

/// <summary>Client-provided body for POST/PUT /programs.</summary>
public class ProgramRequest
{
    public string ProgramName { get; set; } = "";
    public IntervalPeriod? IntervalPeriod { get; set; }
    public List<ProgramDescription>? ProgramDescriptions { get; set; }

    /// <summary>Mixed list of eventPayloadDescriptor/reportPayloadDescriptor; kept untyped
    /// here since this demo never needs to interpret it.</summary>
    public List<object>? PayloadDescriptors { get; set; }

    public List<ValuesMap>? Attributes { get; set; }
    public List<string>? Targets { get; set; }
}

/// <summary>Server representation of a program: objectMetadata + ProgramRequest fields.</summary>
public class ProgramDto : ObjectMetadata
{
    public string ProgramName { get; set; } = "";
    public IntervalPeriod? IntervalPeriod { get; set; }
    public List<ProgramDescription>? ProgramDescriptions { get; set; }
    public List<object>? PayloadDescriptors { get; set; }
    public List<ValuesMap>? Attributes { get; set; }
    public List<string>? Targets { get; set; }
}
