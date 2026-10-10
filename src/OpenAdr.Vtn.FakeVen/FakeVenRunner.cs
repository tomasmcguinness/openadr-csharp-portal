using System.Net;
using OpenAdr.Vtn.Core.Logic;
using OpenAdr.Vtn.Core.Models;

namespace OpenAdr.Vtn.FakeVen;

/// <summary>Drives the exact HTTP + MQTT sequence described in docs/ven-flow.md: get a token,
/// find-or-create the ven, discover notifiers and subscribe to MQTT topics, discover the
/// program and forecast event, then post an hourly (or, with --fast, 10-second) forecast +
/// actuals report — recovering on its own from an expired token, a deleted/missing object, or
/// a dropped MQTT connection.</summary>
public class FakeVenRunner(VtnClient api, MqttClientWrapper mqtt, string venName, bool fast)
{
    private readonly ForecastSimulator _simulator = new();
    private string _venId = "";
    private string _programId = "";
    private string _forecastEventId = "";

    public async Task RunAsync(CancellationToken ct)
    {
        mqtt.Reconnected += async reconnectCt =>
        {
            Logger.Info("Resyncing ven/program/event via REST after MQTT reconnect.");
            await EnsureVenAsync(reconnectCt);
            await DiscoverProgramAndEventAsync(reconnectCt);
        };

        // The VTN itself can be unreachable — at cold start, or because it's mid-restart.
        // Any failure anywhere in setup-or-loop below (not just the 404 case the inner catch
        // handles) falls out here and the whole sequence — token, ven, MQTT, discovery — is
        // simply retried from scratch after a short wait, same as a real cold start.
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ConnectAndLoopAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Normal shutdown.
            }
            catch (Exception ex)
            {
                Logger.Info($"VTN unreachable or an unexpected error occurred ({ex.Message}) — retrying in 5s.");
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
    }

    private async Task ConnectAndLoopAsync(CancellationToken ct)
    {
        await api.FetchTokenAsync(ct);
        await EnsureVenAsync(ct);
        await ConnectAndSubscribeAsync(ct);
        await DiscoverProgramAndEventAsync(ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await SendHourlyReportsAsync(ct);
            }
            catch (VtnNotFoundException ex)
            {
                Logger.Info($"{ex.Message} — rediscovering ven/program/event.");
                await EnsureVenAsync(ct);
                await DiscoverProgramAndEventAsync(ct);
            }

            await Task.Delay(fast ? TimeSpan.FromSeconds(10) : TimeUntilNextHour(), ct);
        }
    }

    private static TimeSpan TimeUntilNextHour()
    {
        var now = DateTimeOffset.UtcNow;
        var nextHour = now.Date.AddHours(now.Hour + 1);
        return nextHour - now;
    }

    private async Task EnsureVenAsync(CancellationToken ct)
    {
        var found = await api.GetAsync($"/vens?venName={Uri.EscapeDataString(venName)}", ct);
        var existing = found.IsSuccess ? found.Json<List<VenDto>>() : [];

        if (existing.Count > 0)
        {
            _venId = existing[0].Id;
            Logger.Info($"Found existing ven '{venName}' ({_venId}).");
            return;
        }

        var created = await api.PostAsync("/vens", new VenRequest { ObjectType = "VEN_VEN_REQUEST", VenName = venName }, ct);
        if (!created.IsSuccess)
        {
            throw new InvalidOperationException($"Could not create ven '{venName}': {created.StatusCode}");
        }

        _venId = created.Json<VenDto>().Id;
        Logger.Info($"Created ven '{venName}' ({_venId}).");
    }

    private async Task ConnectAndSubscribeAsync(CancellationToken ct)
    {
        var notifiers = await api.GetAsync("/notifiers", ct);
        if (notifiers.IsSuccess && notifiers.Json<NotifiersResponse>().Mqtt is { } mqttBinding)
        {
            Logger.Info($"VTN advertises MQTT at {string.Join(", ", mqttBinding.Uris)} (auth: {mqttBinding.Authentication.Method}).");
        }

        await mqtt.ConnectAsync(ct);
        await SubscribeVenScopedTopicsAsync(ct);
    }

    private async Task SubscribeVenScopedTopicsAsync(CancellationToken ct)
    {
        await SubscribeAllTopicAsync($"/notifiers/mqtt/topics/vens/{_venId}/events", ct);
        await SubscribeAllTopicAsync($"/notifiers/mqtt/topics/vens/{_venId}/programs", ct);
    }

    private async Task SubscribeAllTopicAsync(string discoveryPath, CancellationToken ct)
    {
        var response = await api.GetAsync(discoveryPath, ct);
        if (!response.IsSuccess)
        {
            Logger.Info($"Could not discover topics at {discoveryPath}: {response.StatusCode}");
            return;
        }

        var topics = response.Json<NotifierTopicsResponse>().Topics;
        if (topics.All is { } allTopic)
        {
            await mqtt.SubscribeAsync(allTopic, ct);
        }
    }

    private async Task DiscoverProgramAndEventAsync(CancellationToken ct)
    {
        var programTarget = TargetNames.Of(TargetNames.ProgramName, "HomeForecast");
        var programsResponse = await api.GetAsync($"/programs?targets={Uri.EscapeDataString(programTarget)}", ct);
        var programs = programsResponse.IsSuccess ? programsResponse.Json<List<ProgramDto>>() : [];

        if (programs.Count == 0)
        {
            throw new InvalidOperationException("No HomeForecast program found on the VTN.");
        }

        _programId = programs[0].Id;
        await SubscribeAllTopicAsync($"/notifiers/mqtt/topics/programs/{_programId}/events", ct);

        var eventsResponse = await api.GetAsync($"/events?programId={_programId}", ct);
        var events = eventsResponse.IsSuccess ? eventsResponse.Json<List<EventDto>>() : [];

        var forecastEvent = events.FirstOrDefault(e =>
            e.ReportDescriptors?.Any(d => d.PayloadType == "FORECAST") ?? false);

        if (forecastEvent is null)
        {
            throw new InvalidOperationException("No event with a FORECAST reportDescriptor found under the HomeForecast program.");
        }

        _forecastEventId = forecastEvent.Id;
        Logger.Info($"Discovered program '{_programId}' and forecast event '{_forecastEventId}'.");
    }

    private async Task SendHourlyReportsAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var hourStart = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero);
        var localNow = TimeZoneInfo.ConvertTime(hourStart, LocalDayBucketer.EuropeLondon);

        _simulator.NextTick();
        await PostForecastReportAsync(hourStart, localNow.Hour, ct);
        await PostActualReportAsync(hourStart.AddHours(-1), (localNow.Hour - 1 + 24) % 24, ct);
    }

    private async Task PostForecastReportAsync(DateTimeOffset hourStart, int startLocalHour, CancellationToken ct)
    {
        var values = _simulator.ForecastKw(startLocalHour, 24);

        var intervals = Enumerable.Range(0, 24).Select(i => new Interval
        {
            Id = i,
            IntervalPeriod = new IntervalPeriod { Start = hourStart.AddHours(i), Duration = "PT1H" },
            Payloads = [new ValuesMap { Type = "FORECAST", Values = [values[i]] }],
        }).ToList();

        var report = new ReportRequest
        {
            EventId = _forecastEventId,
            ClientName = venName,
            ReportName = $"Forecast {hourStart:yyyy-MM-ddTHH}",
            PayloadDescriptors = [new ReportPayloadDescriptor { PayloadType = "FORECAST", ReadingType = "DEMAND", Units = "KW" }],
            Resources = [new ReportResource { ResourceName = "AGGREGATED_REPORT", Intervals = intervals }],
        };

        await PostReportOrThrowAsync(report, ct);
    }

    private async Task PostActualReportAsync(DateTimeOffset completedHourStart, int completedLocalHour, CancellationToken ct)
    {
        var value = _simulator.ActualKw(completedLocalHour);

        var report = new ReportRequest
        {
            EventId = _forecastEventId,
            ClientName = venName,
            ReportName = $"Actual {completedHourStart:yyyy-MM-ddTHH}",
            PayloadDescriptors = [new ReportPayloadDescriptor { PayloadType = "DIRECT_READ", ReadingType = "DEMAND", Units = "KW" }],
            Resources =
            [
                new ReportResource
                {
                    ResourceName = "AGGREGATED_REPORT",
                    Intervals =
                    [
                        new Interval
                        {
                            Id = 0,
                            IntervalPeriod = new IntervalPeriod { Start = completedHourStart, Duration = "PT1H" },
                            Payloads = [new ValuesMap { Type = "DIRECT_READ", Values = [value] }],
                        },
                    ],
                },
            ],
        };

        await PostReportOrThrowAsync(report, ct);
    }

    private async Task PostReportOrThrowAsync(ReportRequest report, CancellationToken ct)
    {
        var response = await api.PostAsync("/reports", report, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new VtnNotFoundException($"POST /reports returned 404 for event '{report.EventId}'");
        }

        if (!response.IsSuccess)
        {
            Logger.Info($"POST /reports failed unexpectedly: {response.StatusCode}");
        }
    }
}

public class VtnNotFoundException(string message) : Exception(message);
