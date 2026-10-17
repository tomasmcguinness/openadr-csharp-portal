using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenAdr.Vtn.Core.Logic;
using OpenAdr.Vtn.Server.Data;
using OpenAdr.Vtn.Server.Mqtt;

namespace OpenAdr.Vtn.Server.Endpoints;

/// <summary>Read-only support API for the "My Home" dashboard. Deliberately separate from the
/// spec-gated OpenADR REST surface (/programs, /events, /vens, /reports) and unauthenticated:
/// the browser dashboard is not an OpenADR client and has no client_id/secret of its own, and
/// security is explicitly out of scope for this demo. Does the DST-safe local-day bucketing
/// here in C# (reusing the already-tested LocalDayBucketer) rather than duplicating that logic
/// in TypeScript.</summary>
public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/dashboard");

        group.MapGet("/status", async (VtnDbContext db, VenConnectionTracker tracker, IOptions<VtnOptions> options) =>
        {
            var vens = await db.Vens.AsNoTracking().ToListAsync();
            var statuses = new List<object>();

            foreach (var ven in vens)
            {
                // SQLite's EF provider can't translate DateTimeOffset ordering to SQL, so
                // order in memory once the (small, demo-scale) per-VEN report set is fetched.
                var lastReport = (await db.Reports.AsNoTracking()
                        .Where(r => r.ClientName == ven.VenName)
                        .Select(r => r.CreatedDateTime)
                        .ToListAsync())
                    .OrderByDescending(t => t)
                    .Cast<DateTimeOffset?>()
                    .FirstOrDefault();

                statuses.Add(new
                {
                    venName = ven.VenName,
                    venId = ven.Id,
                    mqttConnected = options.Value.MqttEnabled ? tracker.IsConnected(ven.VenName) : (bool?)null,
                    lastReportTime = lastReport,
                });
            }

            return Results.Json(statuses, Core.OpenAdrJson.Options);
        });

        group.MapGet("/forecast", async (string venName, DateOnly date, VtnDbContext db) =>
        {
            var timeZone = LocalDayBucketer.EuropeLondon;
            var buckets = LocalDayBucketer.GetHourBuckets(date, timeZone);
            var dayStart = buckets[0].UtcStart;
            var dayEnd = buckets[^1].UtcEnd;

            // SQLite's EF provider can't translate DateTimeOffset comparisons/ordering to SQL,
            // so filter by the (translatable) ClientName column only and do the date-range
            // filtering and ordering in memory — fine at this demo's scale. A report created
            // shortly before/after local midnight can still carry intervals for this local day
            // (or the previous one), so the slack window is generous rather than exact.
            var reports = (await db.Reports.AsNoTracking()
                    .Where(r => r.ClientName == venName)
                    .ToListAsync())
                .Where(r => r.CreatedDateTime >= dayStart.AddDays(-1) && r.CreatedDateTime <= dayEnd.AddDays(1))
                .OrderBy(r => r.CreatedDateTime)
                .ToList();

            var forecastVersions = new List<(DateTimeOffset CreatedAt, double?[] Values)>();
            var actual = new double?[buckets.Count];

            foreach (var report in reports)
            {
                double?[]? forecastValues = null;

                foreach (var resource in report.Resources)
                {
                    foreach (var interval in resource.Intervals)
                    {
                        var start = interval.IntervalPeriod?.Start ?? resource.IntervalPeriod?.Start;
                        if (start is null)
                        {
                            continue;
                        }

                        var bucketIndex = LocalDayBucketer.FindBucketIndex(buckets, start.Value);
                        if (bucketIndex < 0)
                        {
                            continue;
                        }

                        foreach (var payload in interval.Payloads)
                        {
                            if (payload.Values.Count == 0 || !TryToDouble(payload.Values[0], out var value))
                            {
                                continue;
                            }

                            if (payload.Type == "FORECAST")
                            {
                                forecastValues ??= new double?[buckets.Count];
                                forecastValues[bucketIndex] = value;
                            }
                            else if (payload.Type == "DIRECT_READ")
                            {
                                actual[bucketIndex] = value;
                            }
                        }
                    }
                }

                if (forecastValues is not null)
                {
                    forecastVersions.Add((report.CreatedDateTime, forecastValues));
                }
            }

            var hours = buckets.Select(b => new { b.Index, utcStart = b.UtcStart, label = b.LocalLabel }).ToList();

            return Results.Json(new
            {
                date = date.ToString("yyyy-MM-dd"),
                timeZone = timeZone.Id,
                hours,
                actualKw = actual,
                forecastVersions = forecastVersions.Select((v, i) => new
                {
                    createdAt = v.CreatedAt,
                    isLatest = i == forecastVersions.Count - 1,
                    valuesKw = v.Values,
                }),
            }, Core.OpenAdrJson.Options);
        });
    }

    private static bool TryToDouble(object value, out double result)
    {
        switch (value)
        {
            case double d:
                result = d;
                return true;
            case int i:
                result = i;
                return true;
            case System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.Number } element:
                return element.TryGetDouble(out result);
            default:
                result = 0;
                return false;
        }
    }
}
