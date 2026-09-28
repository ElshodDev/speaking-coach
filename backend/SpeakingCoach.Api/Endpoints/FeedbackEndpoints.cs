using Microsoft.EntityFrameworkCore;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;
using SpeakingCoach.Api.Services.Telegram;

namespace SpeakingCoach.Api.Endpoints;

public record FeedbackRequest(string? Kind, string? Message, string? Page);

/// <summary>Fikr-mulohaza qoidalari — sof funksiyalar (testlanadi).</summary>
public static class FeedbackRules
{
    public static readonly string[] Kinds = { "bug", "idea", "content", "ai", "other" };
    public const int MinMessage = 3;
    public const int MaxMessage = 2000;
    public const int MaxPage = 200;

    /// <summary>Xato kaliti (Texts) yoki null — hammasi joyida.</summary>
    public static string? Validate(string? kind, string? message)
    {
        if (kind is null || !Kinds.Contains(kind.Trim().ToLowerInvariant())) return "feedback.bad_kind";
        var m = (message ?? "").Trim();
        if (m.Length < MinMessage || m.Length > MaxMessage) return "feedback.bad_message";
        return null;
    }

    public static string NormalizeKind(string kind) => kind.Trim().ToLowerInvariant();

    /// <summary>Sahifa manzili — ixtiyoriy, 200 belgigacha qisqartiriladi.</summary>
    public static string? NormalizePage(string? page) => TelegramLoginRules.Clip(page, MaxPage);

    /// <summary>Adminga Telegram xabari uchun qisqa ko'rinish (xabar 500 belgigacha).</summary>
    public static string Preview(string message, int max = 500) =>
        message.Length > max ? message[..max] + "…" : message;
}

public static class FeedbackEndpoints
{
    public static void MapFeedbackEndpoints(this IEndpointRouteBuilder app)
    {
        // Mehmon ham yubora oladi (Telegram/Instagram ichidagi brauzerda kira olmagan odam ham).
        app.MapPost("/api/feedback", async (FeedbackRequest body, HttpRequest request, AuthService auth, AppDbContext db,
            AdminOptions admins, TelegramOptions telegram, ITelegramApi api, ILogger<Program> logger) =>
        {
            var error = FeedbackRules.Validate(body.Kind, body.Message);
            if (error is not null)
            {
                return Results.BadRequest(request.ErrorWithCode(error, FeedbackRules.MinMessage, FeedbackRules.MaxMessage));
            }

            var user = await auth.GetCurrentUserAsync(request);
            var feedback = new Feedback
            {
                Id = Guid.NewGuid(),
                UserId = user?.Id,
                Kind = FeedbackRules.NormalizeKind(body.Kind!),
                Message = body.Message!.Trim(),
                Page = FeedbackRules.NormalizePage(body.Page),
                Lang = Texts.LangOf(request),
                CreatedAtUtc = DateTime.UtcNow,
                Ip = TelegramLoginRules.Clip(request.HttpContext.Connection.RemoteIpAddress?.ToString(), 64),
            };
            db.Feedbacks.Add(feedback);
            await db.SaveChangesAsync();

            // Adminlarga Telegram'da qisqa xabar: Telegram:Admins ID'lari va Admin:Emails
            // hisoblari ulangan chatlar. Chatlar ro'yxati hozir (so'rov ichida) olinadi,
            // yuborish esa javobni kutdirmaydi (fire-and-forget, xatolar faqat logga).
            if (telegram.Enabled)
            {
                try
                {
                    var adminEmails = admins.Emails.ToList();
                    var adminIds = adminEmails.Count == 0
                        ? new List<Guid>()
                        : await db.Users.Where(u => adminEmails.Contains(u.Email)).Select(u => u.Id).ToListAsync();
                    var chats = adminIds.Count == 0
                        ? []
                        : await db.TelegramAccounts
                            .Where(a => adminIds.Contains(a.UserId))
                            .Select(a => new { a.ChatId, a.Lang })
                            .ToListAsync();
                    var targets = chats.Select(c => (c.ChatId, c.Lang))
                        .Concat(telegram.AdminIds.Select(id => (ChatId: id, Lang: Texts.DefaultLang)))
                        .DistinctBy(t => t.ChatId)
                        .ToList();
                    var tgName = user is null ? null : await db.TelegramAccounts.Where(a => a.UserId == user.Id).Select(a => a.Username).FirstOrDefaultAsync();
                    var from = user is null ? "guest" : AdminService.Contact(user.Email, tgName, user.DisplayName);
                    var tail = $"{from} · {feedback.Page ?? "—"} · {feedback.Lang}";
                    _ = Task.Run(async () =>
                    {
                        foreach (var (chatId, lang) in targets)
                        {
                            try
                            {
                                await api.SendMessageAsync(chatId, Texts.Get(lang, "feedback.admin_notify",
                                    BotLogic.Html(feedback.Kind), BotLogic.Html(FeedbackRules.Preview(feedback.Message)), BotLogic.Html(tail)));
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(ex, "Fikr haqida adminga yozib bo'lmadi: chat {Chat}", chatId);
                            }
                        }
                    });
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Fikr haqida adminlarni aniqlab bo'lmadi");
                }
            }

            return Results.Ok(new { ok = true, id = feedback.Id });
        }).RequireRateLimiting("write");

        // Admin: oxirgi fikrlar (eng yangisi birinchi). 401 — kirmagan; 403 — admin emas.
        app.MapGet("/api/admin/feedback", async (HttpRequest request, AuthService auth, AdminOptions admins, AppDbContext db, int take = 50) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Results.Json(request.Error("login.required"), statusCode: StatusCodes.Status401Unauthorized);
            if (!admins.IsAdmin(user)) return Results.Json(request.Error("admin.only"), statusCode: StatusCodes.Status403Forbidden);

            take = Math.Clamp(take, 1, 200);
            var rows = await db.Feedbacks
                .OrderByDescending(f => f.CreatedAtUtc)
                .Take(take)
                .ToListAsync();
            var userIds = rows.Where(f => f.UserId is not null).Select(f => f.UserId!.Value).Distinct().ToList();
            // Admin fikr yozgan odamni to'liq ko'radi: email yoki Telegram (@username / ism / ID).
            var tgNames = userIds.Count == 0
                ? new Dictionary<Guid, string?>()
                : (await db.TelegramAccounts.Where(a => userIds.Contains(a.UserId)).Select(a => new { a.UserId, a.Username }).ToListAsync())
                    .ToDictionary(a => a.UserId, a => a.Username);
            var emails = userIds.Count == 0
                ? new Dictionary<Guid, string>()
                : (await db.Users.Where(u => userIds.Contains(u.Id)).Select(u => new { u.Id, u.Email, u.DisplayName }).ToListAsync())
                    .ToDictionary(u => u.Id, u => AdminService.Contact(u.Email, tgNames.GetValueOrDefault(u.Id), u.DisplayName));
            var result = rows.Select(f => new
            {
                id = f.Id,
                kind = f.Kind,
                message = f.Message,
                page = f.Page,
                lang = f.Lang,
                createdAtUtc = f.CreatedAtUtc,
                userEmail = f.UserId is Guid id && emails.TryGetValue(id, out var e) ? e : null,
            }).ToList();
            return Results.Ok(result);
        });
    }
}
