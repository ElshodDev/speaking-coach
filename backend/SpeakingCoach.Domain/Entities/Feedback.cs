namespace SpeakingCoach.Domain.Entities;

public static class FeedbackKinds
{
    /// <summary>Bir bosishli baholar va ichki hodisalar (adminning fikrlar ro'yxatiga kirmaydi).</summary>
    public const string Rating = "rating";
}

/// <summary>Foydalanuvchi fikri: xato, taklif, kontent yoki AI haqida (mehmon ham yubora oladi).</summary>
public class Feedback
{
    public Guid Id { get; set; }

    /// <summary>Kirgan foydalanuvchi (mehmon — null). Hisob o'chirilsa, uning fikrlari ham o'chadi (maxfiylik siyosati bo'yicha).</summary>
    public Guid? UserId { get; set; }

    /// <summary>bug | idea | content | ai | other.</summary>
    public string Kind { get; set; } = "";

    public string Message { get; set; } = "";

    /// <summary>Qaysi sahifadan yuborilgan (masalan "#/mock").</summary>
    public string? Page { get; set; }

    public string? Lang { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? Ip { get; set; }
}
