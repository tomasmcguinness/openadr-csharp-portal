using Microsoft.EntityFrameworkCore;
using OpenAdr.Vtn.Core.Logic;
using OpenAdr.Vtn.Core.Models;
using OpenAdr.Vtn.Server.Auth;
using OpenAdr.Vtn.Server.Data;
using OpenAdr.Vtn.Server.Mqtt;

namespace OpenAdr.Vtn.Server.Endpoints;

public static class ProgramsEndpoints
{
    public static void MapProgramsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/programs").AddEndpointFilter<BearerAuthFilter>();

        group.MapGet("/", async (VtnDbContext db, string[]? targets, int skip = 0, int? limit = null) =>
        {
            var programs = await db.Programs.AsNoTracking().ToListAsync();
            var filtered = programs.Where(p => TargetMatcher.Matches(p.Targets, targets)).Skip(skip);
            if (limit is { } take)
            {
                filtered = filtered.Take(take);
            }

            return Results.Json(filtered.ToList(), Core.OpenAdrJson.Options);
        });

        group.MapGet("/{programId}", async (string programId, VtnDbContext db) =>
        {
            var program = await db.Programs.AsNoTracking().FirstOrDefaultAsync(p => p.Id == programId);
            return program is null
                ? Results.Problem(statusCode: 404, title: "Not Found")
                : Results.Json(program, Core.OpenAdrJson.Options);
        });

        group.MapPost("/", async (ProgramRequest request, VtnDbContext db, NotificationService notifications) =>
        {
            var now = DateTimeOffset.UtcNow;
            var program = new ProgramDto
            {
                Id = IdGenerator.New("program"),
                ObjectType = ObjectTypes.Program,
                CreatedDateTime = now,
                ModificationDateTime = now,
                ProgramName = request.ProgramName,
                IntervalPeriod = request.IntervalPeriod,
                ProgramDescriptions = request.ProgramDescriptions,
                PayloadDescriptors = request.PayloadDescriptors,
                Attributes = request.Attributes,
                Targets = WithProgramNameTarget(request.Targets, request.ProgramName),
            };

            db.Programs.Add(program);
            await db.SaveChangesAsync();
            await notifications.PublishProgramAsync(Operations.Create, program);

            return Results.Json(program, Core.OpenAdrJson.Options, statusCode: 201);
        });

        group.MapPut("/{programId}", async (string programId, ProgramRequest request, VtnDbContext db, NotificationService notifications) =>
        {
            var program = await db.Programs.FirstOrDefaultAsync(p => p.Id == programId);
            if (program is null)
            {
                return Results.Problem(statusCode: 404, title: "Not Found");
            }

            program.ProgramName = request.ProgramName;
            program.IntervalPeriod = request.IntervalPeriod;
            program.ProgramDescriptions = request.ProgramDescriptions;
            program.PayloadDescriptors = request.PayloadDescriptors;
            program.Attributes = request.Attributes;
            program.Targets = WithProgramNameTarget(request.Targets, request.ProgramName);
            program.ModificationDateTime = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync();
            await notifications.PublishProgramAsync(Operations.Update, program);

            return Results.Json(program, Core.OpenAdrJson.Options);
        });

        group.MapDelete("/{programId}", async (string programId, VtnDbContext db, NotificationService notifications) =>
        {
            var program = await db.Programs.FirstOrDefaultAsync(p => p.Id == programId);
            if (program is null)
            {
                return Results.Problem(statusCode: 404, title: "Not Found");
            }

            db.Programs.Remove(program);
            await db.SaveChangesAsync();
            await notifications.PublishProgramAsync(Operations.Delete, program);

            return Results.Json(program, Core.OpenAdrJson.Options);
        });
    }

    /// <summary>Every program always carries a PROGRAM_NAME target equal to its own name,
    /// per the interworking profile — enforced server-side so clients can't omit it.</summary>
    internal static List<string> WithProgramNameTarget(List<string>? targets, string programName)
    {
        var result = new List<string>(targets ?? []);
        var tag = TargetNames.Of(TargetNames.ProgramName, programName);
        if (!result.Contains(tag))
        {
            result.Add(tag);
        }

        return result;
    }
}
