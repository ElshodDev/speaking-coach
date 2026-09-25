using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Endpoints;

public record AuthRequest(string Email, string Password);

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/register", async (AuthRequest body, AuthService auth) =>
        {
            var result = await auth.RegisterAsync(body.Email, body.Password);
            return result.Error is null
                ? Results.Ok(new { token = result.Token, email = result.Email })
                : Results.BadRequest(new { error = result.Error });
        }).RequireRateLimiting("auth");

        app.MapPost("/api/auth/login", async (AuthRequest body, AuthService auth) =>
        {
            var result = await auth.LoginAsync(body.Email, body.Password);
            // 401 — "kim ekaningizni tasdiqlab bo'lmadi". 400 (noto'g'ri
            // so'rov shakli) emas, chunki so'rov shakli to'g'ri edi.
            return result.Error is null
                ? Results.Ok(new { token = result.Token, email = result.Email })
                : Results.Json(new { error = result.Error }, statusCode: StatusCodes.Status401Unauthorized);
        }).RequireRateLimiting("auth");

        app.MapPost("/api/auth/logout", async (HttpRequest request, AuthService auth) =>
        {
            await auth.LogoutAsync(request);
            return Results.Ok();
        });

        // Sahifa ochilganda frontend saqlangan token hali yaroqlimi deb shu
        // yerdan so'raydi (muddati o'tgan yoki "Chiqish" qilingan bo'lishi mumkin).
        app.MapGet("/api/auth/me", async (HttpRequest request, AuthService auth) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            return user is null
                ? Results.Json(new { error = "Tizimga kirilmagan" }, statusCode: StatusCodes.Status401Unauthorized)
                : Results.Ok(new { email = user.Email });
        });
    }
}
