using OpenAdr.Vtn.Core.Logic;
using OpenAdr.Vtn.Core.Models;

namespace OpenAdr.Vtn.Core.Tests;

public class ReportValidatorTests
{
    private static ReportRequest ValidForecastReport(int intervalCount = 24, string intervalDuration = "PT1H") =>
        new()
        {
            EventId = "event-1",
            ClientName = "ven-kitchen",
            Resources =
            [
                new ReportResource
                {
                    ResourceName = "AGGREGATED_REPORT",
                    IntervalPeriod = new IntervalPeriod { Start = DateTimeOffset.UtcNow, Duration = intervalDuration },
                    Intervals = Enumerable.Range(0, intervalCount)
                        .Select(i => new Interval
                        {
                            Id = i,
                            Payloads = [new ValuesMap { Type = "FORECAST", Values = [1.23] }],
                        })
                        .ToList(),
                },
            ],
        };

    [Fact]
    public void Accepts_a_well_formed_24_interval_forecast_report()
    {
        var result = ReportValidator.Validate(ValidForecastReport());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(23)] // DST spring-forward day
    [InlineData(25)] // DST fall-back day
    [InlineData(1)]  // a single actual/direct-read interval
    public void Accepts_variable_length_interval_counts(int intervalCount)
    {
        var result = ReportValidator.Validate(ValidForecastReport(intervalCount));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Rejects_a_report_with_no_resources()
    {
        var report = ValidForecastReport();
        report.Resources.Clear();

        var result = ReportValidator.Validate(report);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("resources"));
    }

    [Fact]
    public void Rejects_a_resource_with_no_intervals()
    {
        var report = ValidForecastReport();
        report.Resources[0].Intervals.Clear();

        var result = ReportValidator.Validate(report);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("intervals"));
    }

    [Fact]
    public void Rejects_an_interval_with_no_payloads()
    {
        var report = ValidForecastReport(intervalCount: 1);
        report.Resources[0].Intervals[0].Payloads.Clear();

        var result = ReportValidator.Validate(report);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("payloads"));
    }

    [Fact]
    public void Rejects_a_payload_with_no_values()
    {
        var report = ValidForecastReport(intervalCount: 1);
        report.Resources[0].Intervals[0].Payloads[0].Values.Clear();

        var result = ReportValidator.Validate(report);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("values"));
    }

    [Fact]
    public void Rejects_a_report_missing_eventId_or_clientName()
    {
        var report = ValidForecastReport();
        report.EventId = "";
        report.ClientName = "";

        var result = ReportValidator.Validate(report);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("eventID"));
        Assert.Contains(result.Errors, e => e.Contains("clientName"));
    }
}
