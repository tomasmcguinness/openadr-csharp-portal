namespace OpenAdr.Vtn.Core.Logic;

/// <summary>Builds the "TYPE:value" target strings used by the interworking profile's
/// VEN_NAME/PROGRAM_NAME targeting convention. The 3.1 spec's `target` schema is a flat
/// string, so this convention is this project's own (documented) choice of encoding.</summary>
public static class TargetNames
{
    public const string VenName = "VEN_NAME";
    public const string ProgramName = "PROGRAM_NAME";

    public static string Of(string type, string value) => $"{type}:{value}";
}
