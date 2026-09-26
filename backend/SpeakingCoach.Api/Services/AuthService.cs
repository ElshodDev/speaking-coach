using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services;

/// <summary>
/// Ro'yxatdan o'tish/kirish natijasi: token, YOKI "emailni tasdiqlang"
/// (NeedsVerification), YOKI xato KALITI (Texts) va parametrlari — matn so'rov
/// tilida endpoint'da tuziladi. Status — xato bo'lsa HTTP holat kodi.
/// </summary>
public record AuthResult(
    string? Token,
    string? Email,
    string? Error,
    object?[]? ErrorArgs = null,
    bool NeedsVerification = false,
    int Status = StatusCodes.Status400BadRequest)
{
    public static AuthResult Fail(string errorKey, params object?[] args) => new(null, null, errorKey, args);
    public static AuthResult FailWith(int status, string errorKey, params object?[] args) => new(null, null, errorKey, args, false, status);
    public static AuthResult VerifyEmail(string email) => new(null, email, null, null, true);
    public static AuthResult Done(string? email = null) => new(null, email, null);
}

public class AuthService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);
    private const int MinPasswordLength = 8;
    private const int MaxPasswordLength = 128;

    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _hasher;
    private readonly IEmailSender _email;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext db, IPasswordHasher<User> hasher, IEmailSender email, ILogger<AuthService> logger)
    {
        _db = db;
        _hasher = hasher;
        _email = email;
        _logger = logger;
    }

    /// <summary>
    /// Email tasdiqlash faqat xat yuborish sozlanganda yoqiladi. Sozlanmagan
    /// bo'lsa (masalan, Brevo kaliti hali qo'shilmagan) — ilova avvalgidek
    /// ishlaydi, hech kim tizimdan "qulflanib" qolmaydi.
    /// </summary>
    public bool VerificationRequired => _email.IsConfigured;

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public async Task<AuthResult> RegisterAsync(string email, string password, string lang = Texts.DefaultLang)
    {
        var normalized = NormalizeEmail(email ?? "");
        if (normalized.Length > 256 || !System.Net.Mail.MailAddress.TryCreate(normalized, out _) || !normalized.Contains('.'))
        {
            return AuthResult.Fail("auth.invalid_email");
        }
        var passwordError = CheckPassword(password);
        if (passwordError is not null) return passwordError;

        var existing = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalized);
        if (existing is not null)
        {
            // Tasdiqlanmagan hisob — hech kim emailga egaligini isbotlamagan.
            // Kimdir begona email bilan ro'yxatdan o'tib, uni "band qilib"
            // qo'ymasligi uchun: yangi parol yoziladi va kod qayta yuboriladi.
            // Hisobga faqat email egasi (kodni oluvchi) kira oladi.
            if (!VerificationRequired || existing.EmailVerifiedAtUtc is not null)
            {
                return AuthResult.Fail("auth.email_taken");
            }
            existing.PasswordHash = _hasher.HashPassword(existing, password);
            await _db.SaveChangesAsync();
            return await SendCodeAsync(existing, EmailCodePurpose.Verify, lang, quietCooldown: true)
                ?? AuthResult.VerifyEmail(existing.Email);
        }

        var user = new User { Id = Guid.NewGuid(), Email = normalized, CreatedAtUtc = DateTime.UtcNow };
        user.PasswordHash = _hasher.HashPassword(user, password);
        _db.Users.Add(user);

        if (VerificationRequired)
        {
            await _db.SaveChangesAsync();
            return await SendCodeAsync(user, EmailCodePurpose.Verify, lang, quietCooldown: false)
                ?? AuthResult.VerifyEmail(user.Email);
        }

        var token = AddSession(user.Id);
        // Foydalanuvchi va sessiya bitta SaveChanges'da — ya'ni bitta
        // tranzaksiyada yoziladi: yo ikkalasi ham saqlanadi, yo hech biri.
        await _db.SaveChangesAsync();
        return new AuthResult(token, user.Email, null);
    }

    public async Task<AuthResult> LoginAsync(string email, string password, string lang = Texts.DefaultLang)
    {
        var normalized = NormalizeEmail(email ?? "");
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalized);

        // Email topilmasa ham, parol noto'g'ri bo'lsa ham — BIR XIL xabar.
        // Aks holda hujumchi "bu email ro'yxatdan o'tganmi?" degan savolga
        // javob olib, email'larni birma-bir tekshirib chiqishi mumkin edi.
        const string wrongCredentials = "auth.wrong_credentials";
        // PasswordHash bo'sh — hisob faqat Google orqali ochilgan, paroli yo'q
        // (xohlasa "Parolni unutdim" orqali parol o'rnatadi).
        if (user is null || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(user.PasswordHash))
        {
            return AuthResult.FailWith(StatusCodes.Status401Unauthorized, wrongCredentials);
        }

        var check = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (check == PasswordVerificationResult.Failed)
        {
            return AuthResult.FailWith(StatusCodes.Status401Unauthorized, wrongCredentials);
        }
        if (check == PasswordVerificationResult.SuccessRehashNeeded)
        {
            // Xeshlash algoritmi kuchaytirilgan bo'lsa (masalan .NET yangi
            // versiyasida takrorlashlar soni oshgan), parolni yangi usulda
            // qayta xeshlab qo'yamiz — foydalanuvchi buni sezmaydi.
            user.PasswordHash = _hasher.HashPassword(user, password);
        }

        // Parol to'g'ri, lekin email hali tasdiqlanmagan (yangi yoki bu
        // funksiyadan oldin ochilgan hisob) — avval kod.
        if (VerificationRequired && user.EmailVerifiedAtUtc is null)
        {
            await _db.SaveChangesAsync();
            return await SendCodeAsync(user, EmailCodePurpose.Verify, lang, quietCooldown: true)
                ?? AuthResult.VerifyEmail(user.Email);
        }

        var token = AddSession(user.Id);
        await _db.SaveChangesAsync();
        return new AuthResult(token, user.Email, null);
    }

    /// <summary>
    /// Google orqali kirish. Google emailni o'zi tasdiqlagan (email_verified),
    /// shuning uchun kod shart emas. Shu email bilan hisob bor bo'lsa — o'sha
    /// hisobga kiriladi (va email tasdiqlangan deb belgilanadi); bo'lmasa —
    /// parolsiz yangi hisob ochiladi.
    /// </summary>
    public async Task<AuthResult> GoogleSignInAsync(GooglePayload google)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == google.Email);
        var now = DateTime.UtcNow;
        if (user is null)
        {
            user = new User { Id = Guid.NewGuid(), Email = google.Email, CreatedAtUtc = now, PasswordHash = "" };
            _db.Users.Add(user);
        }
        else if (user.EmailVerifiedAtUtc is null)
        {
            // Hisob ochilgan, lekin email hech qachon tasdiqlanmagan — parolni
            // begona odam (email egasi emas) qo'ygan bo'lishi mumkin. Haqiqiy
            // egasi Google orqali keldi: o'sha parol va eski sessiyalar bekor.
            user.PasswordHash = "";
            _db.Sessions.RemoveRange(await _db.Sessions.Where(x => x.UserId == user.Id).ToListAsync());
        }
        user.EmailVerifiedAtUtc ??= now;

        var token = AddSession(user.Id);
        await _db.SaveChangesAsync();
        return new AuthResult(token, user.Email, null);
    }

    /// <summary>Emailga kelgan kod bilan tasdiqlash. Muvaffaqiyatli bo'lsa — darhol kirilgan holat (token).</summary>
    public async Task<AuthResult> VerifyEmailAsync(string email, string code)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == NormalizeEmail(email ?? ""));
        if (user is null) return AuthResult.Fail("code.wrong");

        var failure = await ConsumeCodeAsync(user, EmailCodePurpose.Verify, code);
        if (failure is not null) return failure;

        user.EmailVerifiedAtUtc ??= DateTime.UtcNow;
        var token = AddSession(user.Id);
        await _db.SaveChangesAsync();
        return new AuthResult(token, user.Email, null);
    }

    /// <summary>
    /// Kodni qayta yuborish. Email topilmasa ham "yuborildi" deymiz — kimdir
    /// bu yo'l bilan qaysi emaillar ro'yxatdan o'tganini bilib olmasin.
    /// </summary>
    public async Task<AuthResult> ResendAsync(string email, EmailCodePurpose purpose, string lang)
    {
        if (!VerificationRequired) return AuthResult.FailWith(StatusCodes.Status503ServiceUnavailable, "email.unavailable");
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == NormalizeEmail(email ?? ""));
        if (user is null) return AuthResult.Done();
        if (purpose == EmailCodePurpose.Verify && user.EmailVerifiedAtUtc is not null) return AuthResult.Done();
        return await SendCodeAsync(user, purpose, lang, quietCooldown: false) ?? AuthResult.Done();
    }

    /// <summary>"Parolni unutdim": parolni tiklash kodini yuboradi.</summary>
    public Task<AuthResult> ForgotPasswordAsync(string email, string lang) =>
        ResendAsync(email, EmailCodePurpose.ResetPassword, lang);

    /// <summary>
    /// Kod bilan yangi parol o'rnatadi. Barcha eski sessiyalar bekor qilinadi
    /// (boshqa qurilmalardan chiqariladi) — parol o'g'irlangan bo'lsa ham,
    /// hujumchi tizimda qolmaydi. Kod emailga kelgani uchun email ham tasdiqlanadi.
    /// </summary>
    public async Task<AuthResult> ResetPasswordAsync(string email, string code, string newPassword)
    {
        var passwordError = CheckPassword(newPassword);
        if (passwordError is not null) return passwordError;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == NormalizeEmail(email ?? ""));
        if (user is null) return AuthResult.Fail("code.wrong");

        var failure = await ConsumeCodeAsync(user, EmailCodePurpose.ResetPassword, code);
        if (failure is not null) return failure;

        user.PasswordHash = _hasher.HashPassword(user, newPassword);
        user.EmailVerifiedAtUtc ??= DateTime.UtcNow;
        _db.Sessions.RemoveRange(await _db.Sessions.Where(s => s.UserId == user.Id).ToListAsync());
        var token = AddSession(user.Id);
        await _db.SaveChangesAsync();
        return new AuthResult(token, user.Email, null);
    }

    private static AuthResult? CheckPassword(string? password)
    {
        if (password is null || password.Length < MinPasswordLength)
        {
            return AuthResult.Fail("auth.password_short", MinPasswordLength);
        }
        if (password.Length > MaxPasswordLength)
        {
            return AuthResult.Fail("auth.password_long", MaxPasswordLength);
        }
        return null;
    }

    /// <summary>
    /// Yangi kod yaratib, emailga yuboradi. null — muvaffaqiyatli. quietCooldown:
    /// yaqinda kod yuborilgan bo'lsa, xato bermay jim o'tib ketadi (masalan,
    /// foydalanuvchi "Kirish"ni ikki marta bosdi — birinchi kod hali amal qiladi).
    /// </summary>
    private async Task<AuthResult?> SendCodeAsync(User user, EmailCodePurpose purpose, string lang, bool quietCooldown)
    {
        var now = DateTime.UtcNow;
        var recent = await _db.EmailCodes
            .Where(c => c.UserId == user.Id && c.CreatedAtUtc > now.AddHours(-1))
            .Select(c => c.CreatedAtUtc)
            .ToListAsync();
        if (!EmailCodeRules.CanSend(recent, now, out var wait))
        {
            return quietCooldown
                ? null
                : AuthResult.FailWith(StatusCodes.Status429TooManyRequests, "code.wait", (int)Math.Ceiling(wait.TotalSeconds));
        }

        // Eskirgan kodlarni tozalaymiz — jadval o'smasin.
        _db.EmailCodes.RemoveRange(await _db.EmailCodes
            .Where(c => c.UserId == user.Id && c.CreatedAtUtc < now.AddDays(-1))
            .ToListAsync());

        var code = EmailCodeRules.Generate();
        _db.EmailCodes.Add(new EmailCode
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Purpose = purpose,
            CodeHash = EmailCodeRules.Hash(user.Id, code),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(EmailCodeRules.Lifetime),
        });
        await _db.SaveChangesAsync();

        try
        {
            await _email.SendAsync(user.Email, EmailTemplates.Code(lang, purpose, code));
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kod xatini yuborib bo'lmadi: {UserId}", user.Id);
            return AuthResult.FailWith(StatusCodes.Status502BadGateway, "email.send_failed");
        }
    }

    /// <summary>Oxirgi kodni tekshiradi; noto'g'ri bo'lsa urinishni hisoblaydi. null — kod to'g'ri va ishlatildi.</summary>
    private async Task<AuthResult?> ConsumeCodeAsync(User user, EmailCodePurpose purpose, string code)
    {
        var now = DateTime.UtcNow;
        var latest = await _db.EmailCodes
            .Where(c => c.UserId == user.Id && c.Purpose == purpose)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync();

        var result = EmailCodeRules.Check(latest, user.Id, code, now);
        if (result == CodeCheck.Ok)
        {
            latest!.UsedAtUtc = now;
            return null;
        }
        if (result == CodeCheck.Wrong && latest is not null)
        {
            latest.Attempts++;
            await _db.SaveChangesAsync();
            var left = EmailCodeRules.MaxAttempts - latest.Attempts;
            return left > 0 ? AuthResult.Fail("code.wrong_left", left) : AuthResult.Fail("code.too_many");
        }
        return AuthResult.Fail(EmailCodeRules.KeyFor(result));
    }

    public async Task LogoutAsync(HttpRequest request)
    {
        var token = ReadBearerToken(request);
        if (token is null) return;
        var hash = HashToken(token);
        var session = await _db.Sessions.FirstOrDefaultAsync(s => s.TokenHash == hash);
        if (session is not null)
        {
            _db.Sessions.Remove(session);
            await _db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// So'rovdagi tokenga ko'ra foydalanuvchini topadi. Token yo'q, noto'g'ri
    /// yoki muddati o'tgan bo'lsa — null (ya'ni "mehmon").
    /// </summary>
    public async Task<User?> GetCurrentUserAsync(HttpRequest request)
    {
        var token = ReadBearerToken(request);
        if (token is null) return null;

        var hash = HashToken(token);
        var now = DateTime.UtcNow;
        var userId = await _db.Sessions
            .Where(s => s.TokenHash == hash && s.ExpiresAtUtc > now)
            .Select(s => (Guid?)s.UserId)
            .FirstOrDefaultAsync();
        if (userId is null) return null;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
        // Email tasdiqlanmagan hisobning (masalan, bu funksiyadan oldin
        // ochilgan) eski sessiyasi ham yaroqsiz — qayta kirib, kodni kiritadi.
        if (user is not null && VerificationRequired && user.EmailVerifiedAtUtc is null) return null;
        return user;
    }

    public async Task<Guid?> GetCurrentUserIdAsync(HttpRequest request) =>
        (await GetCurrentUserAsync(request))?.Id;

    private string AddSession(Guid userId)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _db.Sessions.Add(new Session
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(token),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.Add(SessionLifetime),
        });
        return token;
    }

    private static string? ReadBearerToken(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
        var token = header[prefix.Length..].Trim();
        return token.Length == 0 ? null : token;
    }

    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
