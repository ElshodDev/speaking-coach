using System.Text.Json;
using Microsoft.EntityFrameworkCore;
namespace SpeakingCoach.Api.Endpoints;

public record GroupNameRequest(string? Name);
public record AssignmentRequest(string? Kind, int? Target, string? Instructions, DateTime? DueAtUtc);

/// <summary>
/// O'qituvchi bo'limi: guruhlar, a'zolar, vazifalar va natijalar.
/// Maxfiylik: o'qituvchi faqat o'z guruhidagi o'quvchilarning ISMINI va
/// SHU GURUH VAZIFALARIGA mos urinishlarini ko'radi (boshqa mashqlarini,
/// emailini emas). O'quvchi qo'shilishdan oldin buni ko'radi va rozi bo'ladi.
/// </summary>
public static class GroupEndpoints
{
    public static void MapGroupEndpoints(this IEndpointRouteBuilder app)
    {
        static IResult Unauthorized(HttpRequest r) => Results.Json(r.Error("login.required"), statusCode: StatusCodes.Status401Unauthorized);
        static IResult NotFound(HttpRequest r) => Results.Json(r.Error("group.not_found"), statusCode: StatusCodes.Status404NotFound);

        async Task<string> UniqueCodeAsync(AppDbContext db)
        {
            for (var i = 0; i < 20; i++)
            {
                var code = GroupLogic.NewJoinCode();
                if (!await db.Groups.AnyAsync(g => g.JoinCode == code)) return code;
            }
            throw new InvalidOperationException("Taklif kodini yaratib bo'lmadi");
        }

        // ---------------- O'qituvchi ----------------

        app.MapGet("/api/teacher/groups", async (HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var groups = await db.Groups.Where(g => g.TeacherId == user.Id).OrderBy(g => g.CreatedAtUtc).ToListAsync();
            var ids = groups.Select(g => g.Id).ToList();
            var members = await db.GroupMembers.Where(m => ids.Contains(m.GroupId)).Select(m => m.GroupId).ToListAsync();
            var assignments = await db.Assignments.Where(a => ids.Contains(a.GroupId)).Select(a => a.GroupId).ToListAsync();
            return Results.Ok(groups.Select(g => new
            {
                g.Id, g.Name, g.JoinCode, g.CreatedAtUtc,
                members = members.Count(m => m == g.Id),
                assignments = assignments.Count(a => a == g.Id),
            }));
        });

        app.MapPost("/api/teacher/groups", async (GroupNameRequest body, HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var name = GroupLogic.CleanName(body.Name);
            if (name is null) return Results.BadRequest(request.Error("group.name_invalid"));
            if (await db.Groups.CountAsync(g => g.TeacherId == user.Id) >= GroupLogic.MaxGroupsPerTeacher)
                return Results.BadRequest(request.Error("group.limit", GroupLogic.MaxGroupsPerTeacher));
            var group = new Group { Id = Guid.NewGuid(), TeacherId = user.Id, Name = name, JoinCode = await UniqueCodeAsync(db), CreatedAtUtc = DateTime.UtcNow };
            db.Groups.Add(group);
            await db.SaveChangesAsync();
            return Results.Ok(new { group.Id, group.Name, group.JoinCode });
        }).RequireRateLimiting(RateLimits.WritePolicy);

        async Task<Group?> OwnGroupAsync(AppDbContext db, Guid id, Guid teacherId) =>
            await db.Groups.FirstOrDefaultAsync(g => g.Id == id && g.TeacherId == teacherId);

        app.MapGet("/api/teacher/groups/{id:guid}", async (Guid id, HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var group = await OwnGroupAsync(db, id, user.Id);
            if (group is null) return NotFound(request);

            var memberRows = await db.GroupMembers.Where(m => m.GroupId == id).ToListAsync();
            var memberIds = memberRows.Select(m => m.UserId).ToList();
            var people = await db.Users.Where(u => memberIds.Contains(u.Id)).Select(u => new { u.Id, u.DisplayName, u.Email }).ToListAsync();
            var members = memberRows
                .Select(m =>
                {
                    var p = people.First(x => x.Id == m.UserId);
                    return new { userId = m.UserId, name = GroupLogic.DisplayNameOf(p.DisplayName, p.Email), m.JoinedAtUtc };
                })
                .OrderBy(m => m.name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            var assignments = await db.Assignments.Where(a => a.GroupId == id).OrderByDescending(a => a.CreatedAtUtc).ToListAsync();
            var results = await GroupQueries.ResultsAsync(db, assignments, memberIds);
            return Results.Ok(new
            {
                group = new { group.Id, group.Name, group.JoinCode, group.CreatedAtUtc },
                members,
                assignments = assignments.Select(a => new
                {
                    a.Id, a.Kind, a.Target, a.Instructions, a.DueAtUtc, a.CreatedAtUtc,
                    done = memberIds.Count(uid => results[a.Id][uid].Done),
                }),
                // results[assignmentId][userId] = holat
                results = results.ToDictionary(r => r.Key.ToString(), r => r.Value.ToDictionary(x => x.Key.ToString(), x => x.Value)),
            });
        });

        app.MapPut("/api/teacher/groups/{id:guid}", async (Guid id, GroupNameRequest body, HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var group = await OwnGroupAsync(db, id, user.Id);
            if (group is null) return NotFound(request);
            var name = GroupLogic.CleanName(body.Name);
            if (name is null) return Results.BadRequest(request.Error("group.name_invalid"));
            group.Name = name;
            await db.SaveChangesAsync();
            return Results.Ok(new { group.Id, group.Name });
        }).RequireRateLimiting(RateLimits.WritePolicy);

        // Kod tarqalib ketsa — yangisi; eski kod bilan endi qo'shilib bo'lmaydi.
        app.MapPost("/api/teacher/groups/{id:guid}/code", async (Guid id, HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var group = await OwnGroupAsync(db, id, user.Id);
            if (group is null) return NotFound(request);
            group.JoinCode = await UniqueCodeAsync(db);
            await db.SaveChangesAsync();
            return Results.Ok(new { group.JoinCode });
        }).RequireRateLimiting(RateLimits.WritePolicy);

        app.MapDelete("/api/teacher/groups/{id:guid}", async (Guid id, HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var group = await OwnGroupAsync(db, id, user.Id);
            if (group is null) return NotFound(request);
            db.Groups.Remove(group);   // a'zolik va vazifalar — cascade
            await db.SaveChangesAsync();
            return Results.Ok(new { deleted = true });
        }).RequireRateLimiting(RateLimits.WritePolicy);

        app.MapDelete("/api/teacher/groups/{id:guid}/members/{userId:guid}", async (Guid id, Guid userId, HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            if (await OwnGroupAsync(db, id, user.Id) is null) return NotFound(request);
            db.GroupMembers.RemoveRange(await db.GroupMembers.Where(m => m.GroupId == id && m.UserId == userId).ToListAsync());
            await db.SaveChangesAsync();
            return Results.Ok(new { removed = true });
        }).RequireRateLimiting(RateLimits.WritePolicy);

        app.MapPost("/api/teacher/groups/{id:guid}/assignments", async (Guid id, AssignmentRequest body, HttpRequest request, AuthService auth, AppDbContext db, IServiceScopeFactory scopes) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            if (await OwnGroupAsync(db, id, user.Id) is null) return NotFound(request);
            if (!GroupLogic.IsValidKind(body.Kind)) return Results.BadRequest(request.Error("assignment.bad_kind"));
            int? target = null;
            if (body.Kind == "review")
            {
                if (body.Target is not (>= 1 and <= 500)) return Results.BadRequest(request.Error("assignment.bad_target", 1, 500));
                target = body.Target;
            }
            var now = DateTime.UtcNow;
            DateTime? due = body.DueAtUtc is DateTime d ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : null;
            if (due is DateTime dd && (dd < now.AddMinutes(-5) || dd > now.AddDays(366))) return Results.BadRequest(request.Error("assignment.bad_due"));
            if (await db.Assignments.CountAsync(a => a.GroupId == id) >= GroupLogic.MaxAssignmentsPerGroup)
                return Results.BadRequest(request.Error("assignment.limit", GroupLogic.MaxAssignmentsPerGroup));
            var instructions = (body.Instructions ?? "").Trim();
            if (instructions.Length > 1000) instructions = instructions[..1000];
            var a = new Assignment { Id = Guid.NewGuid(), GroupId = id, Kind = body.Kind!, Target = target, Instructions = instructions, DueAtUtc = due, CreatedAtUtc = now };
            db.Assignments.Add(a);
            await db.SaveChangesAsync();
            // O'quvchilarga Telegram xabari — fonda (o'qituvchi kutib qolmasin).
            _ = Task.Run(async () =>
            {
                using var scope = scopes.CreateScope();
                try
                {
                    await scope.ServiceProvider.GetRequiredService<AssignmentNotifier>().NotifyAsync(a.Id);
                }
                catch (Exception ex)
                {
                    scope.ServiceProvider.GetRequiredService<ILogger<Program>>().LogWarning(ex, "Vazifa xabarlari yuborilmadi: {Id}", a.Id);
                }
            });
            return Results.Ok(new { a.Id });
        }).RequireRateLimiting(RateLimits.WritePolicy);

        app.MapDelete("/api/teacher/assignments/{id:guid}", async (Guid id, HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var a = await db.Assignments.FirstOrDefaultAsync(x => x.Id == id);
            if (a is null || await OwnGroupAsync(db, a.GroupId, user.Id) is null) return NotFound(request);
            db.Assignments.Remove(a);
            await db.SaveChangesAsync();
            return Results.Ok(new { deleted = true });
        }).RequireRateLimiting(RateLimits.WritePolicy);

        // O'qituvchi o'quvchining SHU vazifaga mos urinishini ko'radi (masalan, insho matni va baholash).
        app.MapGet("/api/teacher/assignments/{id:guid}/students/{userId:guid}", async (Guid id, Guid userId, HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var a = await db.Assignments.FirstOrDefaultAsync(x => x.Id == id);
            if (a is null || await OwnGroupAsync(db, a.GroupId, user.Id) is null) return NotFound(request);
            if (!await db.GroupMembers.AnyAsync(m => m.GroupId == a.GroupId && m.UserId == userId)) return NotFound(request);
            var status = (await GroupQueries.ResultsAsync(db, [a], [userId]))[a.Id][userId];
            if (status.ActivityId is not Guid activityId) return Results.Ok(new { status, activity = (object?)null });
            var act = await db.Activities.FirstAsync(x => x.Id == activityId);
            using var prompt = JsonDocument.Parse(act.PromptData);
            using var result = JsonDocument.Parse(act.ResponseData);
            return Results.Ok(new
            {
                status,
                activity = new { act.Id, type = act.Type.ToString(), act.CreatedAtUtc, prompt = prompt.RootElement.Clone(), result = result.RootElement.Clone() },
            });
        });

        // AI dars vazifasi xulosasi: kim bajardi, mini-test o'rtachasi, o'zlashtirish va sinfdagi eng ko'p xatolar.
        app.MapGet("/api/teacher/assignments/{id:guid}/insights", async (Guid id, HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var a = await db.Assignments.FirstOrDefaultAsync(x => x.Id == id);
            if (a is null || await OwnGroupAsync(db, a.GroupId, user.Id) is null) return NotFound(request);
            if (GroupLogic.LessonTopic(a.Kind) is not { } topic) return Results.BadRequest(request.Error("assignment.not_lesson"));
            var memberIds = await db.GroupMembers.Where(m => m.GroupId == a.GroupId).Select(m => m.UserId).ToListAsync();
            var results = (await GroupQueries.ResultsAsync(db, [a], memberIds))[a.Id];
            var hitIds = results.Values.Where(st => st.ActivityId is not null).Select(st => st.ActivityId!.Value).ToList();
            var responses = hitIds.Count == 0 ? [] : await db.Activities.Where(x => hitIds.Contains(x.Id)).Select(x => x.ResponseData).ToListAsync();
            return Results.Ok(new { topic, insights = LessonInsights.Build(memberIds.Count, responses) });
        });

        // ---------------- O'quvchi ----------------

        app.MapGet("/api/groups/join/{code}", async (string code, HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var c = GroupLogic.NormalizeCode(code);
            var group = c is null ? null : await db.Groups.FirstOrDefaultAsync(g => g.JoinCode == c);
            if (group is null) return Results.Json(request.Error("group.bad_code"), statusCode: StatusCodes.Status404NotFound);
            var teacher = await db.Users.Where(u => u.Id == group.TeacherId).Select(u => new { u.DisplayName, u.Email }).FirstAsync();
            return Results.Ok(new
            {
                group.Id, group.Name,
                teacher = GroupLogic.TeacherNameOf(teacher.DisplayName),
                isTeacher = group.TeacherId == user.Id,
                isMember = await db.GroupMembers.AnyAsync(m => m.GroupId == group.Id && m.UserId == user.Id),
            });
        }).RequireRateLimiting(RateLimits.JoinPolicy);

        app.MapPost("/api/groups/join/{code}", async (string code, HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var c = GroupLogic.NormalizeCode(code);
            var group = c is null ? null : await db.Groups.FirstOrDefaultAsync(g => g.JoinCode == c);
            if (group is null) return Results.Json(request.Error("group.bad_code"), statusCode: StatusCodes.Status404NotFound);
            if (group.TeacherId == user.Id) return Results.BadRequest(request.Error("group.own_group"));
            if (await db.GroupMembers.AnyAsync(m => m.GroupId == group.Id && m.UserId == user.Id)) return Results.Ok(new { group.Id, joined = false });
            if (await db.GroupMembers.CountAsync(m => m.GroupId == group.Id) >= GroupLogic.MaxMembersPerGroup)
                return Results.BadRequest(request.Error("group.full", GroupLogic.MaxMembersPerGroup));
            if (await db.GroupMembers.CountAsync(m => m.UserId == user.Id) >= GroupLogic.MaxGroupsPerStudent)
                return Results.BadRequest(request.Error("group.student_limit", GroupLogic.MaxGroupsPerStudent));
            db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = user.Id, JoinedAtUtc = DateTime.UtcNow });
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (DbErrors.IsUniqueViolation(ex))
            {
                // Ikki marta bosildi: parallel so'rov a'zolikni allaqachon yozdi (kalit — GroupId+UserId).
                return Results.Ok(new { group.Id, joined = false });
            }
            return Results.Ok(new { group.Id, joined = true });
        }).RequireRateLimiting(RateLimits.JoinPolicy);

        app.MapGet("/api/student/assignments", async (HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            var groupIds = await db.GroupMembers.Where(m => m.UserId == user.Id).Select(m => m.GroupId).ToListAsync();
            var groups = await db.Groups.Where(g => groupIds.Contains(g.Id)).Select(g => new { g.Id, g.Name, g.TeacherId }).ToListAsync();
            var teacherIds = groups.Select(g => g.TeacherId).Distinct().ToList();
            var teachers = await db.Users.Where(u => teacherIds.Contains(u.Id)).Select(u => new { u.Id, u.DisplayName, u.Email }).ToListAsync();
            var assignments = await db.Assignments.Where(a => groupIds.Contains(a.GroupId)).OrderByDescending(a => a.CreatedAtUtc).Take(200).ToListAsync();
            var results = await GroupQueries.ResultsAsync(db, assignments, [user.Id]);
            return Results.Ok(new
            {
                groups = groups.Select(g =>
                {
                    var t = teachers.First(x => x.Id == g.TeacherId);
                    return new { g.Id, g.Name, teacher = GroupLogic.TeacherNameOf(t.DisplayName) };
                }),
                assignments = assignments.Select(a => new
                {
                    a.Id, a.GroupId, groupName = groups.First(g => g.Id == a.GroupId).Name,
                    a.Kind, a.Target, a.Instructions, a.DueAtUtc, a.CreatedAtUtc,
                    status = results[a.Id][user.Id],
                }),
            });
        });

        app.MapDelete("/api/student/groups/{id:guid}", async (Guid id, HttpRequest request, AuthService auth, AppDbContext db) =>
        {
            var user = await auth.GetCurrentUserAsync(request);
            if (user is null) return Unauthorized(request);
            db.GroupMembers.RemoveRange(await db.GroupMembers.Where(m => m.GroupId == id && m.UserId == user.Id).ToListAsync());
            await db.SaveChangesAsync();
            return Results.Ok(new { left = true });
        }).RequireRateLimiting(RateLimits.WritePolicy);
    }
}
