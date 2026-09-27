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
/// </summary>
public static class RateLimits
{
    public const string AiPolicy = "ai";
    /// <summary>Guruhga qo'shilish kodi: IP uchun daqiqasiga 60 ta — bir sinf bitta
    /// Wi-Fi ortida bir vaqtda qo'shila oladi, kodlarni terib chiqish esa amalda imkonsiz (31⁶ variant).</summary>
    public const string JoinPolicy = "join";

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
