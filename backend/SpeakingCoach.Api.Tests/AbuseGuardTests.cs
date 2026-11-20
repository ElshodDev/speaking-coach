using Microsoft.AspNetCore.Http;
using SpeakingCoach.Api.Endpoints;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Tests;

/// <summary>
/// Suiiste'mollikka qarshi cheklovlar: parallel AI so'rovlari, muddati kelmagan
/// kartani baholash, fikr-mulohaza bildirishnomalari, email limiti, "write" IP chegarasi.
/// </summary>
public class AbuseGuardTests
{
    private static readonly DateOnly Day = new(2026, 11, 20);
    private static readonly DateTime Now = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);

    // ---- AiInFlight: "ishlatilgan + bajarilayotgan" limitdan oshmaydi ----

    [Fact]
    public void InFlight_blocks_parallel_requests_beyond_the_limit()
    {
        var f = new AiInFlight();
        // Limit 3, bugun 1 ta ishlatilgan: parallel ravishda faqat 2 tasi o'tadi.
        var a = f.TryReserve("u:1", Day, AiKind.Exercise, used: 1, limit: 3);
        var b = f.TryReserve("u:1", Day, AiKind.Exercise, used: 1, limit: 3);
        var c = f.TryReserve("u:1", Day, AiKind.Exercise, used: 1, limit: 3);

        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.Null(c);
        Assert.Equal(2, f.Count("u:1", Day, AiKind.Exercise));
    }

    [Fact]
    public void InFlight_releases_on_dispose_once()
    {
        var f = new AiInFlight();
        var a = f.TryReserve("u:1", Day, AiKind.Exercise, used: 0, limit: 1)!;
        Assert.Null(f.TryReserve("u:1", Day, AiKind.Exercise, used: 0, limit: 1));

        a.Dispose();
        a.Dispose(); // ikkinchi marta — boshqa joyni bo'shatib yubormasin
        Assert.Equal(0, f.Count("u:1", Day, AiKind.Exercise));
        Assert.NotNull(f.TryReserve("u:1", Day, AiKind.Exercise, used: 0, limit: 1));
    }

    [Fact]
    public void InFlight_keys_are_independent()
    {
        var f = new AiInFlight();
        Assert.NotNull(f.TryReserve("u:1", Day, AiKind.Exercise, 0, 1));
        Assert.NotNull(f.TryReserve("u:2", Day, AiKind.Exercise, 0, 1));
        Assert.NotNull(f.TryReserve("u:1", Day, AiKind.Word, 0, 1));
        Assert.NotNull(f.TryReserve("u:1", Day.AddDays(1), AiKind.Exercise, 0, 1));
    }

    [Fact]
    public void InFlight_is_safe_under_concurrency()
    {
        var f = new AiInFlight();
        var granted = 0;
        Parallel.For(0, 200, _ =>
        {
            if (f.TryReserve("u:1", Day, AiKind.Exercise, used: 0, limit: 7) is not null) Interlocked.Increment(ref granted);
        });
        Assert.Equal(7, granted);
    }

    [Fact]
    public void Principal_key_separates_users_and_guests()
    {
        var user = new Data.User { Id = Guid.NewGuid(), Email = "a@b.uz", PasswordHash = "" };
        Assert.StartsWith("u:", AiQuotaService.PrincipalKey(user, "10.0.0.1"));
        Assert.Equal("g:10.0.0.1", AiQuotaService.PrincipalKey(null, "10.0.0.1"));
        Assert.NotEqual(MockLimit.InFlightKey(user.Id), AiQuotaService.PrincipalKey(user, ""));
    }

    // ---- Takrorlash: muddati kelmagan kartani baholab XP yig'ib bo'lmaydi ----

    [Fact]
    public void Grading_a_card_that_is_not_due_is_too_early()
    {
        Assert.True(ReviewScheduler.IsTooEarly(Now.AddDays(3), Now));
        Assert.True(ReviewScheduler.IsTooEarly(Now.AddMinutes(10), Now)); // "Again" dan keyin darhol qayta baholash
        Assert.False(ReviewScheduler.IsTooEarly(Now, Now));
        Assert.False(ReviewScheduler.IsTooEarly(Now.AddDays(-2), Now));
        Assert.False(ReviewScheduler.IsTooEarly(Now.AddSeconds(30), Now)); // soatlar farqi — qabul qilinadi
    }

    [Fact]
    public void Comprehension_has_a_daily_xp_cap()
    {
        Assert.InRange(XpGuard.ComprehensionPerDay, 10, 100);
    }

    // ---- Fikr-mulohaza: adminlarga bildirishnomalar oqimi cheklangan ----

    [Fact]
    public void Feedback_notifications_are_throttled_per_hour()
    {
        var t = new FeedbackNotifyThrottle();
        for (var i = 0; i < FeedbackNotifyThrottle.MaxPerHour; i++) Assert.True(t.Take(Now.AddSeconds(i)));
        Assert.False(t.Take(Now.AddMinutes(30)));
        // Bir soatdan keyin eski yozuvlar chiqib ketadi.
        Assert.True(t.Take(Now.AddHours(1).AddMinutes(1)));
    }

    // ---- Email: tasdiqlanmagan manzillar limitning faqat bir qismini oladi ----

    [Fact]
    public void Unverified_recipients_cannot_drain_the_daily_email_cap()
    {
        var cap = new EmailDailyCap();
        const int daily = 100;
        var sent = 0;
        // Tasodifiy manzillarga kun bo'yi (har soatda) kod so'raladi.
        for (var h = 0; h < 24; h++)
            for (var i = 0; i < 100; i++)
                if (cap.TryTake(daily, Now.Date.AddHours(-5 + h).AddMinutes(i * 0.5), verifiedRecipient: false)) sent++;

        Assert.Equal(EmailDailyCap.UnverifiedCap(daily), sent);
        // Tasdiqlangan hisoblar uchun joy qoldi.
        Assert.True(cap.TryTake(daily, Now.Date.AddHours(18), verifiedRecipient: true));
    }

    [Fact]
    public void Unverified_recipients_are_limited_per_hour()
    {
        var cap = new EmailDailyCap();
        for (var i = 0; i < EmailDailyCap.UnverifiedPerHour; i++) Assert.True(cap.TryTake(1000, Now.AddSeconds(i), false));
        Assert.False(cap.TryTake(1000, Now.AddMinutes(10), false));
        Assert.False(cap.HasRoom(1000, Now.AddMinutes(10), false));
        Assert.True(cap.HasRoom(1000, Now.AddMinutes(10), true));
        Assert.True(cap.TryTake(1000, Now.AddMinutes(61), false));
    }

    [Fact]
    public void Verified_recipients_keep_the_old_behaviour()
    {
        var cap = new EmailDailyCap();
        for (var i = 0; i < 5; i++) Assert.True(cap.TryTake(5, Now));
        Assert.False(cap.TryTake(5, Now));
        cap.Release(Now);
        Assert.True(cap.TryTake(5, Now));
    }

    // ---- "write" siyosati: soxta tokenlar IP chegarasidan qochib qutula olmaydi ----

    [Fact]
    public void Write_ip_ceiling_applies_regardless_of_bearer_token()
    {
        using var limiter = RateLimits.WriteIpCeiling();
        var endpoint = new Endpoint(_ => Task.CompletedTask,
            new EndpointMetadataCollection(new Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute(RateLimits.WritePolicy)), "w");

        var granted = 0;
        for (var i = 0; i < RateLimits.WritePerIpPerMinute + 50; i++)
        {
            var http = new DefaultHttpContext();
            http.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.9");
            http.Request.Headers.Authorization = "Bearer fake-" + i; // har safar yangi soxta token
            http.SetEndpoint(endpoint);
            using var lease = limiter.AttemptAcquire(http);
            if (lease.IsAcquired) granted++;
        }
        Assert.Equal(RateLimits.WritePerIpPerMinute, granted);
    }

    [Fact]
    public void Write_ip_ceiling_ignores_other_endpoints()
    {
        using var limiter = RateLimits.WriteIpCeiling();
        var http = new DefaultHttpContext();
        http.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.9");
        for (var i = 0; i < RateLimits.WritePerIpPerMinute + 10; i++)
        {
            using var lease = limiter.AttemptAcquire(http);
            Assert.True(lease.IsAcquired);
        }
    }
}
