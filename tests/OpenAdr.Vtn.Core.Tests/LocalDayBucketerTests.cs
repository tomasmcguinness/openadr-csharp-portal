using OpenAdr.Vtn.Core.Logic;

namespace OpenAdr.Vtn.Core.Tests;

public class LocalDayBucketerTests
{
    private static readonly TimeZoneInfo London = LocalDayBucketer.EuropeLondon;

    [Fact]
    public void A_normal_day_has_24_hourly_buckets()
    {
        var buckets = LocalDayBucketer.GetHourBuckets(new DateOnly(2026, 6, 15), London);

        Assert.Equal(24, buckets.Count);
        Assert.Equal("00:00", buckets[0].LocalLabel);
        Assert.Equal("23:00", buckets[23].LocalLabel);
    }

    [Fact]
    public void Spring_forward_day_has_23_hourly_buckets()
    {
        // UK clocks jump 00:59 BST... actually GMT->BST: 00:59 GMT -> 02:00 BST on 2026-03-29.
        var buckets = LocalDayBucketer.GetHourBuckets(new DateOnly(2026, 3, 29), London);

        Assert.Equal(23, buckets.Count);

        // The 01:00 local hour never happens that day — clocks jump straight from 01:00 to 02:00.
        Assert.DoesNotContain(buckets, b => b.LocalLabel == "01:00");
        Assert.Contains(buckets, b => b.LocalLabel == "00:00");
        Assert.Contains(buckets, b => b.LocalLabel == "02:00");
    }

    [Fact]
    public void Fall_back_day_has_25_hourly_buckets()
    {
        // UK clocks repeat 01:00-02:00 on 2026-10-25 (BST->GMT at 02:00 BST -> 01:00 GMT).
        var buckets = LocalDayBucketer.GetHourBuckets(new DateOnly(2026, 10, 25), London);

        Assert.Equal(25, buckets.Count);

        // The 01:00 local hour is ambiguous and appears for two different UTC buckets.
        var oneAmBuckets = buckets.Where(b => b.LocalLabel == "01:00").ToList();
        Assert.Equal(2, oneAmBuckets.Count);
        Assert.NotEqual(oneAmBuckets[0].UtcStart, oneAmBuckets[1].UtcStart);
    }

    [Fact]
    public void Buckets_are_contiguous_and_span_exactly_one_local_day()
    {
        var buckets = LocalDayBucketer.GetHourBuckets(new DateOnly(2026, 10, 25), London);

        for (var i = 1; i < buckets.Count; i++)
        {
            Assert.Equal(buckets[i - 1].UtcEnd, buckets[i].UtcStart);
        }

        Assert.Equal(TimeSpan.FromHours(1), buckets[0].UtcEnd - buckets[0].UtcStart);
    }

    [Fact]
    public void FindBucketIndex_locates_the_containing_bucket()
    {
        var buckets = LocalDayBucketer.GetHourBuckets(new DateOnly(2026, 6, 15), London);
        var noonUtc = buckets[12].UtcStart.AddMinutes(30);

        Assert.Equal(12, LocalDayBucketer.FindBucketIndex(buckets, noonUtc));
        Assert.Equal(-1, LocalDayBucketer.FindBucketIndex(buckets, buckets[^1].UtcEnd));
    }

    [Fact]
    public void GetLocalDate_round_trips_through_each_bucket_on_a_dst_day()
    {
        var day = new DateOnly(2026, 3, 29);
        var buckets = LocalDayBucketer.GetHourBuckets(day, London);

        foreach (var bucket in buckets)
        {
            Assert.Equal(day, LocalDayBucketer.GetLocalDate(bucket.UtcStart, London));
        }
    }
}
