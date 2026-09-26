namespace SpeakingCoach.Api.Data;

/// <summary>
/// O'qituvchining guruhi (sinf). Guruhni istalgan foydalanuvchi ochishi
/// mumkin — ochgan kishi shu guruhning o'qituvchisi. O'quvchilar taklif
/// kodi (JoinCode) orqali qo'shiladi.
/// </summary>
public class Group
{
    public Guid Id { get; set; }
    public Guid TeacherId { get; set; }
    public string Name { get; set; } = "";

    /// <summary>Taklif kodi: 6 belgi, chalkashadigan belgilarsiz (0/O, 1/I/L yo'q). Yangilash mumkin.</summary>
    public string JoinCode { get; set; } = "";

    public DateTime CreatedAtUtc { get; set; }
}

public class GroupMember
{
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAtUtc { get; set; }
}

/// <summary>
/// Vazifa. Bajarilishi alohida "topshirish" bilan emas, o'quvchining oddiy
/// mashqlaridan avtomatik aniqlanadi: vazifa berilgandan keyin shu turdagi
/// birinchi urinish — javob (Activities / ReviewLogs jadvallaridan).
/// </summary>
public class Assignment
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }

    /// <summary>
    /// Turi: "practice:speaking|writing|reading|listening", "review" yoki
    /// "mock:ielts|cefr:speaking|writing|listening|reading".
    /// </summary>
    public string Kind { get; set; } = "";

    /// <summary>"review" uchun — nechta karta takrorlash kerak.</summary>
    public int? Target { get; set; }

    public string Instructions { get; set; } = "";
    public DateTime? DueAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
