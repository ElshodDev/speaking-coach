namespace SpeakingCoach.Api.Data;

/// <summary>
/// Ro'yxatdan o'tgan foydalanuvchi. Parolning o'zi HECH QACHON saqlanmaydi —
/// faqat PasswordHash (ASP.NET Core'ning PasswordHasher'i: PBKDF2 + tuz
/// (salt) + ko'p marta takrorlash). Baza sizib chiqsa ham, xeshdan asl
/// parolni tiklash amalda juda qimmat.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>Doim kichik harflarda va bo'sh joylarsiz saqlanadi (Ali@Mail.com == ali@mail.com).</summary>
    public string Email { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Kirish sessiyasi. Foydalanuvchi kirganda tasodifiy token yaratiladi va
/// brauzerga beriladi; bazada esa tokenning O'ZI emas, SHA-256 XESHI
/// saqlanadi. Sabab xuddi parolnikidek: baza sizib chiqsa ham, undagi
/// xeshlar bilan hech kimning nomidan kirib bo'lmaydi.
/// </summary>
public class Session
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
