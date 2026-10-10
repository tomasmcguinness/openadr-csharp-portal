using Microsoft.EntityFrameworkCore;
using OpenAdr.Vtn.Core.Logic;
using OpenAdr.Vtn.Core.Models;
using OpenAdr.Vtn.Server.Auth;
using OpenAdr.Vtn.Server.Data;
using OpenAdr.Vtn.Server.Mqtt;

namespace OpenAdr.Vtn.Server.Endpoints;

public static class VensEndpoints
{
    public static void MapVensEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/vens").AddEndpointFilter<BearerAuthFilter>();

        group.MapGet("/", async (VtnDbContext db, string? venName, string[]? targets, int skip = 0, int? limit = null) =>
        {
            var query = db.Vens.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(venName))
            {
                query = query.Where(v => v.VenName == venName);
            }

            var vens = await query.ToListAsync();
            var filtered = vens.Where(v => TargetMatcher.Matches(v.Targets, targets)).Skip(skip);
            if (limit is { } take)
            {
                filtered = filtered.Take(take);
            }

            return Results.Json(filtered.ToList(), Core.OpenAdrJson.Options);
        });

        group.MapGet("/{venId}", async (string venId, VtnDbContext db) =>
        {
            var ven = await db.Vens.AsNoTracking().FirstOrDefaultAsync(v => v.Id == venId);
            return ven is null
                ? Results.Problem(statusCode: 404, title: "Not Found")
                : Results.Json(ven, Core.OpenAdrJson.Options);
        });

        group.MapPost("/", async (VenRequest request, HttpContext http, VtnDbContext db, NotificationService notifications) =>
        {
            var clientId = BearerAuthFilter.GetClientId(http);

            if (await db.Vens.AnyAsync(v => v.VenName == request.VenName))
            {
                return Results.Problem(detail: $"A ven named '{request.VenName}' already exists.", statusCode: 409, title: "Conflict");
            }

            var now = DateTimeOffset.UtcNow;
            var ven = new VenDto
            {
                Id = IdGenerator.New("ven"),
                ObjectType = ObjectTypes.Ven,
                CreatedDateTime = now,
                ModificationDateTime = now,
                ClientId = clientId,
                VenName = request.VenName,
                Attributes = request.Attributes,
                Targets =
                [
                    TargetNames.Of(TargetNames.VenName, request.VenName),
                    TargetNames.Of(TargetNames.ProgramName, SeedData.ProgramName),
                    .. request.Targets ?? [],
                ],
            };

            db.Vens.Add(ven);
            await db.SaveChangesAsync();
            await notifications.PublishVenAsync(Operations.Create, ven);

            return Results.Json(ven, Core.OpenAdrJson.Options, statusCode: 201);
        });

        group.MapPut("/{venId}", async (string venId, VenRequest request, VtnDbContext db, NotificationService notifications) =>
        {
            var ven = await db.Vens.FirstOrDefaultAsync(v => v.Id == venId);
            if (ven is null)
            {
                return Results.Problem(statusCode: 404, title: "Not Found");
            }

            ven.VenName = request.VenName;
            ven.Attributes = request.Attributes;
            ven.Targets = request.Targets ?? ven.Targets;
            ven.ModificationDateTime = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync();
            await notifications.PublishVenAsync(Operations.Update, ven);

            return Results.Json(ven, Core.OpenAdrJson.Options);
        });

        group.MapDelete("/{venId}", async (string venId, VtnDbContext db, NotificationService notifications) =>
        {
            var ven = await db.Vens.FirstOrDefaultAsync(v => v.Id == venId);
            if (ven is null)
            {
                return Results.Problem(statusCode: 404, title: "Not Found");
            }

            db.Vens.Remove(ven);
            await db.SaveChangesAsync();
            await notifications.PublishVenAsync(Operations.Delete, ven);

            return Results.Json(ven, Core.OpenAdrJson.Options);
        });
    }
}
