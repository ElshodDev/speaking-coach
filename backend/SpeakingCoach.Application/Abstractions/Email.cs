using System.Net;
using System.Net.Http.Json;

namespace SpeakingCoach.Application.Abstractions;

public interface IEmailSender
{
    /// <summary>Xat yuborish sozlanganmi. Sozlanmagan bo'lsa — email tasdiqlash o'chiq (ilova avvalgidek ishlaydi).</summary>
    bool IsConfigured { get; }

    Task SendAsync(string to, EmailMessage message, CancellationToken ct = default);
}

public record EmailMessage(string Subject, string Text, string Html);

/// <summary>Kod xatlari uch tilda. Sof funksiya — testlanadi.</summary>
public static class EmailTemplates
{
    public static EmailMessage Code(string lang, EmailCodePurpose purpose, string code)
    {
        var minutes = (int)EmailCodeRules.Lifetime.TotalMinutes;
        var (subject, intro, validity, ignore) = lang switch
        {
            "ru" => (
                purpose switch
                {
                    EmailCodePurpose.Verify => $"{code} — код подтверждения Speaking Coach",
                    EmailCodePurpose.Login => $"{code} — код для входа в Speaking Coach",
                    _ => $"{code} — код для сброса пароля Speaking Coach",
                },
                purpose switch
                {
                    EmailCodePurpose.Verify => "Ваш код для подтверждения email:",
                    EmailCodePurpose.Login => "Ваш код для входа:",
                    _ => "Ваш код для сброса пароля:",
                },
                $"Код действует {minutes} минут.",
                "Если это были не вы, просто проигнорируйте это письмо."),
            "en" => (
                purpose switch
                {
                    EmailCodePurpose.Verify => $"{code} is your Speaking Coach verification code",
                    EmailCodePurpose.Login => $"{code} is your Speaking Coach sign-in code",
                    _ => $"{code} is your Speaking Coach password reset code",
                },
                purpose switch
                {
                    EmailCodePurpose.Verify => "Your code to confirm your email:",
                    EmailCodePurpose.Login => "Your sign-in code:",
                    _ => "Your code to reset your password:",
                },
                $"The code is valid for {minutes} minutes.",
                "If this wasn't you, just ignore this email."),
            _ => (
                purpose switch
                {
                    EmailCodePurpose.Verify => $"{code} — Speaking Coach tasdiqlash kodi",
                    EmailCodePurpose.Login => $"{code} — Speaking Coach'ga kirish kodi",
                    _ => $"{code} — Speaking Coach parolni tiklash kodi",
                },
                purpose switch
                {
                    EmailCodePurpose.Verify => "Emailingizni tasdiqlash uchun kod:",
                    EmailCodePurpose.Login => "Kirish uchun kod:",
                    _ => "Parolni tiklash uchun kod:",
                },
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
