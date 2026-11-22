namespace SpeakingCoach.Api.Endpoints;

public record GroupNameRequest(string? Name);
public record AssignmentRequest(string? Kind, int? Target, string? Instructions, DateTime? DueAtUtc);

/// <summary>
/// O'qituvchi bo'limi: guruhlar, a'zolar, vazifalar va natijalar — faqat HTTP qatlami.
/// Barcha mantiq va maxfiylik qoidalari Application'dagi <see cref="GroupService"/>'da.
/// </summary>
public static class GroupEndpoints
{
    public static void MapGroupEndpoints(this IEndpointRouteBuilder app)
    {
        static IResult Unauthorized(HttpRequest r) => Results.Json(r.Error("login.required"), statusCode: StatusCodes.Status401Unauthorized);
        static IResult Failed(HttpRequest r, Fail f) => Results.Json(r.Error(f.Key, f.Args), statusCode: f.Status);
        static IResult Done<T>(HttpRequest r, Outcome<T> o, Func<T, object> ok) => o.Ok ? Results.Ok(ok(o.Value!)) : Failed(r, o.Failure!);

        // ---------------- O'qituvchi ----------------

        app.MapGet("/api/teacher/groups", async (HttpRequest request, AuthService auth, GroupService groups) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            return user is null ? Unauthorized(request) : Results.Ok(await groups.ListTeacherGroupsAsync(user.Id));
        });

        app.MapPost("/api/teacher/groups", async (GroupNameRequest body, HttpRequest request, AuthService auth, GroupService groups) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            return user is null ? Unauthorized(request) : Done(request, await groups.CreateGroupAsync(user.Id, body.Name), g => g);
        }).RequireRateLimiting(RateLimits.WritePolicy);

        app.MapGet("/api/teacher/groups/{id:guid}", async (Guid id, HttpRequest request, AuthService auth, GroupService groups) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            return user is null ? Unauthorized(request) : Done(request, await groups.GetGroupAsync(id, user.Id), g => g);
        });

        app.MapPut("/api/teacher/groups/{id:guid}", async (Guid id, GroupNameRequest body, HttpRequest request, AuthService auth, GroupService groups) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            return user is null ? Unauthorized(request) : Done(request, await groups.RenameAsync(id, user.Id, body.Name), g => g);
        }).RequireRateLimiting(RateLimits.WritePolicy);

        // Kod tarqalib ketsa — yangisi; eski kod bilan endi qo'shilib bo'lmaydi.
        app.MapPost("/api/teacher/groups/{id:guid}/code", async (Guid id, HttpRequest request, AuthService auth, GroupService groups) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            return user is null ? Unauthorized(request) : Done(request, await groups.NewCodeAsync(id, user.Id), code => new { joinCode = code });
        }).RequireRateLimiting(RateLimits.WritePolicy);

        app.MapDelete("/api/teacher/groups/{id:guid}", async (Guid id, HttpRequest request, AuthService auth, GroupService groups) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            return user is null ? Unauthorized(request) : Done(request, await groups.DeleteGroupAsync(id, user.Id), _ => new { deleted = true });
        }).RequireRateLimiting(RateLimits.WritePolicy);

        app.MapDelete("/api/teacher/groups/{id:guid}/members/{userId:guid}", async (Guid id, Guid userId, HttpRequest request, AuthService auth, GroupService groups) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            return user is null ? Unauthorized(request) : Done(request, await groups.RemoveMemberAsync(id, userId, user.Id), _ => new { removed = true });
        }).RequireRateLimiting(RateLimits.WritePolicy);

        app.MapPost("/api/teacher/groups/{id:guid}/assignments", async (Guid id, AssignmentRequest body, HttpRequest request, AuthService auth, GroupService groups, IServiceScopeFactory scopes) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var created = await groups.CreateAssignmentAsync(id, user.Id, new NewAssignment(body.Kind, body.Target, body.Instructions, body.DueAtUtc));
            if (!created.Ok) return Failed(request, created.Failure!);
            var assignmentId = created.Value;
            // O'quvchilarga Telegram xabari — fonda (o'qituvchi kutib qolmasin).
            _ = Task.Run(async () =>
            {
                using var scope = scopes.CreateScope();
                try
                {
                    await scope.ServiceProvider.GetRequiredService<AssignmentNotifier>().NotifyAsync(assignmentId);
                }
                catch (Exception ex)
                {
                    scope.ServiceProvider.GetRequiredService<ILogger<Program>>().LogWarning(ex, "Vazifa xabarlari yuborilmadi: {Id}", assignmentId);
                }
            });
            return Results.Ok(new { Id = assignmentId });
        }).RequireRateLimiting(RateLimits.WritePolicy);

        app.MapDelete("/api/teacher/assignments/{id:guid}", async (Guid id, HttpRequest request, AuthService auth, GroupService groups) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            return user is null ? Unauthorized(request) : Done(request, await groups.DeleteAssignmentAsync(id, user.Id), _ => new { deleted = true });
        }).RequireRateLimiting(RateLimits.WritePolicy);

        // O'qituvchi o'quvchining SHU vazifaga mos urinishini ko'radi (masalan, insho matni va baholash).
        app.MapGet("/api/teacher/assignments/{id:guid}/students/{userId:guid}", async (Guid id, Guid userId, HttpRequest request, AuthService auth, GroupService groups) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            return user is null ? Unauthorized(request) : Done(request, await groups.StudentAttemptAsync(id, userId, user.Id), r => r);
        });

        // AI dars vazifasi xulosasi: kim bajardi, mini-test o'rtachasi, o'zlashtirish va sinfdagi eng ko'p xatolar.
        app.MapGet("/api/teacher/assignments/{id:guid}/insights", async (Guid id, HttpRequest request, AuthService auth, GroupService groups) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            return user is null ? Unauthorized(request) : Done(request, await groups.InsightsAsync(id, user.Id), r => r);
        });

        // ---------------- O'quvchi ----------------

        app.MapGet("/api/groups/join/{code}", async (string code, HttpRequest request, AuthService auth, GroupService groups) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            return user is null ? Unauthorized(request) : Done(request, await groups.JoinInfoAsync(code, user.Id), r => r);
        }).RequireRateLimiting(RateLimits.JoinPolicy);

        app.MapPost("/api/groups/join/{code}", async (string code, HttpRequest request, AuthService auth, GroupService groups) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            return user is null ? Unauthorized(request) : Done(request, await groups.JoinAsync(code, user.Id), r => r);
        }).RequireRateLimiting(RateLimits.JoinPolicy);

        app.MapGet("/api/student/assignments", async (HttpRequest request, AuthService auth, GroupService groups) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            return user is null ? Unauthorized(request) : Results.Ok(await groups.StudentTasksAsync(user.Id));
        });

        app.MapDelete("/api/student/groups/{id:guid}", async (Guid id, HttpRequest request, AuthService auth, GroupService groups) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            await groups.LeaveAsync(id, user.Id);
            return Results.Ok(new { left = true });
        }).RequireRateLimiting(RateLimits.WritePolicy);
    }
}
