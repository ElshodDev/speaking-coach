using Microsoft.AspNetCore.Http;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Mock;
using SpeakingCoach.Api.Services.Telegram;

namespace SpeakingCoach.Api.Tests;

/// <summary>Xavfsizlik va barqarorlik qoidalari (audit tuzatishlari).</summary>
public class SecurityTests
{
    private static readonly DateTime T = new(2026, 9, 27, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid User = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private static EmailCode Code(string code, int attempts = 0, DateTime? used = null) => new()
    {
        UserId = User,
        CodeHash = EmailCodeRules.Hash(User, code),
        CreatedAtUtc = T,
        ExpiresAtUtc = T.Add(EmailCodeRules.Lifetime),
        Attempts = attempts,
        UsedAtUtc = used,
    };

    [Fact]
    public void Code_precheck_blocks_used_expired_and_exhausted_codes_before_comparing()
    {
        Assert.Null(EmailCodeRules.Precheck(Code("123456"), T));
        Assert.Equal(CodeCheck.NotFound, EmailCodeRules.Precheck(null, T));
        Assert.Equal(CodeCheck.NotFound, EmailCodeRules.Precheck(Code("123456", used: T), T));
        Assert.Equal(CodeCheck.Expired, EmailCodeRules.Precheck(Code("123456"), T.Add(EmailCodeRules.Lifetime)));
        Assert.Equal(CodeCheck.TooManyAttempts, EmailCodeRules.Precheck(Code("123456", attempts: EmailCodeRules.MaxAttempts), T));
    }

    [Fact]
    public void Code_matching_is_exact_and_bound_to_the_user()
    {
        Assert.True(EmailCodeRules.Matches(Code("042917"), User, "042 917"));
        Assert.False(EmailCodeRules.Matches(Code("042917"), User, "042918"));
        Assert.False(EmailCodeRules.Matches(Code("042917"), Guid.NewGuid(), "042917"));
        Assert.False(EmailCodeRules.Matches(Code("042917"), User, ""));
        // Check = Precheck + Matches
        Assert.Equal(CodeCheck.TooManyAttempts, EmailCodeRules.Check(Code("042917", attempts: 5), User, "042917", T));
    }

    [Fact]
    public void Telegram_admin_ids_ignore_usernames_and_garbage()
    {
        Assert.Equal([42L, 7L], TelegramOptions.ParseAdminIds("42, @someone, 7, abc, -3, 0, 42").OrderByDescending(x => x).ToArray());
        Assert.Empty(TelegramOptions.ParseAdminIds(null));
    }

    [Fact]
    public void Mini_app_init_data_is_short_lived()
    {
        Assert.True(TelegramWebAppAuth.MaxAge <= TimeSpan.FromHours(1));
    }

    [Fact]
    public void Generation_gate_allows_one_run_per_user_and_counts_failures()
    {
        var gate = new GenerationGate();
        var a = Guid.NewGuid();
        Assert.Equal(GenerationGate.Outcome.Ok, gate.TryEnter(a, 2, out var first));
        // Parallel ikkinchi so'rov — band.
        Assert.Equal(GenerationGate.Outcome.Busy, gate.TryEnter(a, 2, out var none));
        Assert.Null(none);
        // Boshqa foydalanuvchiga ta'sir qilmaydi.
        Assert.Equal(GenerationGate.Outcome.Ok, gate.TryEnter(Guid.NewGuid(), 2, out var other));
        other!.Dispose();

        first!.Dispose(); // (muvaffaqiyatsiz bo'lsa ham) urinish sanaldi
        first.Dispose();  // ikki marta Dispose — zararsiz
        Assert.Equal(GenerationGate.Outcome.Ok, gate.TryEnter(a, 2, out var second));
        second!.Dispose();
        Assert.Equal(GenerationGate.Outcome.Exhausted, gate.TryEnter(a, 2, out _));
        Assert.Equal(2, gate.AttemptsOf(a));
    }

    [Fact]
    public void Rate_limit_key_uses_the_token_when_present_otherwise_the_ip()
    {
        var guest = new DefaultHttpContext();
        guest.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.5");
        Assert.Equal("ip:10.0.0.5", RateLimits.User(guest));

        var a = new DefaultHttpContext();
        a.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.5");
        a.Request.Headers.Authorization = "Bearer token-a";
        var b = new DefaultHttpContext();
        b.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.5");
        b.Request.Headers.Authorization = "Bearer token-b";

        // Bir sinf (bitta IP) — har bir o'quvchi alohida; token o'zi kalitda ko'rinmaydi.
        Assert.StartsWith("t:", RateLimits.User(a));
        Assert.NotEqual(RateLimits.User(a), RateLimits.User(b));
        Assert.DoesNotContain("token-a", RateLimits.User(a));
        Assert.Equal(RateLimits.User(a), RateLimits.User(a));
        Assert.Equal("ip:10.0.0.5", RateLimits.Ip(a));
    }

    [Fact]
    public void Connection_string_gets_gss_disabled_once()
    {
        Assert.Equal("Host=h;Database=d;GSS Encryption Mode=Disable", DbConnection.Normalize("Host=h;Database=d;"));
        Assert.Equal("Host=h;GSS Encryption Mode=Prefer", DbConnection.Normalize("Host=h;GSS Encryption Mode=Prefer"));
        Assert.Equal("postgres://u:p@h/d", DbConnection.Normalize("postgres://u:p@h/d"));
    }

    [Fact]
    public void A_double_click_grade_is_ignored()
    {
        Assert.True(ReviewScheduler.IsDuplicateGrade(T, T.AddSeconds(2)));
        Assert.False(ReviewScheduler.IsDuplicateGrade(T, T.AddSeconds(10)));
        Assert.False(ReviewScheduler.IsDuplicateGrade(null, T));
        Assert.False(ReviewScheduler.IsDuplicateGrade(T.AddSeconds(5), T)); // soat orqaga ketgan bo'lsa ham bloklamaydi
    }

    [Fact]
    public void Join_codes_are_random_and_valid()
    {
        var codes = Enumerable.Range(0, 300).Select(_ => GroupLogic.NewJoinCode()).ToList();
        Assert.All(codes, c => Assert.Equal(c, GroupLogic.NormalizeCode(c)));
        Assert.True(codes.Distinct().Count() > 295);
    }
}
