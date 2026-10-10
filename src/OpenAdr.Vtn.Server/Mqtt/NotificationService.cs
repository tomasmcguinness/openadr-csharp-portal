using Microsoft.EntityFrameworkCore;
using OpenAdr.Vtn.Core.Logic;
using OpenAdr.Vtn.Core.Models;
using OpenAdr.Vtn.Server.Data;

namespace OpenAdr.Vtn.Server.Mqtt;

/// <summary>Publishes the MQTT notification for a create/update/delete of a subscribable
/// object to every topic that scheme applies to, built from the single MqttTopics source of
/// truth so these publishes can never name a topic the discovery endpoints didn't advertise.</summary>
public class NotificationService(IMqttPublisher publisher, VtnDbContext db)
{
    public Task PublishProgramAsync(string operation, ProgramDto program) =>
        PublishTo(operation, program, ObjectTypes.Program, program.Targets,
            MqttTopics.Programs(),
            operation == Operations.Create ? null : MqttTopics.Program(program.Id));

    public async Task PublishEventAsync(string operation, EventDto evt)
    {
        var topicSets = new List<NotifierOperationsTopics>
        {
            MqttTopics.Events(),
            MqttTopics.ProgramEvents(evt.ProgramId),
        };

        foreach (var venId in await ResolveTargetedVenIdsAsync(evt.Targets))
        {
            topicSets.Add(MqttTopics.VenEvents(venId));
        }

        await PublishTo(operation, evt, ObjectTypes.Event, evt.Targets, topicSets.ToArray());
    }

    public Task PublishVenAsync(string operation, VenDto ven) =>
        PublishTo(operation, ven, ObjectTypes.Ven, ven.Targets,
            MqttTopics.VensAll(),
            operation == Operations.Create ? null : MqttTopics.Ven(ven.Id));

    public Task PublishReportAsync(string operation, ReportDto report) =>
        PublishTo(operation, report, ObjectTypes.Report, null, MqttTopics.Reports());

    private async Task<List<string>> ResolveTargetedVenIdsAsync(List<string>? targets)
    {
        var venNames = targets?
            .Where(t => t.StartsWith(TargetNames.VenName + ":", StringComparison.Ordinal))
            .Select(t => t[(TargetNames.VenName.Length + 1)..])
            .ToList();

        if (venNames is null || venNames.Count == 0)
        {
            return [];
        }

        return await db.Vens.Where(v => venNames.Contains(v.VenName)).Select(v => v.Id).ToListAsync();
    }

    private async Task PublishTo(
        string operation,
        object subject,
        string objectType,
        List<string>? targets,
        params NotifierOperationsTopics?[] topicSets)
    {
        var notification = new Notification
        {
            ObjectType = objectType,
            Operation = operation,
            Object = subject,
            Targets = targets,
        };

        foreach (var topics in topicSets)
        {
            if (topics is null)
            {
                continue;
            }

            var topic = MqttTopics.TopicFor(topics, operation);
            if (topic is not null)
            {
                await publisher.PublishAsync(topic, notification);
            }
        }
    }
}
