using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Endpoints;

public static class HistoryQueries
{
    /// <summary>
    /// Barcha to'rtta mashq turining "Oldingi urinishlar" ro'yxati shu bitta
    /// funksiyadan o'tadi — bitta Activities jadvali bo'lgani uchun farq faqat
    /// Type'da. Tizimga kirmagan foydalanuvchi uchun bo'sh ro'yxat: login
    /// qo'shilgandan keyin hech kim boshqalarning tarixini ko'rmaydi.
    /// </summary>
    public static async Task<IResult> GetHistoryAsync(
        ActivityType type, HttpRequest request, AppDbContext db, AuthService auth)
    {
        var userId = await auth.GetCurrentUserIdAsync(request);
        if (userId is null)
        {
            return Results.Ok(Array.Empty<object>());
        }

        var items = await db.Activities
            .Where(a => a.UserId == userId && a.Type == type)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(20)
            .Select(a => new
            {
                a.Id,
                a.CreatedAtUtc,
                a.PromptData,
                a.ResponseData,
            })
            .ToListAsync();

        return Results.Ok(items);
    }
}
