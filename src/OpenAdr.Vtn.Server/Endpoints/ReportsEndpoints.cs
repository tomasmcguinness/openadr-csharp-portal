using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OpenAdr.Vtn.Core.Logic;
using OpenAdr.Vtn.Core.Models;
using OpenAdr.Vtn.Server.Auth;
using OpenAdr.Vtn.Server.Data;
using OpenAdr.Vtn.Server.Hubs;
using OpenAdr.Vtn.Server.Mqtt;

namespace OpenAdr.Vtn.Server.Endpoints;

public static class ReportsEndpoints
{
    public static void MapReportsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/reports").AddEndpointFilter<BearerAuthFilter>();

        group.MapGet("/", async (VtnDbContext db, string? programId, string? eventId, string? clientName, int skip = 0, int? limit = null) =>
        {
            var query = db.Reports.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(programId))
            {
                var eventIdsInProgram = await db.Events.Where(e => e.ProgramId == programId).Select(e => e.Id).ToListAsync();
                query = query.Where(r => eventIdsInProgram.Contains(r.EventId));
            }

            if (!string.IsNullOrWhiteSpace(eventId))
            {
                query = query.Where(r => r.EventId == eventId);
            }

            if (!string.IsNullOrWhiteSpace(clientName))
            {
                query = query.Where(r => r.ClientName == clientName);
            }

            // SQLite's EF provider can't translate DateTimeOffset ordering to SQL, so the
            // key-based filters above run server-side and the CreatedDateTime ordering runs
            // in memory — fine at this demo's scale.
            var results = (await query.ToListAsync()).OrderByDescending(r => r.CreatedDateTime).Skip(skip);
            if (limit is { } take)
            {
                results = results.Take(take);
            }

            return Results.Json(results.ToList(), Core.OpenAdrJson.Options);
        });

        group.MapGet("/{reportId}", async (string reportId, VtnDbContext db) =>
        {
            var report = await db.Reports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == reportId);
            return report is null
                ? Results.Problem(statusCode: 404, title: "Not Found")
                : Results.Json(report, Core.OpenAdrJson.Options);
        });

        group.MapPost("/", async (
            ReportRequest request,
            HttpContext http,
            VtnDbContext db,
            NotificationService notifications,
            IHubContext<DashboardHub, IDashboardClient> dashboard) =>
        {
            var validation = ReportValidator.Validate(request);
            if (!validation.IsValid)
            {
                return Results.Problem(detail: string.Join(" ", validation.Errors), statusCode: 400, title: "Bad Request");
            }

            var now = DateTimeOffset.UtcNow;
            var report = new ReportDto
            {
                Id = IdGenerator.New("report"),
                ObjectType = ObjectTypes.Report,
                CreatedDateTime = now,
                ModificationDateTime = now,
                ClientId = BearerAuthFilter.GetClientId(http),
                EventId = request.EventId,
                ClientName = request.ClientName,
                ReportName = request.ReportName,
                PayloadDescriptors = request.PayloadDescriptors,
                Resources = request.Resources,
            };

            // Every POST inserts a new row rather than upserting by (eventId, clientName) —
            // that is what keeps the full history of forecast versions for the dashboard.
            db.Reports.Add(report);
            await db.SaveChangesAsync();
            await notifications.PublishReportAsync(Operations.Create, report);
            await dashboard.Clients.All.ReportReceived(report);

            return Results.Json(report, Core.OpenAdrJson.Options, statusCode: 201);
        });

        group.MapPut("/{reportId}", async (string reportId, ReportRequest request, VtnDbContext db, NotificationService notifications) =>
        {
            var validation = ReportValidator.Validate(request);
            if (!validation.IsValid)
            {
                return Results.Problem(detail: string.Join(" ", validation.Errors), statusCode: 400, title: "Bad Request");
            }

            var report = await db.Reports.FirstOrDefaultAsync(r => r.Id == reportId);
            if (report is null)
            {
                return Results.Problem(statusCode: 404, title: "Not Found");
            }

            report.EventId = request.EventId;
            report.ClientName = request.ClientName;
            report.ReportName = request.ReportName;
            report.PayloadDescriptors = request.PayloadDescriptors;
            report.Resources = request.Resources;
            report.ModificationDateTime = DateTimeOffset.UtcNow;

            await db.SaveChangesAsync();
            await notifications.PublishReportAsync(Operations.Update, report);

            return Results.Json(report, Core.OpenAdrJson.Options);
        });

        group.MapDelete("/{reportId}", async (string reportId, VtnDbContext db, NotificationService notifications) =>
        {
            var report = await db.Reports.FirstOrDefaultAsync(r => r.Id == reportId);
            if (report is null)
            {
                return Results.Problem(statusCode: 404, title: "Not Found");
            }

            db.Reports.Remove(report);
            await db.SaveChangesAsync();
            await notifications.PublishReportAsync(Operations.Delete, report);

            return Results.Json(report, Core.OpenAdrJson.Options);
        });
    }
}
