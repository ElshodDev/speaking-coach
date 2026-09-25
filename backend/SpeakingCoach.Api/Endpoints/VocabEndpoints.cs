using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Endpoints;

public record VocabAddRequest(
    string Word,
    string Translation,
    string? PartOfSpeech = null,
    string? DefinitionEn = null,
    string? Example = null,
    string? Source = null);

public static class VocabEndpoints
{
    public const int MaxWords = 2000;

    public static void MapVocabEndpoints(this IEndpointRouteBuilder app)
    {
        static IResult Unauthorized(HttpRequest request) =>
            Results.Json(request.Error("login.vocab"), statusCode: StatusCodes.Status401Unauthorized);

        // Lug'at ro'yxati va holat bo'yicha sonlar. Qidiruv va saralash
        // brauzerda: bitta foydalanuvchida bir necha yuz so'z — kichik ro'yxat.
        app.MapGet("/api/vocab", async (HttpRequest request, AuthService auth, ReviewService reviews) =>
        {
            var userId = await auth.GetCurrentUserIdAsync(request);
            if (userId is null) return Unauthorized(request);

            var now = DateTime.UtcNow;
            var cards = await reviews.GetVocabAsync(userId.Value, MaxWords);
            var words = cards.Select(c => new
            {
                id = c.Id,
                word = c.Front,
                meaning = c.Back,
                note = c.Note,
                kind = c.Kind.ToString(),
                source = c.Source?.ToString(),
                status = Vocabulary.StatusOf(c.LastReviewedAtUtc, c.IntervalDays).ToString(),
                due = c.DueAtUtc <= now,
                createdAtUtc = c.CreatedAtUtc,
                lapses = c.Lapses,
            }).ToList();

            return Results.Ok(new
            {
                total = words.Count,
                @new = words.Count(w => w.status == nameof(VocabStatus.New)),
                learning = words.Count(w => w.status == nameof(VocabStatus.Learning)),
                known = words.Count(w => w.status == nameof(VocabStatus.Known)),
                due = words.Count(w => w.due),
                words,
            });
        });

        // So'z izohidan (WordSheet) lug'atga qo'shish. So'z darhol
        // takrorlash navbatiga ham tushadi.
        app.MapPost("/api/vocab", async (VocabAddRequest body, HttpRequest request, AuthService auth, ReviewService reviews, AppDbContext db) =>
        {
            var userId = await auth.GetCurrentUserIdAsync(request);
            if (userId is null) return Unauthorized(request);

            var word = ReviewCardFactory.Clean(body.Word, 80);
            var meaning = ReviewCardFactory.Clean(body.Translation, ReviewCardFactory.MaxBackLength);
            if (word.Length == 0 || meaning.Length == 0)
            {
                return Results.BadRequest(request.Error("vocab.word_required"));
            }

            var note = Vocabulary.ComposeNote(body.PartOfSpeech, body.DefinitionEn, body.Example);
            var added = await reviews.AddCardsAsync(userId.Value, Vocabulary.ParseSource(body.Source), new[]
            {
                new NewCard(ReviewCardKind.Word, word, meaning, note),
            });
            if (added == 0)
            {
                return Results.Conflict(request.Error("vocab.exists"));
            }
            await db.SaveChangesAsync();
            return Results.Ok(new { added });
        });
    }
}
