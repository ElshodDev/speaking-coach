using System.Text.Json;
namespace SpeakingCoach.Application.Telegram;

/// <summary>
/// Shaxsiy podcast botda (/podcast): holat ("pc:link" — havola kutilmoqda) va xabar matnlari — toza funksiyalar (testlanadi).
/// Dars tayyorlash — mavjud YouTube shadowing generatori (IMockAuthoring); natija faqat foydalanuvchining o'ziga ko'rinadi.
/// </summary>
public static class BotPodcast
{
    public const string AwaitLink = "pc:link";

    public static bool IsAwaitingLink(string? state) => state == AwaitLink;

    /// <summary>Botdagi har qanday "matn kutilmoqda" holati (AI ustoz yoki podcast) — /cancel shuni tozalaydi.</summary>
    public static bool IsFlowState(string? state) =>
        IsAwaitingLink(state) || BotTutor.ParseState(state) is not null;

    /// <summary>Tayyor darsdan: (sarlavha, gaplar soni, taxminiy soniya); buzuq yozuv — null.</summary>
    public static (string Title, int Lines, int Seconds)? Describe(string? title, string payload)
    {
        try
        {
            var l = JsonSerializer.Deserialize<ShadowingLesson>(payload, MockSets.Web);
            return l is null ? null : (string.IsNullOrWhiteSpace(title) ? l.Title : title, l.Lines.Count, ShadowingRules.Seconds(l));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string IntroText(string lang, int perDay, int maxStored, bool hasLessons) =>
        Texts.Get(lang, "pc.intro", perDay, maxStored) + (hasLessons ? "\n\n" + Texts.Get(lang, "pc.mine_head") : "");

    public static string ReadyText(string lang, string title, int lines, int seconds) =>
        Texts.Get(lang, "pc.ready", BotLogic.Html(title), lines, seconds);

    public static string PracticeRoute(Guid lessonId) => $"shadowing/{MockSets.IdOf(lessonId)}";
}
