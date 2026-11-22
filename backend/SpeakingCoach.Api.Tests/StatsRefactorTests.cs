namespace SpeakingCoach.Api.Tests;

/// <summary>
/// /api/review/stats tezlashtirildi (kam so'rov, kam qator): yangi hisob eski, "hamma tarix bo'yicha" hisob bilan
/// AYNAN bir xil natija berishi kerak — tasodifiy ma'lumotlar bilan solishtiriladi.
/// </summary>
public class StatsRefactorTests
{
    private static readonly int[] Windows = [30, 120, 800];
    private static readonly DateTime Now = new(2026, 11, 22, 17, 30, 0, DateTimeKind.Utc);

    private static async Task<int> Progressive(List<DateTime> all, int tz, int[]? windows = null)
    {
        var loads = new List<int>();
        var r = await ReviewScheduler.ComputeStreakProgressiveAsync(w =>
        {
            loads.Add(w);
            var since = Now.AddDays(-w);
            return Task.FromResult<IReadOnlyList<DateTime>>(all.Where(t => t >= since).ToList());
        }, Now, tz, windows ?? Windows);
        return r;
    }

    private static List<DateTime> Streak(int days, int tz, int skipRecent = 0, int perDay = 1, Random? rng = null)
    {
        rng ??= new Random(1);
        var list = new List<DateTime>();
        var today = ReviewScheduler.ToLocalDate(Now, tz);
        for (var d = skipRecent; d < skipRecent + days; d++)
        {
            var local = today.AddDays(-d).ToDateTime(new TimeOnly(rng.Next(0, 24), rng.Next(0, 60)), DateTimeKind.Utc);
            var utc = local.AddMinutes(tz);
            if (utc <= Now) list.Add(utc);
        }
        return list;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(28)]
    [InlineData(29)]
    [InlineData(30)]
    [InlineData(31)]
    [InlineData(118)]
    [InlineData(119)]
    [InlineData(120)]
    [InlineData(121)]
    [InlineData(250)]
    public async Task Streaks_around_every_window_edge_match_the_full_history_calculation(int days)
    {
        foreach (var tz in new[] { -840, -720, -300, -60, 0, 60, 330, 345, 540, 720 })
        {
            var data = Streak(days, tz);
            Assert.Equal(ReviewScheduler.ComputeStreak(data, Now, tz), await Progressive(data, tz));
        }
    }

    [Fact]
    public async Task A_streak_that_ended_yesterday_and_an_old_gap_are_handled()
    {
        foreach (var tz in new[] { -300, 0, 330 })
        {
            var yesterday = Streak(45, tz, skipRecent: 1);
            Assert.Equal(ReviewScheduler.ComputeStreak(yesterday, Now, tz), await Progressive(yesterday, tz));
            var gap = Streak(10, tz).Concat(Streak(100, tz, skipRecent: 12)).ToList();         // 2 kunlik bo'shliq
            Assert.Equal(10, await Progressive(gap, tz));
            Assert.Equal(ReviewScheduler.ComputeStreak(gap, Now, tz), await Progressive(gap, tz));
            Assert.Equal(0, await Progressive([], tz));
        }
    }

    [Fact]
    public async Task Random_histories_always_match()
    {
        var rng = new Random(20261122);
        for (var i = 0; i < 400; i++)
        {
            var tz = rng.Next(-14 * 60, 14 * 60 + 1);
            var data = new List<DateTime>();
            // Tasodifiy zanjirlar va bo'shliqlar, bir kunda bir necha faoliyat.
            var day = rng.Next(0, 3);
            var limit = rng.Next(1, 300);
            while (day < limit)
            {
                if (rng.NextDouble() < 0.93)
                {
                    for (var k = 0; k < rng.Next(1, 4); k++)
                    {
                        var t = Now.AddDays(-day).AddMinutes(-rng.Next(0, 1440));
                        data.Add(t);
                    }
                }
                day++;
            }
            Assert.Equal(ReviewScheduler.ComputeStreak(data, Now, tz), await Progressive(data, tz));
        }
    }

    [Fact]
    public async Task A_typical_short_streak_reads_only_the_small_window()
    {
        var loads = new List<int>();
        var data = Streak(5, -300);
        var streak = await ReviewScheduler.ComputeStreakProgressiveAsync(w => { loads.Add(w); return Task.FromResult<IReadOnlyList<DateTime>>(data); }, Now, -300, Windows);
        Assert.Equal(5, streak);
        Assert.Equal([30], loads);
    }

    [Fact]
    public async Task Windows_widen_only_when_the_streak_reaches_the_edge()
    {
        var loads = new List<int>();
        var data = Streak(125, 0);
        var streak = await ReviewScheduler.ComputeStreakProgressiveAsync(w => { loads.Add(w); return Task.FromResult<IReadOnlyList<DateTime>>(data); }, Now, 0, Windows);
        Assert.Equal(125, streak);
        Assert.Equal([30, 120, 800], loads);
    }

    // ---- Bugungi kun chegaralari ----

    [Fact]
    public void Local_day_bounds_select_exactly_the_events_of_todays_local_date()
    {
        var rng = new Random(7);
        for (var i = 0; i < 2000; i++)
        {
            var tz = rng.Next(-14 * 60, 14 * 60 + 1);
            var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(rng.Next(0, 60 * 24 * 365));
            var (start, end) = ReviewScheduler.LocalDayBoundsUtc(now, tz);
            Assert.Equal(TimeSpan.FromDays(1), end - start);
            Assert.True(now >= start && now < end);
            var today = ReviewScheduler.ToLocalDate(now, tz);
            for (var k = 0; k < 8; k++)
            {
                var t = now.AddMinutes(rng.Next(-3 * 24 * 60, 24 * 60));
                Assert.Equal(ReviewScheduler.ToLocalDate(t, tz) == today, t >= start && t < end);
            }
            // Chegaralarning o'zi: boshlanish — bugunga kiradi, tugash — ertaga.
            Assert.Equal(today, ReviewScheduler.ToLocalDate(start, tz));
            Assert.Equal(today.AddDays(1), ReviewScheduler.ToLocalDate(end, tz));
            Assert.Equal(today.AddDays(-1), ReviewScheduler.ToLocalDate(start.AddTicks(-1), tz));
        }
    }
}
