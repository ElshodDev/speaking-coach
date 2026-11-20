namespace SpeakingCoach.Api.Web;

// HTTP so'rovga bog'liq qisqartmalar. Application qatlami HttpRequest'ni bilmaydi:
// u token, til kodi yoki IP kaliti bilan ishlaydi; bu yerda ular so'rovdan o'qiladi.
// Metod nomlari avvalgidek — endpointlardagi chaqiruvlar o'zgarmaydi.

/// <summary>So'rov tilida matnlar va xato javoblari: <c>request.T("vocab.exists")</c>, <c>request.Error(...)</c>.</summary>
public static class RequestTexts
{
    public static string LangOf(HttpRequest request) => Texts.ParseLang(request.Headers.AcceptLanguage.ToString());

    /// <summary>Endpoint'lar uchun qisqartma: <c>request.T("vocab.exists")</c>.</summary>
    public static string T(this HttpRequest request, string key, params object?[] args) =>
        Texts.Get(LangOf(request), key, args);

    /// <summary>Qisqartma: <c>{ error = "..." }</c> ko'rinishidagi JSON.</summary>
    public static object Error(this HttpRequest request, string key, params object?[] args) =>
        new { error = request.T(key, args) };

    /// <summary>Qisqartma: <c>{ error = "...", code = "..." }</c> ko'rinishidagi JSON.</summary>
    public static object ErrorWithCode(this HttpRequest request, string key, params object?[] args) =>
        new { error = request.T(key, args), code = Texts.CodeOf(key) };
}

/// <summary>Sessiya tokeni "Authorization: Bearer ..." sarlavhasida keladi.</summary>
public static class AuthHttpExtensions
{
    public static string? BearerToken(this HttpRequest request) =>
        AuthService.ParseBearerToken(request.Headers.Authorization.ToString());

    public static Task<User?> GetCurrentUserAsync(this AuthService auth, HttpRequest request) =>
        auth.GetUserByTokenAsync(request.BearerToken());

    public static async Task<Guid?> GetCurrentUserIdAsync(this AuthService auth, HttpRequest request) =>
        (await auth.GetCurrentUserAsync(request))?.Id;

    public static Task<bool> HasRecentLoginAsync(this AuthService auth, HttpRequest request) =>
        auth.HasRecentLoginAsync(request.BearerToken());

    public static Task LogoutAsync(this AuthService auth, HttpRequest request) =>
        auth.LogoutAsync(request.BearerToken());

    public static Task<AuthResult> SetPasswordAsync(this AuthService auth, User user, string? currentPassword, string? newPassword, HttpRequest request) =>
        auth.SetPasswordAsync(user, currentPassword, newPassword, request.BearerToken());
}

/// <summary>Kunlik AI limitlari sayt so'rovlari uchun: foydalanuvchi tokendan, mehmon — IP bo'yicha.</summary>
public static class AiQuotaHttpExtensions
{
    private static string IpOf(HttpRequest request) => RateLimits.ClientKey(request.HttpContext.Connection.RemoteIpAddress);

    private static Task<User?> UserOf(HttpRequest request) =>
        request.HttpContext.RequestServices.GetRequiredService<AuthService>().GetCurrentUserAsync(request);

    public static async Task<QuotaStatus> GetStatusAsync(this AiQuotaService quota, HttpRequest request) =>
        await quota.GetStatusAsync(await UserOf(request), IpOf(request));

    /// <summary>
    /// Limit tugagan bo'lsa — 429 va tushunarli xabar; aks holda null (davom etaverish mumkin).
    /// Ruxsat berilsa, joy so'rov oxirigacha band qilinadi (parallel so'rovlar limitdan oshmaydi).
    /// </summary>
    public static async Task<IResult?> CheckAsync(this AiQuotaService quota, HttpRequest request, AiKind kind)
    {
        var denied = await quota.CheckAndReserveAsync(await UserOf(request), IpOf(request), kind, request.HttpContext.Response.RegisterForDispose);
        return denied is null
            ? null
            : Results.Json(request.Error(denied.Value.Key, denied.Value.Args), statusCode: StatusCodes.Status429TooManyRequests);
    }

    /// <summary>Muvaffaqiyatli AI javobidan keyin chaqiriladi.</summary>
    public static async Task RecordAsync(this AiQuotaService quota, HttpRequest request, AiKind kind) =>
        await quota.RecordAsync(await UserOf(request), IpOf(request), kind);
}
