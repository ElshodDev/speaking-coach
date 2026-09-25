using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;

namespace SpeakingCoach.Api.Services;

/// <summary>Ro'yxatdan o'tish/kirish natijasi: yoki token, yoki xato matni.</summary>
public record AuthResult(string? Token, string? Email, string? Error)
{
    public static AuthResult Fail(string error) => new(null, null, error);
}

/// <summary>
/// Oddiy, tushunarli token-autentifikatsiya:
///
/// 1. Parol PasswordHasher (PBKDF2) bilan xeshlanadi — ASP.NET Core'ning
///    o'zida bor, tashqi paket kerak emas.
/// 2. Kirganda 32 baytli tasodifiy token yaratiladi (RandomNumberGenerator —
///    kriptografik tasodifiy, Random() emas). Brauzer uni har so'rovda
///    "Authorization: Bearer <token>" sarlavhasida yuboradi.
/// 3. Bazada token emas, uning SHA-256 xeshi saqlanadi.
///
/// Nega JWT emas: JWT'ni muddatidan oldin bekor qilib bo'lmaydi (chiqish =
/// faqat brauzerdan o'chirish). Bu yerda esa sessiya bazada — "Chiqish"
/// bosilganda yozuv o'chadi va token darhol yaroqsiz bo'ladi. Bundan
/// tashqari, JWT uchun qo'shimcha NuGet paket va maxfiy kalit sozlash kerak
/// bo'lardi. Narxi: har bir so'rovda bazaga bitta qo'shimcha (indekslangan)
/// so'rov.
/// </summary>
public class AuthService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromDays(30);
    private const int MinPasswordLength = 8;
    private const int MaxPasswordLength = 128;

    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _hasher;

    public AuthService(AppDbContext db, IPasswordHasher<User> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public async Task<AuthResult> RegisterAsync(string email, string password)
    {
        var normalized = NormalizeEmail(email ?? "");
        if (normalized.Length > 256 || !System.Net.Mail.MailAddress.TryCreate(normalized, out _) || !normalized.Contains('.'))
        {
            return AuthResult.Fail("Email manzili noto'g'ri");
        }
        if (password is null || password.Length < MinPasswordLength)
        {
            return AuthResult.Fail($"Parol kamida {MinPasswordLength} belgidan iborat bo'lishi kerak");
        }
        if (password.Length > MaxPasswordLength)
        {
            return AuthResult.Fail($"Parol {MaxPasswordLength} belgidan oshmasligi kerak");
        }
        if (await _db.Users.AnyAsync(u => u.Email == normalized))
        {
            return AuthResult.Fail("Bu email bilan allaqachon ro'yxatdan o'tilgan");
        }

        var user = new User { Id = Guid.NewGuid(), Email = normalized, CreatedAtUtc = DateTime.UtcNow };
        user.PasswordHash = _hasher.HashPassword(user, password);
        _db.Users.Add(user);

        var token = AddSession(user.Id);
        // Foydalanuvchi va sessiya bitta SaveChanges'da — ya'ni bitta
        // tranzaksiyada yoziladi: yo ikkalasi ham saqlanadi, yo hech biri.
        await _db.SaveChangesAsync();
        return new AuthResult(token, user.Email, null);
    }

    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        var normalized = NormalizeEmail(email ?? "");
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalized);

        // Email topilmasa ham, parol noto'g'ri bo'lsa ham — BIR XIL xabar.
        // Aks holda hujumchi "bu email ro'yxatdan o'tganmi?" degan savolga
        // javob olib, email'larni birma-bir tekshirib chiqishi mumkin edi.
        const string wrongCredentials = "Email yoki parol noto'g'ri";
        if (user is null || string.IsNullOrEmpty(password))
        {
            return AuthResult.Fail(wrongCredentials);
        }

        var check = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        if (check == PasswordVerificationResult.Failed)
        {
            return AuthResult.Fail(wrongCredentials);
        }
        if (check == PasswordVerificationResult.SuccessRehashNeeded)
        {
            // Xeshlash algoritmi kuchaytirilgan bo'lsa (masalan .NET yangi
            // versiyasida takrorlashlar soni oshgan), parolni yangi usulda
            // qayta xeshlab qo'yamiz — foydalanuvchi buni sezmaydi.
            user.PasswordHash = _hasher.HashPassword(user, password);
        }

        var token = AddSession(user.Id);
        await _db.SaveChangesAsync();
        return new AuthResult(token, user.Email, null);
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

        return await _db.Users.FirstOrDefaultAsync(u => u.Id == userId.Value);
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
