namespace SpeakingCoach.Api.Services.Telegram;

/// <summary>
/// /guide — saytning bo'limlari qisqa izoh bilan va har biriga tugma (Mini App
/// bo'lsa — Telegram ichida ochiladi). Toza funksiyalar — testlanadi.
/// </summary>
public static class BotGuide
{
    /// <summary>Bo'lim: matn kaliti (Texts: "bot.sec_{Key}") va saytdagi marshrut.</summary>
    public record Section(string Key, string Route);

    public static readonly Section[] Sections =
    [
        new("today", ""),
        new("practice", "practice"),
        new("shadowing", "shadowing"),
        new("mock", "mock"),
        new("review", "review"),
        new("vocab", "vocab"),
        new("quiz", "quiz"),
        new("progress", "progress"),
        new("teacher", "teacher"),
    ];

    public static string Message(string lang) => Texts.Get(lang, "bot.guide");

    /// <summary>Ikki ustunli tugmalar + pastda "Ilovani ochish".</summary>
    public static IReadOnlyList<IReadOnlyList<TgButton>> Buttons(string lang, TelegramOptions options) =>
    [
        .. Sections.Chunk(2).Select(row => (IReadOnlyList<TgButton>)row.Select(s => options.SiteButton(Texts.Get(lang, $"bot.sec_{s.Key}"), s.Route)).ToArray()),
        [options.SiteButton(Texts.Get(lang, "bot.open_app"), "")],
    ];
}
