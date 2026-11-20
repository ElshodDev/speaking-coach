using System.Net;
using Microsoft.Extensions.Configuration;
namespace SpeakingCoach.Api.Tests;

/// <summary>Audit 10–30: admin, IPv6 kalitlari, qayta kirish, guruh turlari, kalendar, Google kalitlari.</summary>
public class AuditFixesTests
{
    private static readonly DateTime Now = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Admin_requires_a_verified_email()
    {
        var admins = new AdminOptions(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Admin:Emails"] = "boss@site.uz" }).Build());
        Assert.False(admins.IsAdmin(new User { Email = "boss@site.uz", PasswordHash = "" }));
        Assert.True(admins.IsAdmin(new User { Email = "boss@site.uz", PasswordHash = "", EmailVerifiedAtUtc = Now }));
        Assert.False(admins.IsAdmin(new User { Email = "other@site.uz", PasswordHash = "", EmailVerifiedAtUtc = Now }));
    }

    [Fact]
    public void Ipv6_clients_share_a_key_per_64_network()
    {
        var a = RateLimits.ClientKey(IPAddress.Parse("2001:db8:1:2:aaaa::1"));
        var b = RateLimits.ClientKey(IPAddress.Parse("2001:db8:1:2:ffff:1:2:3"));
        var other = RateLimits.ClientKey(IPAddress.Parse("2001:db8:1:3::1"));
        Assert.Equal(a, b);
        Assert.NotEqual(a, other);
        Assert.EndsWith("/64", a);
    }

    [Fact]
    public void Ipv4_keys_are_unchanged_including_mapped_addresses()
    {
        Assert.Equal("10.0.0.5", RateLimits.ClientKey(IPAddress.Parse("10.0.0.5")));
        Assert.Equal("10.0.0.5", RateLimits.ClientKey(IPAddress.Parse("::ffff:10.0.0.5")));
        Assert.Equal("unknown", RateLimits.ClientKey(null));
    }

    [Fact]
    public void Guest_pool_counts_all_guests_together()
    {
        var store = new GuestQuotaStore();
        var day = new DateOnly(2026, 11, 20);
        store.Add("1.1.1.1", day, AiKind.Exercise);
        store.Add("2.2.2.2", day, AiKind.Exercise);
        store.Add("2.2.2.2", day, AiKind.Word);
        Assert.Equal(2, store.Total(day, AiKind.Exercise));
        Assert.Equal(1, store.Total(day, AiKind.Word));
        store.Add("3.3.3.3", day.AddDays(1), AiKind.Exercise);
        Assert.Equal(0, store.Total(day, AiKind.Exercise)); // eski kun tozalandi
    }

    [Fact]
    public void Guest_pool_limits_are_configurable()
    {
        var o = new AiQuotaOptions(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Ai:GuestTotalExercisesPerDay"] = "7" }).Build());
        Assert.Equal(7, o.GuestTotalFor(AiKind.Exercise));
        Assert.True(o.GuestTotalFor(AiKind.Word) > 0);
    }

    [Fact]
    public void Dangerous_actions_need_a_recent_login()
    {
        Assert.True(AuthService.IsRecentLogin(Now.AddMinutes(-5), Now));
        Assert.True(AuthService.IsRecentLogin(Now - AuthService.RecentLoginWindow, Now));
        Assert.False(AuthService.IsRecentLogin(Now.AddHours(-1), Now));
        Assert.False(AuthService.IsRecentLogin(Now.AddDays(-20), Now));
    }

    [Fact]
    public void Group_page_reads_only_the_activity_types_assignments_need()
    {
        var a = (string kind) => new Assignment { Kind = kind, CreatedAtUtc = Now };
        Assert.Empty(GroupLogic.NeededTypes([a("review")]));
        Assert.Equal([ActivityType.Writing], GroupLogic.NeededTypes([a("practice:writing"), a("review")]));
        var t = GroupLogic.NeededTypes([a("practice:speaking"), a("mock:ielts:reading"), a("mock:cefr:writing")]);
        Assert.Equal(2, t.Count);
        Assert.Contains(ActivityType.Speaking, t);
        Assert.Contains(ActivityType.MockExam, t);
    }

    [Fact]
    public void Calendar_from_counts_matches_calendar_from_events()
    {
        var events = new[] { Now, Now.AddHours(-1), Now.AddDays(-1), Now.AddDays(-30) };
        var fromEvents = ProgressCalculator.Calendar(events, Now, -300);
        var counts = events.GroupBy(t => ReviewScheduler.ToLocalDate(t, -300)).ToDictionary(g => g.Key, g => g.Count());
        Assert.Equal(fromEvents, ProgressCalculator.Calendar(counts, Now, -300));
    }

    [Fact]
    public void Google_keys_are_force_reloaded_at_most_once_a_minute()
    {
        Assert.False(GoogleSignInService.CanForceReload(Now.AddSeconds(-10), Now));
        Assert.True(GoogleSignInService.CanForceReload(Now.AddMinutes(-2), Now));
        Assert.True(GoogleSignInService.CanForceReload(DateTime.MinValue, Now));
    }
}
