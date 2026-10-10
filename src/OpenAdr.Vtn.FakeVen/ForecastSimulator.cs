namespace OpenAdr.Vtn.FakeVen;

/// <summary>Generates a synthetic whole-home demand curve: a plausible day shape (low
/// overnight, breakfast and evening peaks) plus random jitter, with an optional slow drift
/// term so repeated --fast ticks visibly move the forecast for demo purposes.</summary>
public class ForecastSimulator(int? seed = null)
{
    private readonly Random _random = seed is { } s ? new Random(s) : new Random();
    private int _tick;

    // kW at each local hour of a typical day, index 0 = 00:00.
    private static readonly double[] BaseProfileKw =
    [
        0.3, 0.3, 0.25, 0.25, 0.3, 0.5, 1.2, 1.8,
        1.5, 0.9, 0.7, 0.6, 0.7, 0.6, 0.6, 0.7,
        1.0, 1.6, 2.4, 2.5, 2.0, 1.3, 0.8, 0.4,
    ];

    /// <summary>Call once per tick (not per value) so the drift term advances once per report,
    /// not once per interval within it.</summary>
    public void NextTick() => _tick++;

    /// <summary>One forecast value per hour starting at local hour <paramref name="startLocalHour"/>,
    /// wrapping through the 24-hour base profile for as many hours as requested (23/24/25 all
    /// work fine — a DST day just means the wrap happens slightly earlier/later).</summary>
    public double[] ForecastKw(int startLocalHour, int hourCount)
    {
        var values = new double[hourCount];
        for (var i = 0; i < hourCount; i++)
        {
            values[i] = ForecastValueForHour(startLocalHour + i);
        }

        return values;
    }

    /// <summary>A plausible "actual" reading for the given completed local hour: a noisy
    /// variant of what the forecast for that hour would have been.</summary>
    public double ActualKw(int completedLocalHour)
    {
        var forecast = ForecastValueForHour(completedLocalHour);
        var noise = (_random.NextDouble() - 0.5) * 0.2 * forecast;
        return Math.Round(Math.Max(0, forecast + noise), 3);
    }

    private double ForecastValueForHour(int localHour)
    {
        var drift = 0.3 * Math.Sin(_tick * 0.1);
        var index = ((localHour % 24) + 24) % 24;
        var baseValue = BaseProfileKw[index];
        var jitter = (_random.NextDouble() - 0.5) * 0.1 * baseValue;
        return Math.Round(Math.Max(0, baseValue + drift + jitter), 3);
    }
}
