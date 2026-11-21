using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpeakingCoach.Application.Telegram;

public record TgUser(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("first_name")] string? FirstName,
    [property: JsonPropertyName("username")] string? Username,
    [property: JsonPropertyName("language_code")] string? LanguageCode);

public record TgChat(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("type")] string Type);

public record TgMessage(
    [property: JsonPropertyName("message_id")] long MessageId,
    [property: JsonPropertyName("chat")] TgChat Chat,
    [property: JsonPropertyName("from")] TgUser? From,
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("caption")] string? Caption = null,
    [property: JsonPropertyName("document")] TgDocument? Document = null,
    [property: JsonPropertyName("photo")] TgPhotoSize[]? Photo = null);

/// <summary>Yuborilgan fayl (PDF, .txt, rasm "fayl sifatida").</summary>
public record TgDocument(
    [property: JsonPropertyName("file_id")] string FileId,
    [property: JsonPropertyName("file_name")] string? FileName,
    [property: JsonPropertyName("mime_type")] string? MimeType,
    [property: JsonPropertyName("file_size")] long? FileSize);

/// <summary>Rasm (Telegram bir necha o'lchamda yuboradi — eng kattasini olamiz).</summary>
public record TgPhotoSize(
    [property: JsonPropertyName("file_id")] string FileId,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height,
    [property: JsonPropertyName("file_size")] long? FileSize);

public record TgCallbackQuery(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("from")] TgUser From,
    [property: JsonPropertyName("message")] TgMessage? Message,
    [property: JsonPropertyName("data")] string? Data);

public record TgUpdate(
    [property: JsonPropertyName("update_id")] long UpdateId,
    [property: JsonPropertyName("message")] TgMessage? Message,
    [property: JsonPropertyName("callback_query")] TgCallbackQuery? CallbackQuery);

/// <summary>getWebhookInfo javobi — admin paneldagi "Telegram holati" uchun.</summary>
public record TgWebhookInfo(
    [property: JsonPropertyName("url")] string? Url,
    [property: JsonPropertyName("pending_update_count")] int PendingUpdateCount,
    [property: JsonPropertyName("last_error_date")] long? LastErrorDate,
    [property: JsonPropertyName("last_error_message")] string? LastErrorMessage);

/// <summary>Inline tugma: bosilganda botga callback keladi yoki havola ochiladi.</summary>
public record TgButton(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("callback_data"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? CallbackData = null,
    [property: JsonPropertyName("url"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Url = null,
    [property: JsonPropertyName("web_app"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] TgWebAppInfo? WebApp = null);

/// <summary>Mini App (sayt Telegram ichida ochiladi). Faqat HTTPS manzil.</summary>
public record TgWebAppInfo([property: JsonPropertyName("url")] string Url);

/// <summary>
/// Sozlamalar: Telegram:BotToken (majburiy), Telegram:BotUsername,
/// Telegram:PublicUrl (bo'lmasa Render beradigan RENDER_EXTERNAL_URL),
/// Telegram:CronSecret (soatlik eslatmalar uchun).
/// Webhook maxfiy kaliti tokendan hosil qilinadi — alohida sozlash shart emas.
/// </summary>
public class TelegramOptions
{
    public string? Token { get; }
    public string BotUsername { get; }
    public string? PublicUrl { get; }
    public string? CronSecret { get; }
    public string FrontendUrl { get; }

    public TelegramOptions(IConfiguration config)
    {
        Token = Blank(config["Telegram:BotToken"]);
        BotUsername = (Blank(config["Telegram:BotUsername"]) ?? "SpeakingCoachUzBot").TrimStart('@');
        PublicUrl = (Blank(config["Telegram:PublicUrl"]) ?? Blank(config["RENDER_EXTERNAL_URL"]))?.TrimEnd('/');
        CronSecret = Blank(config["Telegram:CronSecret"]);
        // Botdagi sayt tugmalari — asosiy (birinchi) manzilga.
        FrontendUrl = FrontendOrigins.Parse(config["FrontendOrigin"])[0];
        AdminIds = ParseAdminIds(config["Telegram:Admins"]);
    }

    /// <summary>
    /// Telegram:Admins — botdagi adminlar: Telegram ID'lari (raqam, vergul bilan).
    /// Faqat ID: @username o'zgartirilishi va boshqa odamga o'tishi mumkin, ID esa
    /// hech qachon o'zgarmaydi. O'z ID'ingizni botda /id buyrug'i ko'rsatadi.
    /// Username yoki xato yozilgan qiymatlar e'tiborsiz qoldiriladi.
    /// </summary>
    public IReadOnlySet<long> AdminIds { get; }

    public static IReadOnlySet<long> ParseAdminIds(string? raw) =>
        (raw ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(a => long.TryParse(a, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToHashSet();

    public bool IsAdmin(TgUser? user) => user is not null && AdminIds.Contains(user.Id);

    public bool Enabled => Token is not null;

    /// <summary>Sayt HTTPS'da bo'lsa — Mini App sifatida (Telegram ichida) ochiladi.</summary>
    public bool WebAppEnabled => FrontendUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Sayt sahifasiga tugma: Mini App bo'lsa Telegram ichida ochiladi (sahifa
    /// "?tgroute=" orqali — Telegram o'z parametrlarini manzilning # qismiga
    /// qo'shadi, shuning uchun marshrutni # da berib bo'lmaydi); aks holda oddiy havola.
    /// </summary>
    public TgButton SiteButton(string label, string route) => WebAppEnabled
        ? new TgButton(label, WebApp: new TgWebAppInfo($"{FrontendUrl}/?tgroute={Uri.EscapeDataString(route)}"))
        : new TgButton(label, Url: $"{FrontendUrl}/#/{route}");

    /// <summary>
    /// Telegram har bir webhook so'rovida X-Telegram-Bot-Api-Secret-Token
    /// sarlavhasini yuboradi. Uni tekshirib, so'rov haqiqatan Telegram'dan
    /// kelganini bilamiz. Faqat A-Z, a-z, 0-9, _ va - ruxsat etilgan.
    /// </summary>
    public string? WebhookSecret => Token is null
        ? null
        : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("webhook:" + Token)))[..48];

    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

/// <summary>Telegram'ga javob qaytarganda "bot bloklangan" kabi holatlarni ajratish uchun.</summary>
public class TelegramApiException(int status, string description) : Exception($"Telegram {status}: {description}")
{
    public int Status { get; } = status;

    /// <summary>Foydalanuvchi botni bloklagan yoki chatni o'chirgan — unga boshqa yozib bo'lmaydi.</summary>
    public bool ChatGone => Status == 403 || description.Contains("chat not found", StringComparison.OrdinalIgnoreCase);
}

public interface ITelegramApi
{
    Task<long?> SendMessageAsync(long chatId, string html, IReadOnlyList<IReadOnlyList<TgButton>>? inline = null, IReadOnlyList<IReadOnlyList<string>>? replyKeyboard = null, CancellationToken ct = default);
    Task EditMessageAsync(long chatId, long messageId, string html, IReadOnlyList<IReadOnlyList<TgButton>>? inline = null, CancellationToken ct = default);
    /// <summary>Faqat tugmalarni almashtirish (null — olib tashlash). Fayl xabarlari uchun ham ishlaydi.</summary>
    Task EditButtonsAsync(long chatId, long messageId, IReadOnlyList<IReadOnlyList<TgButton>>? inline, CancellationToken ct = default);
    Task AnswerCallbackAsync(string callbackId, string? text = null, CancellationToken ct = default);
    Task SetWebhookAsync(string url, string secret, CancellationToken ct = default);
    Task SetCommandsAsync(IReadOnlyList<(string Command, string Description)> commands, string? languageCode, CancellationToken ct = default);
    Task<TgUser> GetMeAsync(CancellationToken ct = default);
    Task<TgWebhookInfo> GetWebhookInfoAsync(CancellationToken ct = default);
    /// <summary>Fayl mazmuni (getFile + yuklab olish). Bot API cheklovi — 20 MB.</summary>
    Task<byte[]> DownloadFileAsync(string fileId, CancellationToken ct = default);
    /// <summary>Fayl yuborish (masalan, testning to'liq matni .txt sifatida).</summary>
    Task SendDocumentAsync(long chatId, string fileName, byte[] content, string? captionHtml = null, IReadOnlyList<IReadOnlyList<TgButton>>? inline = null, CancellationToken ct = default);
}
