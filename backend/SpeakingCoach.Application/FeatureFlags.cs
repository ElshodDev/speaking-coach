namespace SpeakingCoach.Application;

/// <summary>
/// Xatti-harakatni o'zgartiradigan yangiliklar uchun o'chirgichlar (feature flag). Standart — YOQIQ;
/// o'chirish: Render'da muhit o'zgaruvchisini "false" qiling va qayta ishga tushiring.
/// </summary>
public class FeatureFlags(IConfiguration config)
{
    /// <summary>Gapirish natijasi: 4 rasmiy mezon (taxminiy band, talaffuz) va bitta asosiy tuzatish. Env: Features__SpeakingFourCriteria.</summary>
    public bool SpeakingFourCriteria => Read("Features:SpeakingFourCriteria");

    /// <summary>Birinchi kunlik diagnostik test va boshlang'ich tayyorlik bahosi. Env: Features__DayOneDiagnostic.</summary>
    public bool DayOneDiagnostic => Read("Features:DayOneDiagnostic");

    /// <summary>Taklif (referral): taklif havolasi, mukofot (+kunlik AI urinishlar). Env: Features__Referral.</summary>
    public bool Referral => Read("Features:Referral");

    /// <summary>AI ustoz: mavzu yozib o'rganish (mini-dars), veb va Telegram. Env: Features__AiTutor.</summary>
    public bool AiTutor => Read("Features:AiTutor");

    private bool Read(string key) => !bool.TryParse(config[key], out var on) || on;
}
