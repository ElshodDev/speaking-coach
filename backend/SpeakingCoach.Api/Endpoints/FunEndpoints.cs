using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Endpoints;

/// <summary>O'yin elementlari: kunlik so'z va tezkor viktorina (AI'siz, mehmonlar uchun ham).</summary>
public static class FunEndpoints
{
    public static void MapFunEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/word-of-day", (HttpRequest request, int tzOffsetMinutes = 0) =>
        {
            var today = ReviewScheduler.ToLocalDate(DateTime.UtcNow, Math.Clamp(tzOffsetMinutes, -840, 840));
            var w = DailyWords.ForDate(today);
            var lang = Texts.LangOf(request);
            return Results.Ok(new
            {
                date = today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                word = w.Word,
                partOfSpeech = w.Pos,
                level = w.Level,
                definitionEn = w.DefinitionEn,
                example = w.Example,
                translation = w.Translation(lang),
            });
        });

        // Lug'atdagi so'zlar (kirgan bo'lsa) + kunlik so'zlar — 10 ta savol.
        app.MapGet("/api/quiz", async (HttpRequest request, AuthService auth, ReviewService reviews, int count = QuizLogic.DefaultCount) =>
        {
            var lang = Texts.LangOf(request);
            var userId = await auth.GetCurrentUserIdAsync(request);
            var vocab = userId is Guid id
                ? (await reviews.GetVocabAsync(id, 500)).Where(c => c.Kind == ReviewCardKind.Word).Select(c => new QuizPair(c.Front, c.Back)).ToList()
                : [];
            var pool = QuizLogic.Pool(vocab, lang);
            var questions = QuizLogic.Build(pool, Math.Clamp(count, 4, 20), Random.Shared);
            var ownWords = QuizLogic.Clean(vocab).Count;
            return Results.Ok(new { questions, ownWords, fromVocab = ownWords >= QuizLogic.MinPool });
        });
    }
}
