using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services.Telegram;

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

/// <summary>Hisobga qaysi yo'llar bilan kirish mumkin (Profil → "Kirish usullari").</summary>
public record SignInMethods(bool Password, bool Google, bool Telegram, bool RealEmail);

/// <summary>"Telegram orqali kirish" poll natijasi: status + (tasdiqlangan bo'lsa) sessiya.</summary>
public record TelegramPollResult(string Status, string? Token = null, string? Email = null, bool IsNew = false);

/// <summary>Saytdan Telegram orqali kirish urinishi yaratildi.</summary>
public record TelegramLoginStart(Guid LoginId, string PollToken, string Nonce, string Code, int ExpiresInSeconds);

public class AuthService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);

    /// <summary>
    /// Sessiya "sirpanuvchi": muddatiga shundan kam qolganda (va foydalanuvchi
    /// faol bo'lsa) yana 30 kunga uzaytiriladi. 30 − 20 = 10 kun — ya'ni bitta
    /// sessiya uchun bazaga yozish ko'pi bilan 10 kunda bir marta.
    /// </summary>
    public static readonly TimeSpan RenewBelow = TimeSpan.FromDays(20);
    private const int MinPasswordLength = 8;
    private const int MaxPasswordLength = 128;

    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _hasher;
    private readonly IEmailSender _email;
    private readonly ILogger<AuthService> _logger;

    private readonly AdminOptions _admins;
    private readonly int _dailyCap;
    private readonly EmailDailyCap _cap = EmailDailyCap.Shared;

    public AuthService(AppDbContext db, IPasswordHasher<User> hasher, IEmailSender email, ILogger<AuthService> logger, AdminOptions admins, IConfiguration config)
    {
        _admins = admins;
        _dailyCap = EmailDailyCap.CapFrom(config);
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

    /// <summary>
    /// Xat yuborib bo'lmaydigan (haqiqiy bo'lmagan) email: Telegram orqali
    /// ochilgan hisob (tg-…@telegram.invalid), demo hisob va umuman ".invalid"
    /// domenidagi har qanday manzil (RFC 2606).
    /// </summary>
    public static bool IsSyntheticEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return true;
        var e = email.Trim();
        return e.EndsWith("@" + TelegramLoginRules.SyntheticDomain, StringComparison.OrdinalIgnoreCase)
            || e.EndsWith(".invalid", StringComparison.OrdinalIgnoreCase)
            || DemoAccount.IsDemo(e);
    }

    /// <summary>Bugungi xat limiti tugaganmi (Email:DailyCap).</summary>
    public bool EmailCapReached => _cap.IsReached(_dailyCap, DateTime.UtcNow);

    private static AuthResult CapError() => AuthResult.FailWith(StatusCodes.Status503ServiceUnavailable, "email.cap");

    /// <summary>
    /// Google bilan kirganda tasdiqlanmagan hisobning parolini o'chirish kerakmi.
    /// Faqat email tasdiqlash yoqilgan bo'lsa: unda tasdiqlanmagan hisobdagi
    /// parolni begona odam qo'ygan bo'lishi mumkin. Tasdiqlash o'chiq bo'lsa
    /// (xat yuborib bo'lmaydi) — hech kim emailini tasdiqlay olmagan, parol
    /// egasiniki; uni o'chirish odamni hisobidan qulflab qo'yardi.
    /// </summary>
    public static bool ShouldWipePasswordOnGoogle(bool emailVerified, bool verificationRequired) =>
        // Tasdiqlash o'chiq bo'lsa ham: tasdiqlanmagan hisobdagi parolni email egasi emas,
        // begona odam qo'ygan bo'lishi mumkin. Egasi parolni Profil → "Kirish usullari"da qayta o'rnatadi.
        !emailVerified;

    /// <summary>
    /// Sirpanuvchi sessiya: muddatiga <see cref="RenewBelow"/> dan kam qolgan
    /// bo'lsa — yangi muddat (hozir + 30 kun), aks holda null (yozish shart emas).
    /// </summary>
    public static DateTime? RenewedExpiry(DateTime expiresAtUtc, DateTime nowUtc) =>
        expiresAtUtc > nowUtc && expiresAtUtc - nowUtc < RenewBelow ? nowUtc.Add(SessionLifetime) : null;

    public async Task<AuthResult> RegisterAsync(string email, string password, string lang = Texts.DefaultLang)
    {
        var normalized = NormalizeEmail(email ?? "");
        if (!IsValidEmail(normalized))
        {
            return AuthResult.Fail("auth.invalid_email");
        }
        var passwordError = CheckPassword(password);
        if (passwordError is not null) return passwordError;

        // Email tasdiqlash o'chiq bo'lsa, admin emailini hech kim "egallab" olmasin —
        // egaligini isbotlab bo'lmaydi (admin Google orqali kiradi).
        if (!VerificationRequired && _admins.Emails.Contains(normalized))
        {
            return AuthResult.Fail("auth.email_taken");
        }
        // Bugungi xat limiti tugagan — hisob ochib, kodni yubora olmay qolmaylik.
        if (VerificationRequired && EmailCapReached) return CapError();

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
            // Parol BU YERDA almashtirilmaydi: aks holda begona odam egasi kod
            // kiritayotgan paytda o'z parolini yozib qo'yib, tasdiqlangan hisobni
            // egallab olardi. Parol kod bilan birga (VerifyEmailAsync) — kodni
            // olgan email egasi tomonidan o'rnatiladi.
            return await SendCodeAsync(existing, EmailCodePurpose.Verify, lang, quietCooldown: true)
                ?? AuthResult.VerifyEmail(existing.Email);
        }

        var user = new User { Id = Guid.NewGuid(), Email = normalized, CreatedAtUtc = DateTime.UtcNow, SignupMethod = SignupMethods.Email };
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
            user = new User { Id = Guid.NewGuid(), Email = google.Email, CreatedAtUtc = now, PasswordHash = "", SignupMethod = SignupMethods.Google };
            _db.Users.Add(user);
        }
        else if (ShouldWipePasswordOnGoogle(user.EmailVerifiedAtUtc is not null, VerificationRequired))
        {
            // Email tasdiqlash yoqilgan, hisob ochilgan, lekin email hech qachon
            // tasdiqlanmagan — parolni begona odam (email egasi emas) qo'ygan
            // bo'lishi mumkin. Haqiqiy egasi Google orqali keldi: o'sha parol va
            // eski sessiyalar bekor.
            user.PasswordHash = "";
            _db.Sessions.RemoveRange(await _db.Sessions.Where(x => x.UserId == user.Id).ToListAsync());
        }
        // Hech qachon tasdiqlanmagan "bo'sh" hisob (masalan, kod so'ralib, kiritilmagan) —
        // amalda Google orqali ochilgan hisoblanadi (statistika va "Kirish usullari").
        if (user.EmailVerifiedAtUtc is null && string.IsNullOrEmpty(user.PasswordHash)) user.SignupMethod = SignupMethods.Google;
        user.EmailVerifiedAtUtc ??= now;

        var token = AddSession(user.Id);
        await _db.SaveChangesAsync();
        return new AuthResult(token, user.Email, null);
    }

    /// <summary>
    /// Telegram foydalanuvchisining hisobi: chat botga ulangan bo'lsa — o'sha
    /// hisob; aks holda yangi hisob ochiladi (email — tg-ID@telegram.invalid,
    /// parolsiz, "tasdiqlangan": Telegram foydalanuvchini o'zi tasdiqlagan) va
    /// chat unga ulanadi. Shaxsiy chatda chat ID = foydalanuvchi ID.
    /// </summary>
    public async Task<(User User, bool IsNew)> ResolveTelegramUserAsync(TgUser tg, string signupMethod, string lang, CancellationToken ct = default)
    {
        var chatId = tg.Id;
        var account = await _db.TelegramAccounts.FirstOrDefaultAsync(a => a.ChatId == chatId, ct);
        if (account is not null)
        {
            var linked = await _db.Users.FirstOrDefaultAsync(u => u.Id == account.UserId, ct);
            if (linked is not null) return (linked, false);
        }

        var now = DateTime.UtcNow;
        var email = TelegramLoginRules.SyntheticEmail(tg.Id);
        // Avval Telegram orqali ochilgan, keyin botdan uzilgan hisob — qayta ulanadi.
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        var isNew = user is null;
        if (user is null)
        {
            var name = TelegramLoginRules.CleanDisplayName(tg.FirstName);
            if (name is not null)
            {
                // Taxallus band bo'lsa — qo'ymaymiz (aks holda Profilni saqlashda "band" xatosi chiqardi).
                var lower = name.ToLower();
                if (await _db.Users.AnyAsync(u => u.DisplayName != null && u.DisplayName.ToLower() == lower, ct)) name = null;
            }
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = "",
                CreatedAtUtc = now,
                EmailVerifiedAtUtc = now,
                DisplayName = name,
                SignupMethod = signupMethod,
            };
            _db.Users.Add(user);
        }

        // Bitta hisob — bitta chat.
        var userId = user.Id;
        _db.TelegramAccounts.RemoveRange(await _db.TelegramAccounts
            .Where(a => a.ChatId == chatId || a.UserId == userId).ToListAsync(ct));
        _db.TelegramAccounts.Add(new TelegramAccount
        {
            ChatId = chatId,
            UserId = userId,
            Username = TelegramLoginRules.Clip(tg.Username, 64),
            Lang = TelegramLoginRules.NormalizeLang(lang),
            TzOffsetMinutes = -300,
            ReminderHour = 20,
            LinkedAtUtc = now,
            LastSeenAtUtc = now,
        });

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Parallel so'rov (masalan, Mini App va bot bir vaqtda) shu chat
            // uchun hisobni ochib ulgurdi — o'shani olamiz.
            _db.ChangeTracker.Clear();
            var again = await _db.TelegramAccounts.FirstOrDefaultAsync(a => a.ChatId == chatId, ct);
            var existing = again is null ? null : await _db.Users.FirstOrDefaultAsync(u => u.Id == again.UserId, ct);
            if (existing is null) throw;
            return (existing, false);
        }
        if (isNew) _logger.LogInformation("Telegram orqali yangi hisob: {UserId} ({Method})", userId, signupMethod);
        return (user, isNew);
    }

    /// <summary>Mini App (initData tekshirilgan): hisob topiladi yoki ochiladi — yangi sessiya.</summary>
    public async Task<(AuthResult Result, bool IsNew)> SignInTelegramAsync(TgUser tg, string signupMethod, string lang)
    {
        var (user, isNew) = await ResolveTelegramUserAsync(tg, signupMethod, lang);
        var token = AddSession(user.Id);
        await _db.SaveChangesAsync();
        return (new AuthResult(token, user.Email, null), isNew);
    }

    // ---------------- Saytdan Telegram orqali kirish ----------------

    /// <summary>Yangi urinish: nonce (bot havolasi uchun), 2 xonali kod va poll token.</summary>
    public async Task<TelegramLoginStart> StartTelegramLoginAsync(string? lang, string? ip)
    {
        var now = DateTime.UtcNow;
        // Arzon tozalash: 1 kundan eski urinishlar (CreatedAtUtc indeksi bo'yicha).
        var old = now - TelegramLoginRules.Keep;
        await _db.TelegramLogins.Where(l => l.CreatedAtUtc < old).ExecuteDeleteAsync();

        var nonce = TelegramLoginRules.NewNonce();
        var poll = TelegramLoginRules.NewPollToken();
        var login = new TelegramLogin
        {
            Id = Guid.NewGuid(),
            NonceHash = TelegramLoginRules.Hash(nonce),
            PollTokenHash = TelegramLoginRules.Hash(poll),
            Code = TelegramLoginRules.NewCode(),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(TelegramLoginRules.Lifetime),
            Ip = TelegramLoginRules.Clip(ip, 64),
            Lang = TelegramLoginRules.NormalizeLang(lang),
        };
        _db.TelegramLogins.Add(login);
        await _db.SaveChangesAsync();
        return new TelegramLoginStart(login.Id, poll, nonce, login.Code, (int)TelegramLoginRules.Lifetime.TotalSeconds);
    }

    /// <summary>
    /// Sayt natijani so'raydi. Tasdiqlangan urinish bilan sessiya faqat BIR
    /// MARTA beriladi: ConsumedAtUtc shartli UPDATE bilan atomar belgilanadi
    /// (parallel ikki poll'dan faqat bittasi token oladi).
    /// </summary>
    public async Task<TelegramPollResult> PollTelegramLoginAsync(string? pollToken)
    {
        var expired = new TelegramPollResult(TelegramLoginRules.StatusName(TelegramLoginStatus.Expired));
        if (string.IsNullOrWhiteSpace(pollToken) || pollToken.Length > 128) return expired;
        var hash = TelegramLoginRules.Hash(pollToken.Trim());
        var now = DateTime.UtcNow;
        var login = await _db.TelegramLogins.AsNoTracking().FirstOrDefaultAsync(l => l.PollTokenHash == hash);
        if (login is null) return expired;

        var status = TelegramLoginRules.StatusOf(login, now);
        if (status != TelegramLoginStatus.Confirmed) return new TelegramPollResult(TelegramLoginRules.StatusName(status));

        var id = login.Id;
        var since = now - TelegramLoginRules.ConsumeWindow;
        var consumed = await _db.TelegramLogins
            .Where(l => l.Id == id && l.ConsumedAtUtc == null && l.RejectedAtUtc == null
                && l.ConfirmedAtUtc != null && l.ConfirmedAtUtc > since && l.UserId != null)
            .ExecuteUpdateAsync(set => set.SetProperty(l => l.ConsumedAtUtc, (DateTime?)now));
        if (consumed == 0) return expired;

        var userId = login.UserId!.Value;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null) return expired;
        var token = AddSession(user.Id);
        await _db.SaveChangesAsync();
        return new TelegramPollResult(TelegramLoginRules.StatusName(TelegramLoginStatus.Confirmed), token, user.Email, login.IsNewUser);
    }

    // ---------------- Kirish usullari, parol, email ----------------

    public async Task<SignInMethods> GetSignInMethodsAsync(User user)
    {
        var userId = user.Id;
        var telegram = await _db.TelegramAccounts.AnyAsync(a => a.UserId == userId);
        return new SignInMethods(
            Password: !string.IsNullOrEmpty(user.PasswordHash),
            Google: user.SignupMethod == SignupMethods.Google,
            Telegram: telegram,
            RealEmail: !IsSyntheticEmail(user.Email));
    }

    /// <summary>
    /// Parol o'rnatish yoki almashtirish (kirgan foydalanuvchi). Parol bor
    /// bo'lsa — joriysi to'g'ri kiritilishi shart. Faqat haqiqiy emailli
    /// hisobga: parol email bilan birga ishlatiladi. Boshqa qurilmalardagi
    /// sessiyalar bekor qilinadi (joriysi qoladi).
    /// </summary>
    public async Task<AuthResult> SetPasswordAsync(User user, string? currentPassword, string? newPassword, HttpRequest request)
    {
        if (IsSyntheticEmail(user.Email)) return AuthResult.Fail("account.need_email");
        var passwordError = CheckPassword(newPassword);
        if (passwordError is not null) return passwordError;

        if (!string.IsNullOrEmpty(user.PasswordHash))
        {
            var ok = !string.IsNullOrEmpty(currentPassword)
                && _hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword) != PasswordVerificationResult.Failed;
            if (!ok) return AuthResult.Fail("account.wrong_password");
        }

        user.PasswordHash = _hasher.HashPassword(user, newPassword!);
        var current = ReadBearerToken(request) is { } t ? HashToken(t) : "";
        var userId = user.Id;
        _db.Sessions.RemoveRange(await _db.Sessions.Where(s => s.UserId == userId && s.TokenHash != current).ToListAsync());
        await _db.SaveChangesAsync();
        return AuthResult.Done(user.Email);
    }

    /// <summary>Emailni almashtirish/qo'shish, 1-qadam: YANGI manzilga kod (shu manzilga bog'langan).</summary>
    public async Task<AuthResult> RequestEmailChangeAsync(User user, string? email, string lang)
    {
        if (DemoAccount.IsDemo(user.Email)) return AuthResult.FailWith(StatusCodes.Status403Forbidden, "demo.not_allowed");
        if (!VerificationRequired) return AuthResult.FailWith(StatusCodes.Status503ServiceUnavailable, "email.unavailable");
        var normalized = NormalizeEmail(email ?? "");
        if (!IsValidEmail(normalized)) return AuthResult.Fail("auth.invalid_email");
        if (normalized == user.Email) return AuthResult.Fail("account.email_same");
        if (await _db.Users.AnyAsync(u => u.Email == normalized)) return AuthResult.Fail("account.email_taken");
        return await SendCodeAsync(user, EmailCodePurpose.ChangeEmail, lang, quietCooldown: false, to: normalized)
            ?? AuthResult.Done(normalized);
    }

    /// <summary>Emailni almashtirish, 2-qadam: kod to'g'ri bo'lsa — yangi email (tasdiqlangan).</summary>
    public async Task<AuthResult> ConfirmEmailChangeAsync(User user, string? email, string? code)
    {
        var normalized = NormalizeEmail(email ?? "");
        if (!IsValidEmail(normalized)) return AuthResult.Fail("auth.invalid_email");

        var failure = await ConsumeCodeAsync(user, EmailCodePurpose.ChangeEmail, code ?? "", target: normalized);
        if (failure is not null) return failure;

        var userId = user.Id;
        if (await _db.Users.AnyAsync(u => u.Email == normalized && u.Id != userId)) return AuthResult.Fail("account.email_taken");
        user.Email = normalized;
        user.EmailVerifiedAtUtc = DateTime.UtcNow;
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Unique indeks: shu lahzada boshqa hisob bu emailni oldi.
            return AuthResult.Fail("account.email_taken");
        }
        _logger.LogInformation("Email almashtirildi: {UserId}", userId);
        return AuthResult.Done(normalized);
    }

    /// <summary>Emailga kelgan kod bilan tasdiqlash. Muvaffaqiyatli bo'lsa — darhol kirilgan holat (token).</summary>
    /// <remarks>
    /// Parol ham shu yerda — kod bilan birga — o'rnatiladi: kodni faqat email
    /// egasi oladi, demak parolni ham faqat u belgilaydi. (Avval parol ro'yxatdan
    /// o'tishda yozilardi va tasdiqlanmagan hisobga begona odam o'z parolini
    /// qo'yib ulgurishi mumkin edi.)
    /// </remarks>
    public async Task<AuthResult> VerifyEmailAsync(string email, string code, string? password)
    {
        var passwordError = CheckPassword(password ?? "");
        if (passwordError is not null) return passwordError;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == NormalizeEmail(email ?? ""));
        if (user is null) return AuthResult.Fail("code.wrong");

        var failure = await ConsumeCodeAsync(user, EmailCodePurpose.Verify, code);
        if (failure is not null) return failure;

        if (user.EmailVerifiedAtUtc is null)
        {
            user.PasswordHash = _hasher.HashPassword(user, password!);
            // Tasdiqlanmagan paytda ochilgan sessiyalar (bo'lsa) — bekor.
            _db.Sessions.RemoveRange(await _db.Sessions.Where(x => x.UserId == user.Id).ToListAsync());
        }
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
        var normalized = NormalizeEmail(email ?? "");
        // Sintetik manzilga (Telegram/demo) xat yuborilmaydi — javob esa bir xil.
        if (IsSyntheticEmail(normalized)) return AuthResult.Done();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalized);
        if (user is null) return AuthResult.Done();
        if (purpose == EmailCodePurpose.Verify && user.EmailVerifiedAtUtc is not null) return AuthResult.Done();
        return await SendCodeAsync(user, purpose, lang, quietCooldown: false) ?? AuthResult.Done();
    }

    /// <summary>
    /// Parolsiz kirish, 1-qadam: emailga kirish kodi. Hisob bo'lmasa — shu yerda
    /// ochiladi (parolsiz, tasdiqlanmagan), shuning uchun mavjud va yangi email
    /// uchun javob bir xil: kimdir bu yo'l bilan qaysi emaillar ro'yxatdan
    /// o'tganini bila olmaydi. Hisobga faqat kodni olgan email egasi kiradi.
    /// </summary>
    public async Task<AuthResult> RequestLoginCodeAsync(string email, string lang)
    {
        if (!VerificationRequired) return AuthResult.FailWith(StatusCodes.Status503ServiceUnavailable, "email.unavailable");
        var normalized = NormalizeEmail(email ?? "");
        if (!IsValidEmail(normalized)) return AuthResult.Fail("auth.invalid_email");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalized);
        if (user is null)
        {
            if (EmailCapReached) return CapError();
            user = new User { Id = Guid.NewGuid(), Email = normalized, CreatedAtUtc = DateTime.UtcNow, PasswordHash = "", SignupMethod = SignupMethods.Code };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }
        return await SendCodeAsync(user, EmailCodePurpose.Login, lang, quietCooldown: false) ?? AuthResult.Done(user.Email);
    }

    /// <summary>
    /// Parolsiz kirish, 2-qadam: kod to'g'ri bo'lsa — sessiya. Kod emailga
    /// kelgani uchun email tasdiqlangan hisoblanadi. Hisob avval tasdiqlanmagan
    /// bo'lsa (parolni begona odam qo'ygan bo'lishi mumkin) — Google'dagidek,
    /// o'sha parol va eski sessiyalar bekor qilinadi.
    /// </summary>
    public async Task<AuthResult> LoginWithCodeAsync(string email, string code)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == NormalizeEmail(email ?? ""));
        if (user is null || IsSyntheticEmail(user.Email)) return AuthResult.Fail("code.wrong");

        var failure = await ConsumeCodeAsync(user, EmailCodePurpose.Login, code);
        if (failure is not null) return failure;

        if (user.EmailVerifiedAtUtc is null)
        {
            user.PasswordHash = "";
            _db.Sessions.RemoveRange(await _db.Sessions.Where(x => x.UserId == user.Id).ToListAsync());
            user.EmailVerifiedAtUtc = DateTime.UtcNow;
        }
        var token = AddSession(user.Id);
        await _db.SaveChangesAsync();
        return new AuthResult(token, user.Email, null);
    }

    /// <summary>Ro'yxatdan o'tish va kod bilan kirish uchun umumiy tekshiruv (demo manzillar ham rad etiladi).</summary>
    /// <remarks>Sintetik manzillar (tg-…@telegram.invalid, demo, har qanday ".invalid") ham rad etiladi.</remarks>
    public static bool IsValidEmail(string normalized) =>
        normalized.Length <= 256 && System.Net.Mail.MailAddress.TryCreate(normalized, out var parsed)
        && parsed.Address == normalized && normalized.Contains('.') && !IsSyntheticEmail(normalized);

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
    private async Task<AuthResult?> SendCodeAsync(User user, EmailCodePurpose purpose, string lang, bool quietCooldown, string? to = null)
    {
        to ??= user.Email;
        // Demo va Telegram hisoblarining manzili yo'q (".invalid") — xat yuborilmaydi.
        if (IsSyntheticEmail(to)) return null;

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

        // Butun server bo'yicha kunlik limit (Brevo bepul tarifi).
        if (!_cap.TryTake(_dailyCap, now)) return CapError();

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
            // Emailni almashtirish kodi yangi manzilga bog'lanadi.
            CodeHash = EmailCodeRules.Hash(user.Id, code, purpose == EmailCodePurpose.ChangeEmail ? to : null),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(EmailCodeRules.Lifetime),
        });
        await _db.SaveChangesAsync();

        try
        {
            // Yangi manzilni tasdiqlash xati — "emailni tasdiqlash" shabloni bilan bir xil.
            var template = purpose == EmailCodePurpose.ChangeEmail ? EmailCodePurpose.Verify : purpose;
            await _email.SendAsync(to, EmailTemplates.Code(lang, template, code));
            return null;
        }
        catch (Exception ex)
        {
            _cap.Release(now);
            _logger.LogError(ex, "Kod xatini yuborib bo'lmadi: {UserId}", user.Id);
            return AuthResult.FailWith(StatusCodes.Status502BadGateway, "email.send_failed");
        }
    }

    /// <summary>
    /// Oxirgi kodni tekshiradi. null — kod to'g'ri va ishlatildi.
    /// Urinish kodni solishtirishdan OLDIN bazada atomar band qilinadi
    /// ("Attempts &lt; 5" sharti bilan bitta UPDATE): bir vaqtda yuborilgan
    /// yuzlab so'rov ham 5 tadan ortiq taxmin qila olmaydi.
    /// </summary>
    private async Task<AuthResult?> ConsumeCodeAsync(User user, EmailCodePurpose purpose, string code, string? target = null)
    {
        var now = DateTime.UtcNow;
        var latest = await _db.EmailCodes
            .Where(c => c.UserId == user.Id && c.Purpose == purpose)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync();

        var pre = EmailCodeRules.Precheck(latest, now);
        if (pre is not null) return AuthResult.Fail(EmailCodeRules.KeyFor(pre.Value));

        var id = latest!.Id;
        var reserved = await _db.EmailCodes
            .Where(c => c.Id == id && c.UsedAtUtc == null && c.Attempts < EmailCodeRules.MaxAttempts)
            .ExecuteUpdateAsync(set => set.SetProperty(c => c.Attempts, c => c.Attempts + 1));
        if (reserved == 0) return AuthResult.Fail("code.too_many");
        var attempts = latest.Attempts + 1;

        if (EmailCodeRules.Matches(latest, user.Id, code, target))
        {
            // Ikki parallel to'g'ri so'rovdan faqat bittasi ishlatadi.
            var used = await _db.EmailCodes
                .Where(c => c.Id == id && c.UsedAtUtc == null)
                .ExecuteUpdateAsync(set => set.SetProperty(c => c.UsedAtUtc, now));
            return used == 1 ? null : AuthResult.Fail(EmailCodeRules.KeyFor(CodeCheck.NotFound));
        }
        var left = EmailCodeRules.MaxAttempts - attempts;
        return left > 0 ? AuthResult.Fail("code.wrong_left", left) : AuthResult.Fail("code.too_many");
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
        var session = await _db.Sessions
            .Where(s => s.TokenHash == hash && s.ExpiresAtUtc > now)
            .Select(s => new { s.Id, s.UserId, s.ExpiresAtUtc })
            .FirstOrDefaultAsync();
        if (session is null) return null;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == session.UserId);
        if (user is null) return null;
        // Email tasdiqlanmagan hisobning (masalan, bu funksiyadan oldin
        // ochilgan) eski sessiyasi ham yaroqsiz — qayta kirib, kodni kiritadi.
        if (VerificationRequired && user.EmailVerifiedAtUtc is null) return null;

        // Sirpanuvchi sessiya (demo hisob — 24 soat, uzaytirilmaydi). Shartli
        // UPDATE: bir vaqtda kelgan so'rovlardan faqat bittasi yozadi.
        if (!DemoAccount.IsDemo(user.Email) && RenewedExpiry(session.ExpiresAtUtc, now) is { } renewed)
        {
            var sessionId = session.Id;
            var threshold = now + RenewBelow;
            try
            {
                await _db.Sessions
                    .Where(s => s.Id == sessionId && s.ExpiresAtUtc < threshold)
                    .ExecuteUpdateAsync(set => set.SetProperty(s => s.ExpiresAtUtc, renewed));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Uzaytirib bo'lmadi — muhim emas, sessiya hali amal qiladi.
                _logger.LogWarning(ex, "Sessiyani uzaytirib bo'lmadi: {UserId}", user.Id);
            }
        }
        return user;
    }

    public async Task<Guid?> GetCurrentUserIdAsync(HttpRequest request) =>
        (await GetCurrentUserAsync(request))?.Id;

    /// <summary>
    /// Demo hisob: yangi vaqtinchalik foydalanuvchi + tayyor ma'lumotlar (DemoSeed),
    /// sessiya 24 soat. Parol yo'q — faqat shu token bilan kiriladi.
    /// </summary>
    public async Task<AuthResult> CreateDemoAsync(int tzOffsetMinutes)
    {
        var now = DateTime.UtcNow;
        tzOffsetMinutes = Math.Clamp(tzOffsetMinutes, -14 * 60, 12 * 60);

        // Muddati o'tgan demo hisoblar (cron ishlamay qolgan bo'lsa ham) — shu yerda tozalanadi.
        await Maintenance.DeleteExpiredDemosAsync(_db, now);

        var email = DemoAccount.NewEmail();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = "",
            CreatedAtUtc = now,
            EmailVerifiedAtUtc = now,
            // Taxalluslar noyob — har bir demo hisobga o'ziniki ("Demo 3fa9").
            DisplayName = "Demo " + email[5..9],
            Level = "B1",
            ShowOnLeaderboard = false,
            Goal = "ielts",
            TargetScore = "6.5",
            ExamDate = ReviewScheduler.ToLocalDate(now, tzOffsetMinutes).AddDays(45),
            DailyMinutes = 20,
            OnboardedAtUtc = now,
            SignupMethod = SignupMethods.Demo,
        };
        _db.Users.Add(user);
        var seed = DemoSeed.Build(user.Id, now, tzOffsetMinutes);
        _db.Activities.AddRange(seed.Activities);
        _db.ReviewCards.AddRange(seed.Cards);
        _db.ReviewLogs.AddRange(seed.Logs);
        var token = AddSession(user.Id, DemoAccount.Lifetime);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Demo hisob yaratildi: {UserId}", user.Id);
        return new AuthResult(token, user.Email, null);
    }

    private string AddSession(Guid userId, TimeSpan? lifetime = null)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _db.Sessions.Add(new Session
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(token),
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.Add(lifetime ?? SessionLifetime),
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
