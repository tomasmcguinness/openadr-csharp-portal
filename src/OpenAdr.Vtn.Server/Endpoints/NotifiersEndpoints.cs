using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenAdr.Vtn.Core.Logic;
using OpenAdr.Vtn.Core.Models;
using OpenAdr.Vtn.Server.Auth;
using OpenAdr.Vtn.Server.Data;

namespace OpenAdr.Vtn.Server.Endpoints;

public static class NotifiersEndpoints
{
    public static void MapNotifiersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/notifiers").AddEndpointFilter<BearerAuthFilter>();

        group.MapGet("/", (HttpContext http, IOptions<VtnOptions> options) =>
        {
            var response = new NotifiersResponse
            {
                Webhook = true,
                Mqtt = new MqttNotifierBindingObject
                {
                    Uris = [$"mqtt://{http.Request.Host.Host}:{options.Value.MqttPort}"],
                    Serialization = "JSON",
                    Authentication = new MqttNotifierAuthentication { Method = "ANONYMOUS" },
                },
            };

            return Results.Json(response, Core.OpenAdrJson.Options);
        });

        group.MapGet("/mqtt/topics/programs", () => Topics(MqttTopics.Programs()));

        group.MapGet("/mqtt/topics/programs/{programId}", async (string programId, VtnDbContext db) =>
            await Exists(db.Programs, programId) ? Topics(MqttTopics.Program(programId)) : NotFound());

        group.MapGet("/mqtt/topics/events", () => Topics(MqttTopics.Events()));

        group.MapGet("/mqtt/topics/programs/{programId}/events", async (string programId, VtnDbContext db) =>
            await Exists(db.Programs, programId) ? Topics(MqttTopics.ProgramEvents(programId)) : NotFound());

        group.MapGet("/mqtt/topics/vens", () => Topics(MqttTopics.VensAll()));

        group.MapGet("/mqtt/topics/vens/{venId}", async (string venId, VtnDbContext db) =>
            await Exists(db.Vens, venId) ? Topics(MqttTopics.Ven(venId)) : NotFound());

        group.MapGet("/mqtt/topics/vens/{venId}/events", async (string venId, VtnDbContext db) =>
            await Exists(db.Vens, venId) ? Topics(MqttTopics.VenEvents(venId)) : NotFound());

        group.MapGet("/mqtt/topics/vens/{venId}/programs", async (string venId, VtnDbContext db) =>
            await Exists(db.Vens, venId) ? Topics(MqttTopics.VenPrograms(venId)) : NotFound());

        group.MapGet("/mqtt/topics/reports", () => Topics(MqttTopics.Reports()));
    }

    private static async Task<bool> Exists<T>(IQueryable<T> set, string id) where T : ObjectMetadata =>
        await set.AnyAsync(x => x.Id == id);

    private static IResult Topics(NotifierOperationsTopics topics) =>
        Results.Json(new NotifierTopicsResponse { Topics = topics }, Core.OpenAdrJson.Options);

    private static IResult NotFound() => Results.Problem(statusCode: 404, title: "Not Found");
}
