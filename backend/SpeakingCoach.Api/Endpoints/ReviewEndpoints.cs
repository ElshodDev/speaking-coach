using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Endpoints;

public record GradeRequest(int Grade);
public record ManualCardRequest(string Front, string Back, string? Note);

public static class ReviewEndpoints
{
    public static void MapReviewEndpoints(this IEndpointRouteBuilder app)
    {
        // Barcha takrorlash endpoint'lari faqat kirgan foydalanuvchi uchun —
        // kartalar shaxsiy. Umumiy tekshiruvni bitta joyga chiqaramiz.
        static IResult Unauthorized() =>
            Results.Json(new { error = "Takrorlash uchun tizimga kiring" }, statusCode: StatusCodes.Status401Unauthorized);

        app.MapGet("/api/review/due", async (HttpRequest request, AuthService auth, ReviewService reviews, int limit = 20) =>
        {
            var userId = await auth.GetCurrentUserIdAsync(request);
            if (userId is null) return Unauthorized();
            var cards = await reviews.GetDueAsync(userId.Value, Math.Clamp(limit, 1, 50));
            return Results.Ok(cards.Select(ToDto));
        });

        // "Yo'lda" rejimi uchun: navbatda karta bo'lmasa ham, oxirgi
        // kartalarni tinglab takrorlash mumkin bo'lsin.
        app.MapGet("/api/review/cards", async (HttpRequest request, AuthService auth, ReviewService reviews, int limit = 30) =>
        {
            var userId = await auth.GetCurrentUserIdAsync(request);
            if (userId is null) return Unauthorized();
            var cards = await reviews.GetRecentAsync(userId.Value, Math.Clamp(limit, 1, 100));
            return Results.Ok(cards.Select(ToDto));
        });

        app.MapPost("/api/review/cards/{id:guid}/grade", async (Guid id, GradeRequest body, HttpRequest request, AuthService auth, ReviewService reviews) =>
        {
            var userId = await auth.GetCurrentUserIdAsync(request);
            if (userId is null) return Unauthorized();
            if (!Enum.IsDefined(typeof(ReviewGrade), body.Grade))
            {
                return Results.BadRequest(new { error = "grade 0 (Yana), 1 (Qiyin), 2 (Yaxshi) yoki 3 (Oson) bo'lishi kerak" });
            }

            var card = await reviews.GradeAsync(userId.Value, id, (ReviewGrade)body.Grade);
            return card is null
                ? Results.NotFound(new { error = "Karta topilmadi" })
                : Results.Ok(new { card.Id, card.DueAtUtc, card.IntervalDays });
        });

        app.MapPost("/api/review/cards", async (ManualCardRequest body, HttpRequest request, AuthService auth, ReviewService reviews, AppDbContext db) =>
        {
            var userId = await auth.GetCurrentUserIdAsync(request);
            if (userId is null) return Unauthorized();

            var front = ReviewCardFactory.Clean(body.Front, ReviewCardFactory.MaxFrontLength);
            var back = ReviewCardFactory.Clean(body.Back, ReviewCardFactory.MaxBackLength);
            if (front.Length == 0 || back.Length == 0)
            {
                return Results.BadRequest(new { error = "Ikkala tomon ham to'ldirilishi kerak" });
            }

            var added = await reviews.AddCardsAsync(userId.Value, null, new[]
            {
                new NewCard(ReviewCardKind.Manual, front, back, ReviewCardFactory.Clean(body.Note, ReviewCardFactory.MaxNoteLength)),
            });
            if (added == 0)
            {
                return Results.BadRequest(new { error = "Bu ibora sizda allaqachon bor" });
            }
            await db.SaveChangesAsync();
            return Results.Ok(new { added });
        });

        app.MapDelete("/api/review/cards/{id:guid}", async (Guid id, HttpRequest request, AuthService auth, ReviewService reviews) =>
        {
            var userId = await auth.GetCurrentUserIdAsync(request);
            if (userId is null) return Unauthorized();
            return await reviews.DeleteAsync(userId.Value, id)
                ? Results.Ok()
                : Results.NotFound(new { error = "Karta topilmadi" });
        });

        app.MapGet("/api/review/stats", async (HttpRequest request, AuthService auth, ReviewService reviews, int tzOffsetMinutes = 0) =>
        {
            var userId = await auth.GetCurrentUserIdAsync(request);
            if (userId is null) return Unauthorized();
            // Haqiqiy vaqt zonalari −14 soatdan +14 soatgacha.
            var offset = Math.Clamp(tzOffsetMinutes, -14 * 60, 14 * 60);
            return Results.Ok(await reviews.GetStatsAsync(userId.Value, offset));
        });
    }

    private static object ToDto(ReviewCard c) => new
    {
        id = c.Id,
        kind = c.Kind.ToString(),
        source = c.Source?.ToString(),
        front = c.Front,
        back = c.Back,
        note = c.Note,
        repetitions = c.Repetitions,
        lapses = c.Lapses,
    };
}
