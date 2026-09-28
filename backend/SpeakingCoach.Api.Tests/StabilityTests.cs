using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Mock;

namespace SpeakingCoach.Api.Tests;

/// <summary>Testda boshqariladigan soat.</summary>
public sealed class ManualClock(DateTime startUtc) : TimeProvider
{
    public DateTime Now { get; set; } = startUtc;

    public override DateTimeOffset GetUtcNow() => new(Now, TimeSpan.Zero);

    public void Advance(TimeSpan by) => Now += by;
}

public class TashkentTimeTests
{
    [Fact]
    public void Day_starts_at_tashkent_midnight()
    {
        var at = new DateTime(2026, 9, 26, 20, 30, 0, DateTimeKind.Utc); // Toshkentda 27-sentabr 01:30
        Assert.Equal(new DateOnly(2026, 9, 27), TashkentTime.Today(at));
        Assert.Equal(new DateTime(2026, 9, 26, 19, 0, 0, DateTimeKind.Utc), TashkentTime.DayStartUtc(at));
        Assert.Equal(new DateTime(2026, 9, 27, 19, 0, 0, DateTimeKind.Utc), TashkentTime.NextDayStartUtc(at));
        Assert.Equal(DateTimeKind.Utc, TashkentTime.DayStartUtc(at).Kind);

        var morning = new DateTime(2026, 9, 26, 3, 0, 0, DateTimeKind.Utc); // Toshkentda 08:00
        Assert.Equal(new DateOnly(2026, 9, 26), TashkentTime.Today(morning));
        Assert.Equal(new DateTime(2026, 9, 25, 19, 0, 0, DateTimeKind.Utc), TashkentTime.DayStartUtc(morning));
    }
}

public class GeminiErrorTests
{
    private const string PerMinute429 = """
        {"error":{"code":429,"message":"You exceeded your current quota, please check your plan and billing details.","status":"RESOURCE_EXHAUSTED",
        "details":[{"@type":"type.googleapis.com/google.rpc.QuotaFailure","violations":[{"quotaMetric":"generativelanguage.googleapis.com/generate_content_free_tier_requests",
        "quotaId":"GenerateRequestsPerMinutePerProjectPerModel-FreeTier","quotaDimensions":{"model":"gemini-3.6-flash"},"quotaValue":"10"}]},
        {"@type":"type.googleapis.com/google.rpc.RetryInfo","retryDelay":"4s"}]}}
        """;

    private const string PerDay429 = """
        {"error":{"code":429,"message":"You exceeded your current quota.","status":"RESOURCE_EXHAUSTED",
        "details":[{"@type":"type.googleapis.com/google.rpc.QuotaFailure","violations":[{"quotaId":"GenerateRequestsPerDayPerProjectPerModel-FreeTier","quotaValue":"250"}]},
        {"@type":"type.googleapis.com/google.rpc.RetryInfo","retryDelay":"21s"}]}}
        """;

    [Fact]
    public void Per_minute_429_is_a_transient_rate_limit_with_its_hint()
    {
        var info = GeminiErrors.Classify(HttpStatusCode.TooManyRequests, PerMinute429, null);
        Assert.Equal(GeminiFailure.RateLimited, info.Kind);
        Assert.Equal(TimeSpan.FromSeconds(4), info.RetryAfter);
    }

    [Fact]
    public void Daily_quota_is_exhausted_not_retried()
    {
        Assert.Equal(GeminiFailure.QuotaExhausted, GeminiErrors.Classify(HttpStatusCode.TooManyRequests, PerDay429, null).Kind);
        // Model bepul tarifda yo'q ("limit: 0").
        Assert.Equal(GeminiFailure.QuotaExhausted, GeminiErrors.Classify(HttpStatusCode.TooManyRequests,
            """{"error":{"message":"Quota exceeded for metric: x, limit: 0, model: gemini-pro"}}""", null).Kind);
        // Juda uzoq kutish so'ralgan — amalda tugagan kvota.
        Assert.Equal(GeminiFailure.QuotaExhausted, GeminiErrors.Classify(HttpStatusCode.TooManyRequests,
            """{"details":[{"retryDelay":"3600s"}]}""", null).Kind);
        // "limit: 50" — nol emas.
        Assert.Equal(GeminiFailure.RateLimited, GeminiErrors.Classify(HttpStatusCode.TooManyRequests,
            """{"error":{"message":"Quota exceeded, limit: 50"}}""", null).Kind);
    }

    [Fact]
    public void Server_errors_are_overload_and_client_errors_are_fatal()
    {
        Assert.Equal(GeminiFailure.Overloaded, GeminiErrors.Classify(HttpStatusCode.ServiceUnavailable, "high demand", null).Kind);
        Assert.Equal(GeminiFailure.Overloaded, GeminiErrors.Classify(HttpStatusCode.InternalServerError, null, null).Kind);
        Assert.Equal(GeminiFailure.Overloaded, GeminiErrors.Classify(HttpStatusCode.GatewayTimeout, "", null).Kind);
        Assert.Equal(GeminiFailure.Fatal, GeminiErrors.Classify(HttpStatusCode.BadRequest, "bad", null).Kind);
        Assert.Equal(GeminiFailure.Fatal, GeminiErrors.Classify(HttpStatusCode.Forbidden, "key", null).Kind);
    }

    [Fact]
    public void Retry_after_header_is_used_when_the_body_has_no_hint()
    {
        var info = GeminiErrors.Classify(HttpStatusCode.ServiceUnavailable, "{}", TimeSpan.FromSeconds(2));
        Assert.Equal(TimeSpan.FromSeconds(2), info.RetryAfter);
        Assert.Equal(TimeSpan.FromSeconds(1.5), GeminiErrors.RetryDelay("""{"retryDelay": "1.5s"}"""));
        Assert.Null(GeminiErrors.RetryDelay("{}"));
    }

    [Fact]
    public void Model_list_is_configurable_with_safe_fallback()
    {
        Assert.Equal(AiGuardOptions.DefaultModels, AiGuardOptions.ParseModels(null));
        Assert.Equal(AiGuardOptions.DefaultModels, AiGuardOptions.ParseModels(" , "));
        Assert.Equal(new[] { "gemini-x", "gemini-y-lite" }, AiGuardOptions.ParseModels(" gemini-x , gemini-y-lite,gemini-x "));
        // URL'ni buzadigan nomlar tashlanadi.
        Assert.Equal(new[] { "ok-model" }, AiGuardOptions.ParseModels("bad/model?x=1,ok-model"));
    }

    [Fact]
    public void Log_text_is_truncated()
    {
        var s = GeminiClient.Truncate(new string('x', 5000));
        Assert.True(s.Length <= GeminiClient.MaxLoggedChars + 1);
        Assert.Equal("short", GeminiClient.Truncate("short"));
        var ex = Assert.Throws<InvalidOperationException>(() => GeminiClient.DeserializeStrict<TalkTurnResult>("{\"oops\": \"" + new string('y', 5000) + "\""));
        Assert.True(ex.Message.Length < 1000, ex.Message.Length.ToString());
    }
}

public class AiGuardTests
{
    private static readonly DateTime Start = new(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc);

    private static (AiGuard Guard, ManualClock Clock) Make(int perMinute = 3, int perDay = 100, int failures = 3, int cooldownSeconds = 60)
    {
        var clock = new ManualClock(Start);
        var options = new AiGuardOptions
        {
            PerMinute = perMinute,
            PerDay = perDay,
            BreakerFailures = failures,
            BreakerCooldown = TimeSpan.FromSeconds(cooldownSeconds),
            QuotaCooldown = TimeSpan.FromMinutes(30),
        };
        return (new AiGuard(options, clock), clock);
    }

    [Fact]
    public void Minute_budget_is_a_sliding_window()
    {
        var (guard, clock) = Make(perMinute: 3);
        Assert.True(guard.TryAcquire(out _, out _));
        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.True(guard.TryAcquire(out _, out _));
        Assert.True(guard.TryAcquire(out _, out _));

        Assert.False(guard.TryAcquire(out var retryAt, out var reason));
        Assert.Equal("minute", reason);
        Assert.Equal(Start.AddMinutes(1), retryAt); // eng eski so'rov oynadan chiqqanda

        clock.Advance(TimeSpan.FromSeconds(51)); // birinchisidan 61 s o'tdi
        Assert.True(guard.TryAcquire(out _, out _));
        Assert.False(guard.TryAcquire(out _, out _));
    }

    [Fact]
    public void Day_budget_resets_at_tashkent_midnight()
    {
        var (guard, clock) = Make(perMinute: 100, perDay: 2);
        Assert.True(guard.TryAcquire(out _, out _));
        Assert.True(guard.TryAcquire(out _, out _));
        Assert.False(guard.TryAcquire(out var retryAt, out var reason));
        Assert.Equal("day", reason);
        Assert.Equal(new DateTime(2026, 9, 26, 19, 0, 0, DateTimeKind.Utc), retryAt);

        clock.Now = new DateTime(2026, 9, 26, 19, 0, 1, DateTimeKind.Utc);
        Assert.True(guard.TryAcquire(out _, out _));
        Assert.Equal(1, guard.Snapshot().Today);
    }

    [Fact]
    public void Breaker_opens_after_consecutive_failures_and_recovers()
    {
        var (guard, clock) = Make(perMinute: 100, failures: 3, cooldownSeconds: 60);
        guard.ReportFailure();
        guard.ReportFailure();
        guard.ReportSuccess(); // muvaffaqiyat hisobni nolga tushiradi
        guard.ReportFailure();
        guard.ReportFailure();
        Assert.False(guard.IsOpen(out _));
        guard.ReportFailure();
        Assert.True(guard.IsOpen(out var until));
        Assert.Equal(Start.AddSeconds(60), until);

        Assert.False(guard.TryAcquire(out var retryAt, out var reason));
        Assert.Equal("breaker", reason);
        Assert.Equal(until, retryAt);

        // Kutish tugadi — sinov so'roviga ruxsat; yana xato bo'lsa — darhol qayta ochiladi.
        clock.Advance(TimeSpan.FromSeconds(61));
        Assert.True(guard.TryAcquire(out _, out _));
        guard.ReportFailure();
        Assert.True(guard.IsOpen(out _));

        clock.Advance(TimeSpan.FromSeconds(61));
        Assert.True(guard.TryAcquire(out _, out _));
        guard.ReportSuccess();
        guard.ReportFailure();
        Assert.False(guard.IsOpen(out _));
    }

    [Fact]
    public void Acquire_throws_busy_when_the_wait_is_long()
    {
        var (guard, _) = Make(perMinute: 100, perDay: 1);
        guard.AcquireAsync(CancellationToken.None).GetAwaiter().GetResult();
        AiBusyException? busy = null;
        try
        {
            guard.AcquireAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
        catch (AiBusyException ex)
        {
            busy = ex;
        }
        Assert.NotNull(busy);
        Assert.Equal("day", busy!.Reason);
        Assert.Equal(9 * 3600, busy.RetryAfterSeconds(Start)); // 10:00 UTC → 19:00 UTC
    }

    [Fact]
    public void Exhausted_model_is_skipped_for_a_while()
    {
        var (guard, clock) = Make();
        guard.MarkModelExhausted("m1", hint: null);
        Assert.True(guard.IsModelBlocked("m1", out var until));
        Assert.Equal(Start.AddMinutes(30), until);
        Assert.False(guard.IsModelBlocked("m2", out _));

        guard.MarkModelExhausted("m2", TimeSpan.FromSeconds(40));
        Assert.True(guard.IsModelBlocked("m2", out var until2));
        Assert.Equal(Start.AddSeconds(40), until2);

        clock.Advance(TimeSpan.FromMinutes(31));
        Assert.False(guard.IsModelBlocked("m1", out _));
        Assert.False(guard.IsModelBlocked("m2", out _));
    }

    [Fact]
    public void Busy_retry_seconds_are_clamped()
    {
        var now = Start;
        Assert.Equal(1, new AiBusyException(now.AddSeconds(-5), "x").RetryAfterSeconds(now));
        Assert.Equal(3, new AiBusyException(now.AddSeconds(2.2), "x").RetryAfterSeconds(now));
        Assert.Equal(86400, new AiBusyException(now.AddDays(3), "x").RetryAfterSeconds(now));
    }

    [Fact]
    public void Options_have_documented_defaults()
    {
        var o = new AiGuardOptions(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());
        Assert.Equal(12, o.PerMinute);
        Assert.Equal(1400, o.PerDay);
        Assert.Equal(5, o.BreakerFailures);
        Assert.Equal(TimeSpan.FromSeconds(60), o.BreakerCooldown);
        Assert.Equal(AiGuardOptions.DefaultModels, o.Models);
    }

    [Fact]
    public void Busy_errors_are_not_swallowed_by_endpoint_catch_filters()
    {
        var http = new DefaultHttpContext();
        Assert.False(AiErrors.Handles(new AiBusyException(DateTime.UtcNow, "day"), http.Request));
        Assert.True(AiErrors.Handles(new InvalidOperationException("bad json"), http.Request));
        Assert.True(AiErrors.Handles(new GeminiApiException(HttpStatusCode.BadRequest, "x"), http.Request));
        // Timeout (so'rov bekor qilinmagan) — 502; foydalanuvchi o'zi bekor qilgan — ushlanmaydi.
        Assert.True(AiErrors.Handles(new TaskCanceledException(), http.Request));
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        http.RequestAborted = cts.Token;
        Assert.False(AiErrors.Handles(new OperationCanceledException(), http.Request));
    }

    [Fact]
    public void Rate_limit_policy_names_are_stable()
    {
        Assert.Equal("auth", RateLimits.AuthPolicy);
        Assert.Equal("code", RateLimits.CodePolicy);
        Assert.Equal("poll", RateLimits.PollPolicy);
        Assert.Equal("write", RateLimits.WritePolicy);
        Assert.Equal("ai", RateLimits.AiPolicy);
        Assert.Equal("join", RateLimits.JoinPolicy);
    }
}

public class TalkLockTests
{
    [Fact]
    public void Second_turn_on_the_same_talk_is_rejected_until_the_first_ends()
    {
        var locks = new TalkLocks();
        var talk = Guid.NewGuid();
        var first = locks.TryEnter(talk);
        Assert.NotNull(first);
        Assert.Null(locks.TryEnter(talk));
        Assert.True(locks.IsBusy(talk));

        // Boshqa suhbat — mustaqil.
        using (var other = locks.TryEnter(Guid.NewGuid())) Assert.NotNull(other);

        first!.Dispose();
        first.Dispose(); // ikki marta Dispose — zararsiz
        Assert.False(locks.IsBusy(talk));
        using var again = locks.TryEnter(talk);
        Assert.NotNull(again);
    }

    [Fact]
    public void Only_one_of_many_parallel_turns_enters()
    {
        var locks = new TalkLocks();
        var talk = Guid.NewGuid();
        var entered = 0;
        var leases = new System.Collections.Concurrent.ConcurrentBag<IDisposable>();
        Parallel.For(0, 50, _ =>
        {
            var l = locks.TryEnter(talk);
            if (l is not null)
            {
                Interlocked.Increment(ref entered);
                leases.Add(l);
            }
        });
        Assert.Equal(1, entered);
    }

    [Fact]
    public void Generation_gate_refunds_an_attempt()
    {
        var gate = new GenerationGate(new ManualClock(DateTime.UtcNow));
        var user = Guid.NewGuid();
        Assert.Equal(GenerationGate.Outcome.Ok, gate.TryEnter(user, 1, out var lease));
        lease!.Dispose();
        Assert.Equal(GenerationGate.Outcome.Exhausted, gate.TryEnter(user, 1, out _));
        gate.Refund(user);
        Assert.Equal(GenerationGate.Outcome.Ok, gate.TryEnter(user, 1, out var again));
        again!.Dispose();
    }
}

public class XpGuardTests
{
    [Theory]
    [InlineData(10, 80, 100, true)]
    [InlineData(10, 100, 100, true)]
    [InlineData(10, 101, 100, false)] // to'g'ri > jami
    [InlineData(10, 0, 401, false)]   // 10 gapda 400 dan ortiq so'z
    [InlineData(10, 5, 9, false)]     // gapdan kam so'z
    [InlineData(0, 0, 1, false)]
    [InlineData(51, 10, 100, false)]
    [InlineData(1, -1, 5, false)]
    public void Dictation_results_must_be_plausible(int sentences, int correct, int total, bool ok)
    {
        Assert.Equal(ok, XpGuard.ValidDictation(sentences, correct, total));
    }

    private static List<QuestionReview> Review(params string[] given) =>
        given.Select((g, i) => new QuestionReview(i + 1, g, ["x"], false, "")).ToList();

    [Fact]
    public void Stored_result_fingerprint_matches_the_new_submission()
    {
        var review = Review("A", " the  Station ", "");
        var stored = JsonSerializer.Serialize(new ObjectiveResult("ielts", "listening", "t1", "", 0, 3, 3.5m, 100, review), MockSets.Web);
        Assert.Equal(XpGuard.Fingerprint(Review("a", "the station", "")), XpGuard.FingerprintOf(stored));
        Assert.NotEqual(XpGuard.Fingerprint(Review("B", "the station", "")), XpGuard.FingerprintOf(stored));
        Assert.Null(XpGuard.FingerprintOf("not json"));
        Assert.Null(XpGuard.FingerprintOf("""{"overall": 5}"""));
    }

    [Fact]
    public void Duplicate_is_found_only_for_the_same_test_answers_and_window()
    {
        var now = new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc);
        var review = Review("A", "B");
        var json = JsonSerializer.Serialize(new ObjectiveResult("ielts", "reading", "t1", "academic", 0, 2, 3m, 60, review), MockSets.Web);
        var fp = XpGuard.Fingerprint(review);
        var id = Guid.NewGuid();

        var recent = new List<XpGuard.RecentResult> { new(id, now.AddMinutes(-3), "t1", "reading", json) };
        Assert.Equal(id, XpGuard.FindDuplicate(recent, "t1", "reading", fp, now));
        Assert.Null(XpGuard.FindDuplicate(recent, "t2", "reading", fp, now));
        Assert.Null(XpGuard.FindDuplicate(recent, "t1", "listening", fp, now));
        Assert.Null(XpGuard.FindDuplicate(recent, "t1", "reading", XpGuard.Fingerprint(Review("A", "C")), now));

        var old = new List<XpGuard.RecentResult> { new(id, now.AddMinutes(-11), "t1", "reading", json) };
        Assert.Null(XpGuard.FindDuplicate(old, "t1", "reading", fp, now));
        // Javob matni yuklanmagan (oynadan tashqari) — solishtirilmaydi.
        Assert.Null(XpGuard.FindDuplicate([new(id, now.AddMinutes(-1), "t1", "reading", null)], "t1", "reading", fp, now));
    }
}

public class MaintenanceSelectionTests
{
    private static readonly DateTime Now = new(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);

    private sealed class World
    {
        public List<User> Users { get; } = [];
        public List<Session> Sessions { get; } = [];
        public List<Activity> Activities { get; } = [];
        public List<ReviewCard> Cards { get; } = [];
        public List<ReviewLog> Logs { get; } = [];
        public List<AiUsage> Usage { get; } = [];
        public List<TelegramAccount> Telegram { get; } = [];
        public List<TelegramLinkToken> Tokens { get; } = [];
        public List<MockTest> Tests { get; } = [];
        public List<Group> Groups { get; } = [];
        public List<GroupMember> Members { get; } = [];

        public List<Guid> Selected() => Maintenance.AbandonedSignups(
            Users.AsQueryable(), Sessions.AsQueryable(), Activities.AsQueryable(), Cards.AsQueryable(), Logs.AsQueryable(),
            Usage.AsQueryable(), Telegram.AsQueryable(), Tokens.AsQueryable(), Tests.AsQueryable(), Groups.AsQueryable(),
            Members.AsQueryable(), Now).Select(u => u.Id).ToList();

        public User Add(Action<User>? change = null)
        {
            var u = new User { Id = Guid.NewGuid(), Email = $"{Guid.NewGuid():N}@mail.uz", PasswordHash = "", CreatedAtUtc = Now.AddDays(-8) };
            change?.Invoke(u);
            Users.Add(u);
            return u;
        }
    }

    [Fact]
    public void Only_old_unverified_passwordless_unused_accounts_are_selected()
    {
        var w = new World();
        var abandoned = w.Add();
        w.Add(u => u.CreatedAtUtc = Now.AddDays(-6));             // hali 7 kun bo'lmagan
        w.Add(u => u.EmailVerifiedAtUtc = Now.AddDays(-8));        // tasdiqlangan (kod yoki Google)
        w.Add(u => u.PasswordHash = "hash");                      // parol bilan ro'yxatdan o'tgan
        w.Add(u => u.Email = "demo-1@" + DemoAccount.Domain);      // demo — alohida tozalanadi
        w.Add(u => u.OnboardedAtUtc = Now.AddDays(-8));            // tanishtiruvdan o'tgan
        w.Add(u => u.DisplayName = "Ali");

        Assert.Equal([abandoned.Id], w.Selected());
    }

    [Fact]
    public void Any_owned_row_keeps_the_account()
    {
        var w = new World();
        var keep = new List<User>();
        User Owned() { var u = w.Add(); keep.Add(u); return u; }

        w.Sessions.Add(new Session { UserId = Owned().Id });
        w.Activities.Add(new Activity { UserId = Owned().Id });
        w.Cards.Add(new ReviewCard { UserId = Owned().Id });
        w.Logs.Add(new ReviewLog { UserId = Owned().Id });
        w.Usage.Add(new AiUsage { UserId = Owned().Id });
        w.Telegram.Add(new TelegramAccount { UserId = Owned().Id });
        w.Tokens.Add(new TelegramLinkToken { UserId = Owned().Id });
        w.Tests.Add(new MockTest { CreatedByUserId = Owned().Id });
        w.Groups.Add(new Group { TeacherId = Owned().Id });
        w.Members.Add(new GroupMember { UserId = Owned().Id });
        var abandoned = w.Add();

        Assert.Equal([abandoned.Id], w.Selected());
        Assert.Equal(10, keep.Count);
    }
}

public class HeavyQueryTests
{
    [Fact]
    public void Mistake_sql_lists_exactly_the_source_types()
    {
        foreach (var t in SpeakingCoach.Api.Endpoints.MistakeEndpoints.SourceTypes)
            Assert.Contains($"'{t}'", SpeakingCoach.Api.Endpoints.MistakeEndpoints.CorrectionsSql);
        Assert.Contains("LIMIT {1}", SpeakingCoach.Api.Endpoints.MistakeEndpoints.CorrectionsSql);
        Assert.Equal(400, SpeakingCoach.Api.Endpoints.MistakeEndpoints.MaxActivities);
    }

    [Fact]
    public void Light_mock_rows_keep_the_prompt_defaults()
    {
        var row = new SpeakingCoach.Api.Endpoints.MockEndpoints.MockRow { Module = "reading", SetId = "t1" };
        Assert.Equal("ielts", row.Prompt.Exam);
        Assert.Equal("reading", row.Prompt.Module);
        Assert.Equal("t1", row.Prompt.SetId);
        Assert.Contains($"'{ActivityType.MockExam}'", SpeakingCoach.Api.Endpoints.MockEndpoints.MockRowsSql);
    }
}
