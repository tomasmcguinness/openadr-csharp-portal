using Microsoft.EntityFrameworkCore;
using OpenAdr.Vtn.Core.Logic;
using OpenAdr.Vtn.Core.Models;

namespace OpenAdr.Vtn.Server.Data;

/// <summary>Seeds the HomeForecast program and its two demo events on first startup only
/// (guarded by "does any program already exist"). Mirrors what a real deployment would
/// configure by hand via POST /programs and /events.</summary>
public static class SeedData
{
    public const string ProgramName = "HomeForecast";

    public static async Task EnsureSeededAsync(VtnDbContext db)
    {
        if (await db.Programs.AnyAsync())
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        var program = new ProgramDto
        {
            Id = IdGenerator.New("program"),
            ObjectType = ObjectTypes.Program,
            CreatedDateTime = now,
            ModificationDateTime = now,
            ProgramName = ProgramName,
            Targets = [TargetNames.Of(TargetNames.ProgramName, ProgramName)],
        };
        db.Programs.Add(program);

        var fixedTariffEvent = new EventDto
        {
            Id = IdGenerator.New("event"),
            ObjectType = ObjectTypes.Event,
            CreatedDateTime = now,
            ModificationDateTime = now,
            ProgramId = program.Id,
            EventName = "FixedTariffEvent",
            IntervalPeriod = new IntervalPeriod { Start = now, Duration = "P1D" },
            PayloadDescriptors =
            [
                new EventPayloadDescriptor { PayloadType = "PRICE", Units = "KWH", Currency = "GBP" },
            ],
            Intervals =
            [
                new Interval
                {
                    Id = 0,
                    IntervalPeriod = new IntervalPeriod { Duration = "PT6H" },
                    Payloads = [new ValuesMap { Type = "PRICE", Values = [0.115] }],
                },
                new Interval
                {
                    Id = 1,
                    IntervalPeriod = new IntervalPeriod { Duration = "PT18H" },
                    Payloads = [new ValuesMap { Type = "PRICE", Values = [0.356] }],
                },
            ],
        };
        db.Events.Add(fixedTariffEvent);

        var forecastRequestEvent = new EventDto
        {
            Id = IdGenerator.New("event"),
            ObjectType = ObjectTypes.Event,
            CreatedDateTime = now,
            ModificationDateTime = now,
            ProgramId = program.Id,
            EventName = "ForecastRequestEvent",
            IntervalPeriod = new IntervalPeriod { Duration = "PT1H" },
            ReportDescriptors =
            [
                new ReportDescriptor
                {
                    PayloadType = "FORECAST",
                    ReadingType = "DEMAND",
                    Units = "KW",
                    Aggregate = true,
                    Frequency = 1,
                    NumIntervals = 24,
                    Repeat = -1,
                },
            ],
        };
        db.Events.Add(forecastRequestEvent);

        await db.SaveChangesAsync();
    }
}
