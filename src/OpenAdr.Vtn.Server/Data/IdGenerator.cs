namespace OpenAdr.Vtn.Server.Data;

/// <summary>Generates URL-safe object IDs matching the spec's objectID pattern
/// (^[a-zA-Z0-9_-]*$), e.g. "program-4f2a9c1b3e7d4a0a".</summary>
public static class IdGenerator
{
    public static string New(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..(prefix.Length + 17)];
}
