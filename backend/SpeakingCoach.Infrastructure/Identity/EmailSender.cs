using System.Net;
using System.Net.Http.Json;
namespace SpeakingCoach.Infrastructure.Identity;

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
        // Brevo osilib qolsa — so'rov (va rate limit joyi) uzoq band bo'lmasin; xato "email.send_failed" bo'ladi.
        _http.Timeout = TimeSpan.FromSeconds(10);
        _logger = logger;
        _apiKey = config["Email:BrevoApiKey"];
        _fromAddress = config["Email:FromAddress"];
        _fromName = config["Email:FromName"] ?? "FluentUZ";
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
