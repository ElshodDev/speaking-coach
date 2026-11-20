using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpeakingCoach.Infrastructure.Telegram;

// ---- Telegram Bot API: faqat bizga kerak bo'lgan maydonlar ----

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
