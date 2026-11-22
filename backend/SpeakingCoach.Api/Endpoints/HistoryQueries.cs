namespace SpeakingCoach.Api.Endpoints;

public static class HistoryQueries
{
    /// <summary>"Oldingi urinishlar" ro'yxati (barcha mashq turlari uchun bitta joy). Mantiq — ComprehensionPractice.RecentAsync.</summary>
    public static async Task<IResult> GetHistoryAsync(ActivityType type, HttpRequest request, ComprehensionPractice practice, AuthService auth) =>
        Results.Ok(await practice.RecentAsync(await auth.GetCurrentUserIdAsync(request), type));
}
