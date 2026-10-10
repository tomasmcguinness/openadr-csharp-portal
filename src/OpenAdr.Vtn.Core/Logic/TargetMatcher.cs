namespace OpenAdr.Vtn.Core.Logic;

/// <summary>Implements target-based filtering for GET /programs and GET /events. Targets are
/// flat strings (e.g. "PROGRAM_NAME:HomeForecast"); per the spec, filter params are additive,
/// so an object matches only if its targets contain every value the caller asked for.</summary>
public static class TargetMatcher
{
    public static bool Matches(IReadOnlyCollection<string>? objectTargets, IReadOnlyCollection<string>? queryTargets)
    {
        if (queryTargets is null || queryTargets.Count == 0)
        {
            return true;
        }

        if (objectTargets is null || objectTargets.Count == 0)
        {
            return false;
        }

        var ownedTargets = new HashSet<string>(objectTargets, StringComparer.Ordinal);
        return queryTargets.All(ownedTargets.Contains);
    }
}
