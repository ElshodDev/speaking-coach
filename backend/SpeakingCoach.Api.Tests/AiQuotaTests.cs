using Microsoft.Extensions.Configuration;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Tests;

public class AiQuotaTests
{
    private static AiQuotaOptions Options(Dictionary<string, string?>? values = null) =>
        new(new ConfigurationBuilder().AddInMemoryCollection(values ?? new()).Build());

    [Fact]
    public void Defaults_give_guests_less_than_users()
    {
        var o = Options();

        Assert.Equal(30, o.LimitFor(AiKind.Exercise, guest: false));
        Assert.Equal(100, o.LimitFor(AiKind.Word, guest: false));
        Assert.Equal(5, o.LimitFor(AiKind.Exercise, guest: true));
        Assert.Equal(20, o.LimitFor(AiKind.Word, guest: true));
    }

    [Fact]
    public void Limits_are_configurable_and_bad_values_fall_back()
    {
        var o = Options(new() { ["Ai:ExercisesPerDay"] = "50", ["Ai:GuestWordsPerDay"] = "oops", ["Ai:WordsPerDay"] = "-3" });

        Assert.Equal(50, o.Exercises);
        Assert.Equal(20, o.GuestWords);
        Assert.Equal(100, o.Words);
    }

    [Theory]
    [InlineData(0, 30, 30)]
    [InlineData(29, 30, 1)]
    [InlineData(30, 30, 0)]
    [InlineData(35, 30, 0)]
    public void Left_never_goes_below_zero(int used, int limit, int left)
    {
        Assert.Equal(left, new QuotaCounter(used, limit).Left);
    }

    [Fact]
    public void Guest_counters_are_per_ip_per_kind_and_per_day()
    {
        var store = new GuestQuotaStore();
        var today = new DateOnly(2026, 9, 26);

        store.Add("1.1.1.1", today, AiKind.Exercise);
        store.Add("1.1.1.1", today, AiKind.Exercise);
        store.Add("1.1.1.1", today, AiKind.Word);
        store.Add("2.2.2.2", today, AiKind.Exercise);

        Assert.Equal(2, store.Get("1.1.1.1", today, AiKind.Exercise));
        Assert.Equal(1, store.Get("1.1.1.1", today, AiKind.Word));
        Assert.Equal(1, store.Get("2.2.2.2", today, AiKind.Exercise));
        Assert.Equal(0, store.Get("1.1.1.1", today.AddDays(1), AiKind.Exercise));
    }

    [Fact]
    public void Old_days_are_pruned_when_a_new_day_starts()
    {
        var store = new GuestQuotaStore();
        var yesterday = new DateOnly(2026, 9, 25);

        store.Add("1.1.1.1", yesterday, AiKind.Exercise);
        store.Add("1.1.1.1", yesterday.AddDays(1), AiKind.Exercise);

        Assert.Equal(0, store.Get("1.1.1.1", yesterday, AiKind.Exercise));
    }

    [Fact]
    public void The_day_is_the_tashkent_date()
    {
        // Toshkent = UTC+5: 18:59 UTC — hali 26-sana, 19:00 UTC — Toshkentda yarim tun (27-sana).
        Assert.Equal(new DateOnly(2026, 9, 26), AiQuotaService.Today(new DateTime(2026, 9, 26, 18, 59, 0, DateTimeKind.Utc)));
        Assert.Equal(new DateOnly(2026, 9, 27), AiQuotaService.Today(new DateTime(2026, 9, 26, 19, 0, 0, DateTimeKind.Utc)));
        Assert.Equal(new DateOnly(2026, 9, 27), AiQuotaService.Today(new DateTime(2026, 9, 26, 23, 59, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void Talk_and_authoring_caps_have_defaults_and_are_configurable()
    {
        var o = Options();
        Assert.Equal(40, o.TalkTurns);
        Assert.Equal(10, o.Authoring);
        var c = Options(new() { ["Ai:TalkTurnsPerDay"] = "12", ["Ai:AuthoringPerDay"] = "3" });
        Assert.Equal(12, c.TalkTurns);
        Assert.Equal(3, c.Authoring);
    }

    [Fact]
    public void Daily_caps_take_up_to_the_limit_and_refund()
    {
        var caps = new AiDailyCaps();
        var day = new DateOnly(2026, 9, 26);
        var key = AiDailyCaps.TalkKey(Guid.NewGuid());

        Assert.True(caps.TryTake(key, day, 2));
        Assert.True(caps.TryTake(key, day, 2));
        Assert.False(caps.TryTake(key, day, 2));
        Assert.Equal(2, caps.Used(key, day));

        caps.Refund(key, day);
        Assert.True(caps.TryTake(key, day, 2));

        // Yangi kun — yangi hisob; boshqa foydalanuvchi — alohida.
        Assert.True(caps.TryTake(key, day.AddDays(1), 2));
        Assert.True(caps.TryTake(AiDailyCaps.TalkKey(Guid.NewGuid()), day, 2));
        // Refund noldan pastga tushirmaydi.
        var other = AiDailyCaps.AuthoringKey(Guid.NewGuid());
        caps.Refund(other, day);
        Assert.Equal(0, caps.Used(other, day));
    }

    [Fact]
    public void Daily_caps_are_safe_under_parallel_takes()
    {
        var caps = new AiDailyCaps();
        var day = new DateOnly(2026, 9, 26);
        var granted = 0;
        Parallel.For(0, 200, _ =>
        {
            if (caps.TryTake("talk:x", day, 40)) Interlocked.Increment(ref granted);
        });
        Assert.Equal(40, granted);
    }
}
