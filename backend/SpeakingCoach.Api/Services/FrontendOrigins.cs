namespace SpeakingCoach.Api.Services;

/// <summary>
/// FrontendOrigin sozlamasi: bitta yoki bir nechta manzil (vergul yoki bo'sh joy bilan).
/// Birinchisi — asosiy sayt (bot havolalari shunga), hammasi — CORS ruxsati.
/// Masalan: "https://fluentuz.app,https://speaking-coach-theta.vercel.app".
/// </summary>
public static class FrontendOrigins
{
    public const string Default = "http://localhost:5173";

    public static string[] Parse(string? value)
    {
        var list = (value ?? "")
            .Split([',', ';', ' ', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(o => o.TrimEnd('/'))
            .Where(o => o.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || o.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return list.Length > 0 ? list : [Default];
    }
}
