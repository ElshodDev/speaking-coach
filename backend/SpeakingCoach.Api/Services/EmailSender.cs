using System.Net;
using System.Net.Http.Json;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services;

public interface IEmailSender
{
    /// <summary>Xat yuborish sozlanganmi. Sozlanmagan bo'lsa — email tasdiqlash o'chiq (ilova avvalgidek ishlaydi).</summary>
    bool IsConfigured { get; }

    Task SendAsync(string to, EmailMessage message, CancellationToken ct = default);
}

public record EmailMessage(string Subject, string Text, string Html);

/// <summary>
/// Brevo (sobiq Sendinblue) HTTP API orqali xat yuborish. Nega SMTP emas:
/// Render'ning bepul tarifi 2025-yil sentabridan beri 25/465/587 SMTP
/// portlariga chiqishni yopgan — shuning uchun HTTPS (443) orqali API.
/// Sozlamalar: Email:BrevoApiKey, Email:FromAddress (Brevo'da tasdiqlangan
/// yuboruvchi), Email:FromName.
///
/// Email:DevMode=true (faqat lokalda) — xat yuborilmaydi, kod server
/// konsoliga yoziladi: Brevo hisobisiz ham butun oqimni sinash mumkin.
/// </summary>
public class BrevoEmailSender : IEmailSender
{
    private const string Endpoint = "https://api.brevo.com/v3/smtp/email";

    private readonly HttpClient _http;
    private readonly ILogger<BrevoEmailSender> _logger;
    private readonly string? _apiKey;
    private readonly string? _fromAddress;
    private readonly string _fromName;
    private readonly bool _devMode;

    public BrevoEmailSender(IConfiguration config, IHttpClientFactory httpClientFactory, IHostEnvironment env, ILogger<BrevoEmailSender> logger)
    {
        _http = httpClientFactory.CreateClient();
        _logger = logger;
        _apiKey = config["Email:BrevoApiKey"];
        _fromAddress = config["Email:FromAddress"];
        _fromName = config["Email:FromName"] ?? "Speaking Coach";
        // DevMode faqat Development muhitida — production'da tasodifan yoqilib,
        // kodlar logga tushib qolmasin.
        _devMode = env.IsDevelopment() && string.Equals(config["Email:DevMode"], "true", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsConfigured => _devMode || (!string.IsNullOrWhiteSpace(_apiKey) && !string.IsNullOrWhiteSpace(_fromAddress));

    public async Task SendAsync(string to, EmailMessage message, CancellationToken ct = default)
    {
        if (_devMode && string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("[Email:DevMode] {To} ga xat: {Subject}\n{Text}", to, message.Subject, message.Text);
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
        {
            Content = JsonContent.Create(new
            {
                sender = new { name = _fromName, email = _fromAddress },
                to = new[] { new { email = to } },
                subject = message.Subject,
                textContent = message.Text,
                htmlContent = message.Html,
            }),
        };
        request.Headers.Add("api-key", _apiKey);
        request.Headers.Accept.ParseAdd("application/json");

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Brevo xato: {Status} {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException($"Brevo {(int)response.StatusCode}");
        }
    }
}

/// <summary>Kod xatlari uch tilda. Sof funksiya — testlanadi.</summary>
public static class EmailTemplates
{
    public static EmailMessage Code(string lang, EmailCodePurpose purpose, string code)
    {
        var minutes = (int)EmailCodeRules.Lifetime.TotalMinutes;
        var verify = purpose == EmailCodePurpose.Verify;
        var (subject, intro, validity, ignore) = lang switch
        {
            "ru" => (
                verify ? $"{code} — код подтверждения Speaking Coach" : $"{code} — код для сброса пароля Speaking Coach",
                verify ? "Ваш код для подтверждения email:" : "Ваш код для сброса пароля:",
                $"Код действует {minutes} минут.",
                "Если это были не вы, просто проигнорируйте это письмо."),
            "en" => (
                verify ? $"{code} is your Speaking Coach verification code" : $"{code} is your Speaking Coach password reset code",
                verify ? "Your code to confirm your email:" : "Your code to reset your password:",
                $"The code is valid for {minutes} minutes.",
                "If this wasn't you, just ignore this email."),
            _ => (
                verify ? $"{code} — Speaking Coach tasdiqlash kodi" : $"{code} — Speaking Coach parolni tiklash kodi",
                verify ? "Emailingizni tasdiqlash uchun kod:" : "Parolni tiklash uchun kod:",
                $"Kod {minutes} daqiqa amal qiladi.",
                "Agar bu siz boʻlmasangiz, xatga eʼtibor bermang."),
        };

        var text = $"{intro}\n\n{code}\n\n{validity}\n{ignore}\n\n— Speaking Coach";
        Func<string, string> safe = s => WebUtility.HtmlEncode(s);
        var html = $"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:480px;margin:0 auto;padding:24px;color:#0f172a">
              <p style="font-size:16px;margin:0 0 16px">{safe(intro)}</p>
              <p style="font-size:34px;font-weight:700;letter-spacing:8px;margin:0 0 16px;color:#2563eb">{safe(code)}</p>
              <p style="font-size:14px;color:#475569;margin:0 0 4px">{safe(validity)}</p>
              <p style="font-size:14px;color:#475569;margin:0 0 24px">{safe(ignore)}</p>
              <p style="font-size:13px;color:#94a3b8;margin:0">Speaking Coach</p>
            </div>
            """;
        return new EmailMessage(subject, text, html);
    }
}
