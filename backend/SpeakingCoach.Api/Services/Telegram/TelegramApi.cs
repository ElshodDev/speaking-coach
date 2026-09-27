using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpeakingCoach.Api.Services.Telegram;

// ---- Telegram Bot API: faqat bizga kerak bo'lgan maydonlar ----

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
    [property: JsonPropertyName("url"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Url = null);

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
        FrontendUrl = (Blank(config["FrontendOrigin"]) ?? "http://localhost:5173").TrimEnd('/');
        var admins = (config["Telegram:Admins"] ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(a => a.TrimStart('@'))
            .Where(a => a.Length > 0)
            .ToList();
        AdminIds = admins.Where(a => a.All(char.IsDigit) && long.TryParse(a, out _)).Select(long.Parse).ToHashSet();
        AdminUsernames = admins.Where(a => !a.All(char.IsDigit)).Select(a => a.ToLowerInvariant()).ToHashSet();
    }

    /// <summary>
    /// Telegram:Admins — botdagi adminlar (vergul bilan): Telegram ID (raqam,
    /// o'zgarmaydi — tavsiya) yoki @username. Sayt emaili (Admin:Emails) bilan
    /// bir qatorda ishlaydi: bot admini testlarni chop etadi va tasdiqlaydi.
    /// </summary>
    public IReadOnlySet<long> AdminIds { get; }
    public IReadOnlySet<string> AdminUsernames { get; }

    public bool IsAdmin(TgUser? user) =>
        user is not null && (AdminIds.Contains(user.Id) || (user.Username is { } u && AdminUsernames.Contains(u.ToLowerInvariant())));

    public bool Enabled => Token is not null;

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

/// <summary>
/// Telegram Bot API — oddiy HTTPS + JSON (api.telegram.org/bot&lt;token&gt;/METHOD).
/// Tashqi paket ishlatilmaydi: bizga 7 ta metod yetarli.
/// </summary>
public class TelegramApi : ITelegramApi
{
    private static readonly JsonSerializerOptions Json = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private readonly HttpClient _http;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramApi> _logger;

    public TelegramApi(IHttpClientFactory factory, TelegramOptions options, ILogger<TelegramApi> logger)
    {
        _http = factory.CreateClient();
        _http.Timeout = TimeSpan.FromSeconds(20);
        _options = options;
        _logger = logger;
    }

    private async Task<JsonElement> CallAsync(string method, object payload, CancellationToken ct)
    {
        if (_options.Token is null) throw new InvalidOperationException("Telegram:BotToken sozlanmagan");
        using var response = await _http.PostAsJsonAsync($"https://api.telegram.org/bot{_options.Token}/{method}", payload, Json, ct);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        if (!body.TryGetProperty("ok", out var ok) || !ok.GetBoolean())
        {
            var description = body.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
            _logger.LogWarning("Telegram {Method} xato: {Status} {Description}", method, (int)response.StatusCode, description);
            throw new TelegramApiException((int)response.StatusCode, description);
        }
        return body.GetProperty("result");
    }

    private static object? Markup(IReadOnlyList<IReadOnlyList<TgButton>>? inline, IReadOnlyList<IReadOnlyList<string>>? reply) =>
        inline is not null ? new { inline_keyboard = inline }
        : reply is not null ? new { keyboard = reply.Select(r => r.Select(t => new { text = t })), resize_keyboard = true, is_persistent = true }
        : null;

    public async Task<long?> SendMessageAsync(long chatId, string html, IReadOnlyList<IReadOnlyList<TgButton>>? inline = null, IReadOnlyList<IReadOnlyList<string>>? replyKeyboard = null, CancellationToken ct = default)
    {
        var result = await CallAsync("sendMessage", new
        {
            chat_id = chatId,
            text = html,
            parse_mode = "HTML",
            link_preview_options = new { is_disabled = true },
            reply_markup = Markup(inline, replyKeyboard),
        }, ct);
        return result.TryGetProperty("message_id", out var id) ? id.GetInt64() : null;
    }

    public async Task EditMessageAsync(long chatId, long messageId, string html, IReadOnlyList<IReadOnlyList<TgButton>>? inline = null, CancellationToken ct = default)
    {
        try
        {
            await CallAsync("editMessageText", new
            {
                chat_id = chatId,
                message_id = messageId,
                text = html,
                parse_mode = "HTML",
                link_preview_options = new { is_disabled = true },
                reply_markup = inline is null ? null : new { inline_keyboard = inline },
            }, ct);
        }
        catch (TelegramApiException ex) when (ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
        {
            // Tugma ikki marta bosildi — matn o'zgarmagan, xato emas.
        }
    }

    public async Task EditButtonsAsync(long chatId, long messageId, IReadOnlyList<IReadOnlyList<TgButton>>? inline, CancellationToken ct = default)
    {
        try
        {
            await CallAsync("editMessageReplyMarkup", new
            {
                chat_id = chatId,
                message_id = messageId,
                reply_markup = new { inline_keyboard = inline ?? [] },
            }, ct);
        }
        catch (TelegramApiException ex) when (ex.Message.Contains("message is not modified", StringComparison.OrdinalIgnoreCase))
        {
        }
    }

    public async Task AnswerCallbackAsync(string callbackId, string? text = null, CancellationToken ct = default) =>
        await CallAsync("answerCallbackQuery", new { callback_query_id = callbackId, text }, ct);

    public async Task SetWebhookAsync(string url, string secret, CancellationToken ct = default) =>
        await CallAsync("setWebhook", new
        {
            url,
            secret_token = secret,
            allowed_updates = new[] { "message", "callback_query" },
            max_connections = 10,
        }, ct);

    public async Task SetCommandsAsync(IReadOnlyList<(string Command, string Description)> commands, string? languageCode, CancellationToken ct = default) =>
        await CallAsync("setMyCommands", new
        {
            commands = commands.Select(c => new { command = c.Command, description = c.Description }),
            language_code = languageCode,
        }, ct);

    public async Task<TgUser> GetMeAsync(CancellationToken ct = default) =>
        (await CallAsync("getMe", new { }, ct)).Deserialize<TgUser>()!;

    public async Task<TgWebhookInfo> GetWebhookInfoAsync(CancellationToken ct = default) =>
        (await CallAsync("getWebhookInfo", new { }, ct)).Deserialize<TgWebhookInfo>()!;

    public async Task<byte[]> DownloadFileAsync(string fileId, CancellationToken ct = default)
    {
        var file = await CallAsync("getFile", new { file_id = fileId }, ct);
        var path = file.GetProperty("file_path").GetString() ?? throw new TelegramApiException(404, "file_path yo'q");
        using var response = await _http.GetAsync($"https://api.telegram.org/file/bot{_options.Token}/{path}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(ct);
    }

    public async Task SendDocumentAsync(long chatId, string fileName, byte[] content, string? captionHtml = null, IReadOnlyList<IReadOnlyList<TgButton>>? inline = null, CancellationToken ct = default)
    {
        if (_options.Token is null) throw new InvalidOperationException("Telegram:BotToken sozlanmagan");
        using var form = new MultipartFormDataContent
        {
            { new StringContent(chatId.ToString(System.Globalization.CultureInfo.InvariantCulture)), "chat_id" },
            { new ByteArrayContent(content), "document", fileName },
        };
        if (captionHtml is not null)
        {
            form.Add(new StringContent(captionHtml.Length > 1000 ? captionHtml[..1000] : captionHtml), "caption");
            form.Add(new StringContent("HTML"), "parse_mode");
        }
        if (inline is not null) form.Add(new StringContent(JsonSerializer.Serialize(new { inline_keyboard = inline }, Json)), "reply_markup");
        using var response = await _http.PostAsync($"https://api.telegram.org/bot{_options.Token}/sendDocument", form, ct);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        if (!body.TryGetProperty("ok", out var ok) || !ok.GetBoolean())
        {
            var description = body.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
            throw new TelegramApiException((int)response.StatusCode, description);
        }
    }
}
