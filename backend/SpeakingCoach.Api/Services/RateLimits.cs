using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace SpeakingCoach.Api.Services;

/// <summary>
/// So'rovlar cheklovi (rate limiting) kalitlari va AI so'rovlari uchun
/// umumiy himoya. AI (Gemini) so'rovlari uch qavat bilan cheklanadi:
/// <list type="number">
/// <item>foydalanuvchi (token) yoki mehmon (IP) — bir vaqtda 2 ta, navbatda 4 ta;</item>
/// <item>IP — bir vaqtda 8 ta (bir sinf bitta IP ortida bo'lishi mumkin), daqiqasiga 120 ta;</item>
/// <item>butun server — bir vaqtda 16 ta, navbatda 64 ta (xotira va Gemini kvotasi).</item>
/// </list>
/// Ustiga "ai" siyosati: foydalanuvchi/IP uchun daqiqasiga 30 ta.
/// Boshqa nomli siyosatlar (AuthPolicy, CodePolicy, PollPolicy, WritePolicy) — pastda.
/// Rad etilganda 429, JSON xabar va Retry-After sarlavhasi (soniyalarda).
/// </summary>
public static class RateLimits
{
    public const string AiPolicy = "ai";

    /// <summary>Kirish/ro'yxatdan o'tish/Google/Telegram Mini App/tasdiqlash/parol tiklash/demo: IP uchun daqiqasiga 30 ta.</summary>
    public const string AuthPolicy = "auth";

    /// <summary>Email YUBORADIGAN endpoint'lar (kod so'rash, qayta yuborish, parolni unutdim): IP uchun daqiqasiga 6 ta VA soatiga 30 ta.</summary>
    public const string CodePolicy = "code";

    /// <summary>Telegram orqali kirishni kutish (har 2 soniyada so'rov): IP uchun daqiqasiga 90 ta.</summary>
    public const string PollPolicy = "poll";

    /// <summary>Arzon, AI'siz yozuvlar (diktant natijasi, fikr-mulohaza, mock natijasi, profil): foydalanuvchi (yoki IP) uchun daqiqasiga 60 ta.</summary>
    public const string WritePolicy = "write";
    /// <summary>Guruhga qo'shilish kodi: IP uchun daqiqasiga 60 ta — bir sinf bitta
    /// Wi-Fi ortida bir vaqtda qo'shila oladi, kodlarni terib chiqish esa amalda imkonsiz (31⁶ variant).</summary>
    public const string JoinPolicy = "join";

    private static FixedWindowRateLimiterOptions PerWindow(int limit, TimeSpan window) =>
        new() { PermitLimit = limit, Window = window, QueueLimit = 0 };

    /// <summary>Barcha nomli siyosatlar, umumiy AI cheklovi va 429 javobi (Program.cs chaqiradi).</summary>
    public static void Configure(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, ct) =>
        {
            var http = context.HttpContext;
            // Qachon qayta urinish mumkin: oynali cheklovlar aytadi; bir vaqtdagi so'rovlar cheklovi — bir necha soniya.
            var seconds = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                ? (int)Math.Clamp(Math.Ceiling(retryAfter.TotalSeconds), 1, 3600)
                : 5;
            http.Response.Headers.RetryAfter = seconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
            await http.Response.WriteAsJsonAsync(
                new { error = http.Request.T("rate_limited"), code = "rate_limited", retryAfterSeconds = seconds }, ct);
        };

        options.AddPolicy(AuthPolicy, http => RateLimitPartition.GetFixedWindowLimiter(
            Ip(http), _ => PerWindow(30, TimeSpan.FromMinutes(1))));

        // Ikki cheklov birga: daqiqalik (tez takrorlash) va soatlik (email yuborish narxi, spam).
        options.AddPolicy(CodePolicy, http => RateLimitPartition.Get(
            Ip(http), _ => RateLimiter.CreateChained(
                new FixedWindowRateLimiter(PerWindow(6, TimeSpan.FromMinutes(1))),
                new FixedWindowRateLimiter(PerWindow(30, TimeSpan.FromHours(1))))));

        options.AddPolicy(PollPolicy, http => RateLimitPartition.GetFixedWindowLimiter(
            Ip(http), _ => PerWindow(90, TimeSpan.FromMinutes(1))));

        options.AddPolicy(WritePolicy, http => RateLimitPartition.GetFixedWindowLimiter(
            User(http), _ => PerWindow(60, TimeSpan.FromMinutes(1))));

        options.AddPolicy(AiPolicy, http => RateLimitPartition.GetFixedWindowLimiter(
            User(http), _ => PerWindow(30, TimeSpan.FromMinutes(1))));

        options.AddPolicy(JoinPolicy, http => RateLimitPartition.GetFixedWindowLimiter(
            Ip(http), _ => PerWindow(60, TimeSpan.FromMinutes(1))));

        options.GlobalLimiter = AiGlobal();
    }

    public static string Ip(HttpContext http) => "ip:" + (http.Connection.RemoteIpAddress?.ToString() ?? "unknown");

    /// <summary>
    /// Kirgan foydalanuvchi — token xeshi bo'yicha (sinfdagi o'quvchilar bir-birini
    /// to'sib qo'ymasin), mehmon — IP bo'yicha. Soxta tokenlar bilan aylanib
    /// o'tishni IP va server darajasidagi cheklovlar to'xtatadi.
    /// </summary>
    public static string User(HttpContext http)
    {
        var header = http.Request.Headers.Authorization.ToString();
        const string bearer = "Bearer ";
        if (header.Length > bearer.Length && header.Length <= 512 && header.StartsWith(bearer, StringComparison.OrdinalIgnoreCase))
        {
            var token = header[bearer.Length..].Trim();
            if (token.Length > 0)
                return "t:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)), 0, 12);
        }
        return Ip(http);
    }

    public static bool IsAi(HttpContext http) =>
        http.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName == AiPolicy;

    /// <summary>Faqat AI endpoint'lariga ta'sir qiladigan umumiy cheklov (qolganlari — cheklovsiz).</summary>
    public static PartitionedRateLimiter<HttpContext> AiGlobal() => PartitionedRateLimiter.CreateChained(
        PartitionedRateLimiter.Create<HttpContext, string>(http => IsAi(http)
            ? RateLimitPartition.GetConcurrencyLimiter(User(http), _ => new ConcurrencyLimiterOptions { PermitLimit = 2, QueueLimit = 4 })
            : RateLimitPartition.GetNoLimiter("")),
        PartitionedRateLimiter.Create<HttpContext, string>(http => IsAi(http)
            ? RateLimitPartition.GetConcurrencyLimiter(Ip(http), _ => new ConcurrencyLimiterOptions { PermitLimit = 8, QueueLimit = 16 })
            : RateLimitPartition.GetNoLimiter("")),
        PartitionedRateLimiter.Create<HttpContext, string>(http => IsAi(http)
            ? RateLimitPartition.GetFixedWindowLimiter(Ip(http), _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1) })
            : RateLimitPartition.GetNoLimiter("")),
        PartitionedRateLimiter.Create<HttpContext, string>(http => IsAi(http)
            ? RateLimitPartition.GetConcurrencyLimiter("server", _ => new ConcurrencyLimiterOptions { PermitLimit = 16, QueueLimit = 64 })
            : RateLimitPartition.GetNoLimiter("")));
}
