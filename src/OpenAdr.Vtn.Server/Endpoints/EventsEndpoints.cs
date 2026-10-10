using System.Xml;
using Microsoft.EntityFrameworkCore;
using OpenAdr.Vtn.Core.Logic;
using OpenAdr.Vtn.Core.Models;
using OpenAdr.Vtn.Server.Auth;
using OpenAdr.Vtn.Server.Data;
using OpenAdr.Vtn.Server.Mqtt;

namespace OpenAdr.Vtn.Server.Endpoints;

public static class EventsEndpoints
{
    public static void MapEventsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/events").AddEndpointFilter<BearerAuthFilter>();

        group.MapGet("/", async (VtnDbContext db, string? programId, string[]? targets, int skip = 0, int? limit = null, bool? active = null) =>
        {
            var query = db.Events.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(programId))
            {
                query = query.Where(e => e.ProgramId == programId);
            }

            var events = await query.ToListAsync();
            var filtered = events.Where(e => TargetMatcher.Matches(e.Targets, targets));

            if (active == true)
            {
                filtered = filtered.Where(e => !HasTranspired(e));
            }

            filtered = filtered.Skip(skip);
            if (limit is { } take)
            {
                filtered = filtered.Take(take);
            }

            return Results.Json(filtered.ToList(), Core.OpenAdrJson.Options);
        });

        group.MapGet("/{eventId}", async (string eventId, VtnDbContext db) =>
        {
            var evt = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId);
            return evt is null
                ? Results.Problem(statusCode: 404, title: "Not Found")
                : Results.Json(evt, Core.OpenAdrJson.Options);
        });

        group.MapPost("/", async (EventRequest request, VtnDbContext db, NotificationService notifications) =>
        {
            var now = DateTimeOffset.UtcNow;
            var evt = new EventDto
            {
                Id = IdGenerator.New("event"),
                ObjectType = ObjectTypes.Event,
                CreatedDateTime = now,
                ModificationDateTime = now,
                ProgramId = request.ProgramId,
                EventName = request.EventName,
                Duration = request.Duration,
                Priority = request.Priority,
                Targets = request.Targets,
                ReportDescriptors = request.ReportDescriptors,
                PayloadDescriptors = request.PayloadDescriptors,
                IntervalPeriod = request.IntervalPeriod,
                Intervals = request.Intervals,
            };

            db.Events.Add(evt);
            await db.SaveChangesAsync();
            await notifications.PublishEventAsync(Operations.Create, evt);

            return Results.Json(evt, Core.OpenAdrJson.Options, statusCode: 201);
        });

        group.MapPut("/{eventId}", async (string eventId, EventRequest request, VtnDbContext db, NotificationService notifications) =>
        {
            var evt = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (evt is null)
            {
                return Results.Problem(statusCode: 404, title: "Not Found");
            }

            evt.ProgramId = request.ProgramId;
            evt.EventName = request.EventName;
            evt.Duration = request.Duration;
            evt.Priority = request.Priority;
            evt.Targets = request.Targets;
            evt.ReportDescriptors = request.ReportDescriptors;
            evt.PayloadDescriptors = request.PayloadDescriptors;
            evt.IntervalPeriod = request.IntervalPeriod;
            evt.Intervals = request.Intervals;
            evt.ModificationDateTime = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync();
            await notifications.PublishEventAsync(Operations.Update, evt);

            return Results.Json(evt, Core.OpenAdrJson.Options);
        });

        group.MapDelete("/{eventId}", async (string eventId, VtnDbContext db, NotificationService notifications) =>
        {
            var evt = await db.Events.FirstOrDefaultAsync(e => e.Id == eventId);
            if (evt is null)
            {
                return Results.Problem(statusCode: 404, title: "Not Found");
            }

            db.Events.Remove(evt);
            await db.SaveChangesAsync();
            await notifications.PublishEventAsync(Operations.Delete, evt);

            return Results.Json(evt, Core.OpenAdrJson.Options);
        });
    }

    /// <summary>Best-effort "has this event finished" check for the ?active= filter: an event
    /// with no start time is always considered active (e.g. a standing report request like
    /// ForecastRequestEvent); one with a start is transpired once start+duration is in the past.</summary>
    private static bool HasTranspired(EventDto evt)
    {
        var start = evt.IntervalPeriod?.Start;
        if (start is null)
        {
            return false;
        }

        var duration = TryParseDuration(evt.Duration) ?? TryParseDuration(evt.IntervalPeriod?.Duration);
        var end = duration is { } d ? start.Value + d : start.Value;
        return end < DateTimeOffset.UtcNow;
    }

    private static TimeSpan? TryParseDuration(string? isoDuration)
    {
        if (string.IsNullOrWhiteSpace(isoDuration))
        {
            return null;
        }

        try
        {
            return XmlConvert.ToTimeSpan(isoDuration);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
