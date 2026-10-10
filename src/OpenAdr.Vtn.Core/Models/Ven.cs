namespace OpenAdr.Vtn.Core.Models;

/// <summary>Client-provided body for POST/PUT /vens. A VEN posts objectType VEN_VEN_REQUEST
/// (no clientID — the VTN fills it in from the bearer token); business-logic clients may post
/// BL_VEN_REQUEST with an explicit ClientId. Both shapes are modelled as one class for
/// simplicity since this demo VTN always derives clientID from the token regardless.</summary>
public class VenRequest
{
    public string ObjectType { get; set; } = "VEN_VEN_REQUEST";
    public string? ClientId { get; set; }
    public string VenName { get; set; } = "";
    public List<string>? Targets { get; set; }
    public List<ValuesMap>? Attributes { get; set; }
}

/// <summary>Server representation of a ven: objectMetadata + BlVenRequest fields (the VTN
/// always reports clientID, even for VEN-created vens).</summary>
public class VenDto : ObjectMetadata
{
    public string ClientId { get; set; } = "";
    public string VenName { get; set; } = "";
    public List<string>? Targets { get; set; }
    public List<ValuesMap>? Attributes { get; set; }
}
