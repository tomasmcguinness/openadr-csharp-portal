using OpenAdr.Vtn.Core.Models;

namespace OpenAdr.Vtn.Core.Logic;

public record ValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static ValidationResult Ok() => new(true, []);
    public static ValidationResult Fail(params string[] errors) => new(false, errors);
}

/// <summary>Validates POST/PUT /reports bodies. Deliberately lenient: this VTN is a demo, so
/// it only rejects reports that are structurally broken (nothing to report, or a payload with
/// no values), not ones that merely use fields loosely — variable-length intervals, unknown
/// reportIntervals values, extra fields, differing interval counts per resource, etc. are all
/// accepted without complaint.</summary>
public static class ReportValidator
{
    public static ValidationResult Validate(ReportRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.EventId))
        {
            errors.Add("eventID is required.");
        }

        if (string.IsNullOrWhiteSpace(request.ClientName))
        {
            errors.Add("clientName is required.");
        }

        if (request.Resources.Count == 0)
        {
            errors.Add("resources must contain at least one entry.");
        }

        for (var r = 0; r < request.Resources.Count; r++)
        {
            var resource = request.Resources[r];

            if (string.IsNullOrWhiteSpace(resource.ResourceName))
            {
                errors.Add($"resources[{r}].resourceName is required.");
            }

            if (resource.Intervals.Count == 0)
            {
                errors.Add($"resources[{r}].intervals must contain at least one entry.");
                continue;
            }

            for (var i = 0; i < resource.Intervals.Count; i++)
            {
                var interval = resource.Intervals[i];

                if (interval.Payloads.Count == 0)
                {
                    errors.Add($"resources[{r}].intervals[{i}].payloads must contain at least one entry.");
                    continue;
                }

                for (var p = 0; p < interval.Payloads.Count; p++)
                {
                    var payload = interval.Payloads[p];

                    if (string.IsNullOrWhiteSpace(payload.Type))
                    {
                        errors.Add($"resources[{r}].intervals[{i}].payloads[{p}].type is required.");
                    }

                    if (payload.Values.Count == 0)
                    {
                        errors.Add($"resources[{r}].intervals[{i}].payloads[{p}].values must contain at least one entry.");
                    }
                }
            }
        }

        return errors.Count == 0 ? ValidationResult.Ok() : new ValidationResult(false, errors);
    }
}
