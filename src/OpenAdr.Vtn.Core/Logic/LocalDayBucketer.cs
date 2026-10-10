namespace OpenAdr.Vtn.Core.Logic;

/// <summary>One hourly slot in a local-time chart, labelled by the wall-clock hour it starts
/// at in the target time zone. Boundaries are UTC instants; the label is purely cosmetic.</summary>
public record HourBucket(int Index, DateTimeOffset UtcStart, DateTimeOffset UtcEnd, string LocalLabel);

/// <summary>Buckets instants into local-calendar-day, local-hour slots, in a timezone-DST-safe
/// way. A "day" is defined by local midnight to the next local midnight, converted to UTC —
/// so a spring-forward day in Europe/London yields 23 buckets and a fall-back day yields 25,
/// with no special-casing: the bucket count simply falls out of the UTC span between the two
/// local midnights. Everything is stored/compared in UTC; only the label is local.</summary>
public static class LocalDayBucketer
{
    public static TimeZoneInfo EuropeLondon { get; } = TimeZoneInfo.FindSystemTimeZoneById("Europe/London");

    /// <summary>The local calendar date (in <paramref name="timeZone"/>) that a UTC instant falls on.</summary>
    public static DateOnly GetLocalDate(DateTimeOffset utcInstant, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTime(utcInstant, timeZone);
        return DateOnly.FromDateTime(local.DateTime);
    }

    /// <summary>All hourly buckets for one local calendar day. Count is 23/24/25 depending on
    /// DST transitions on that date.</summary>
    public static IReadOnlyList<HourBucket> GetHourBuckets(DateOnly localDate, TimeZoneInfo timeZone)
    {
        var localMidnight = new DateTime(localDate.Year, localDate.Month, localDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var nextLocalMidnight = localMidnight.AddDays(1);

        var utcStart = ToUtcInstant(localMidnight, timeZone);
        var utcEnd = ToUtcInstant(nextLocalMidnight, timeZone);

        var hourCount = (int)Math.Round((utcEnd - utcStart).TotalHours);
        var buckets = new List<HourBucket>(hourCount);

        for (var i = 0; i < hourCount; i++)
        {
            var bucketUtcStart = utcStart.AddHours(i);
            var bucketUtcEnd = bucketUtcStart.AddHours(1);
            var localLabel = TimeZoneInfo.ConvertTime(bucketUtcStart, timeZone).ToString("HH:mm");
            buckets.Add(new HourBucket(i, bucketUtcStart, bucketUtcEnd, localLabel));
        }

        return buckets;
    }

    /// <summary>The index of the bucket containing <paramref name="utcInstant"/>, or -1 if it
    /// falls outside every bucket in the list.</summary>
    public static int FindBucketIndex(IReadOnlyList<HourBucket> buckets, DateTimeOffset utcInstant)
    {
        for (var i = 0; i < buckets.Count; i++)
        {
            if (utcInstant >= buckets[i].UtcStart && utcInstant < buckets[i].UtcEnd)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Converts an unspecified-kind local wall-clock DateTime to its UTC instant.
    /// Europe/London never makes local midnight itself ambiguous or invalid (DST transitions
    /// happen at 01:00/02:00 local), so a plain ConvertTimeToUtc is sufficient here.</summary>
    private static DateTimeOffset ToUtcInstant(DateTime unspecifiedLocal, TimeZoneInfo timeZone)
    {
        var utc = TimeZoneInfo.ConvertTimeToUtc(unspecifiedLocal, timeZone);
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }
}
