using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace SpeakingCoach.Application;

public record TeacherGroupRow(Guid Id, string Name, string JoinCode, DateTime CreatedAtUtc, int Members, int Assignments);
public record GroupCreated(Guid Id, string Name, string JoinCode);
public record GroupRenamed(Guid Id, string Name);
public record GroupHead(Guid Id, string Name, string JoinCode, DateTime CreatedAtUtc);
public record MemberRow(Guid UserId, string Name, DateTime JoinedAtUtc);
public record AssignmentRow(Guid Id, string Kind, int? Target, string Instructions, DateTime? DueAtUtc, DateTime CreatedAtUtc, int Done);
public record GroupDetail(GroupHead Group, List<MemberRow> Members, List<AssignmentRow> Assignments, Dictionary<string, Dictionary<string, AssignmentStatus>> Results);
public record NewAssignment(string? Kind, int? Target, string? Instructions, DateTime? DueAtUtc);
public record ActivityView(Guid Id, string Type, DateTime CreatedAtUtc, JsonElement Prompt, JsonElement Result);
public record StudentAttempt(AssignmentStatus Status, ActivityView? Activity);
public record LessonInsightsView(string Topic, LessonInsightsResult Insights);
public record JoinInfo(Guid Id, string Name, string Teacher, bool IsTeacher, bool IsMember);
public record JoinResult(Guid Id, bool Joined);
public record StudentGroupRow(Guid Id, string Name, string Teacher);
public record StudentAssignmentRow(Guid Id, Guid GroupId, string GroupName, string Kind, int? Target, string Instructions, DateTime? DueAtUtc, DateTime CreatedAtUtc, AssignmentStatus Status);
public record StudentTasks(List<StudentGroupRow> Groups, List<StudentAssignmentRow> Assignments);

/// <summary>
/// O'qituvchi bo'limi: guruhlar, a'zolar, vazifalar va natijalar (foydalanish holatlari).
/// Maxfiylik: o'qituvchi faqat o'z guruhidagi o'quvchilarning ISMINI va SHU GURUH VAZIFALARIGA
/// mos urinishlarini ko'radi (boshqa mashqlarini, emailini emas). HTTP — Api'dagi GroupEndpoints'da.
/// </summary>
public class GroupService(IAppDbContext db)
{
    private static Outcome<T> NotFound<T>() => Outcome<T>.Error("group.not_found", 404);
    private static Outcome<T> BadCode<T>() => Outcome<T>.Error("group.bad_code", 404);

    private async Task<string> UniqueCodeAsync()
    {
        for (var i = 0; i < 20; i++)
        {
            var code = GroupLogic.NewJoinCode();
            if (!await db.Groups.AnyAsync(g => g.JoinCode == code)) return code;
        }
        throw new InvalidOperationException("Taklif kodini yaratib bo'lmadi");
    }

    private Task<Group?> OwnGroupAsync(Guid id, Guid teacherId) => db.Groups.FirstOrDefaultAsync(g => g.Id == id && g.TeacherId == teacherId);

    /// <summary>Vazifa va uning guruhi o'qituvchiga tegishlimi (yo'q yoki begona bo'lsa — null).</summary>
    private async Task<Assignment?> OwnAssignmentAsync(Guid id, Guid teacherId)
    {
        var a = await db.Assignments.FirstOrDefaultAsync(x => x.Id == id);
        return a is null || await OwnGroupAsync(a.GroupId, teacherId) is null ? null : a;
    }

    // ---------------- O'qituvchi ----------------

    public async Task<List<TeacherGroupRow>> ListTeacherGroupsAsync(Guid teacherId)
    {
        var groups = await db.Groups.Where(g => g.TeacherId == teacherId).OrderBy(g => g.CreatedAtUtc).ToListAsync();
        var ids = groups.Select(g => g.Id).ToList();
        var members = await db.GroupMembers.Where(m => ids.Contains(m.GroupId)).Select(m => m.GroupId).ToListAsync();
        var assignments = await db.Assignments.Where(a => ids.Contains(a.GroupId)).Select(a => a.GroupId).ToListAsync();
        return groups.Select(g => new TeacherGroupRow(g.Id, g.Name, g.JoinCode, g.CreatedAtUtc, members.Count(m => m == g.Id), assignments.Count(a => a == g.Id))).ToList();
    }

    public async Task<Outcome<GroupCreated>> CreateGroupAsync(Guid teacherId, string? rawName)
    {
        var name = GroupLogic.CleanName(rawName);
        if (name is null) return Outcome<GroupCreated>.Error("group.name_invalid", 400);
        if (await db.Groups.CountAsync(g => g.TeacherId == teacherId) >= GroupLogic.MaxGroupsPerTeacher)
            return Outcome<GroupCreated>.Error("group.limit", 400, GroupLogic.MaxGroupsPerTeacher);
        var group = new Group { Id = Guid.NewGuid(), TeacherId = teacherId, Name = name, JoinCode = await UniqueCodeAsync(), CreatedAtUtc = DateTime.UtcNow };
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        return Outcome<GroupCreated>.Success(new GroupCreated(group.Id, group.Name, group.JoinCode));
    }

    public async Task<Outcome<GroupDetail>> GetGroupAsync(Guid id, Guid teacherId)
    {
        var group = await OwnGroupAsync(id, teacherId);
        if (group is null) return NotFound<GroupDetail>();

        var memberRows = await db.GroupMembers.Where(m => m.GroupId == id).ToListAsync();
        var memberIds = memberRows.Select(m => m.UserId).ToList();
        var people = await db.Users.Where(u => memberIds.Contains(u.Id)).Select(u => new { u.Id, u.DisplayName, u.Email }).ToListAsync();
        var members = memberRows
            .Select(m =>
            {
                var p = people.First(x => x.Id == m.UserId);
                return new MemberRow(m.UserId, GroupLogic.DisplayNameOf(p.DisplayName, p.Email), m.JoinedAtUtc);
            })
            .OrderBy(m => m.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        var assignments = await db.Assignments.Where(a => a.GroupId == id).OrderByDescending(a => a.CreatedAtUtc).ToListAsync();
        var results = await GroupQueries.ResultsAsync(db, assignments, memberIds);
        return Outcome<GroupDetail>.Success(new GroupDetail(
            new GroupHead(group.Id, group.Name, group.JoinCode, group.CreatedAtUtc),
            members,
            assignments.Select(a => new AssignmentRow(a.Id, a.Kind, a.Target, a.Instructions, a.DueAtUtc, a.CreatedAtUtc, memberIds.Count(uid => results[a.Id][uid].Done))).ToList(),
            // results[assignmentId][userId] = holat
            results.ToDictionary(r => r.Key.ToString(), r => r.Value.ToDictionary(x => x.Key.ToString(), x => x.Value))));
    }

    public async Task<Outcome<GroupRenamed>> RenameAsync(Guid id, Guid teacherId, string? rawName)
    {
        var group = await OwnGroupAsync(id, teacherId);
        if (group is null) return NotFound<GroupRenamed>();
        var name = GroupLogic.CleanName(rawName);
        if (name is null) return Outcome<GroupRenamed>.Error("group.name_invalid", 400);
        group.Name = name;
        await db.SaveChangesAsync();
        return Outcome<GroupRenamed>.Success(new GroupRenamed(group.Id, group.Name));
    }

    /// <summary>Kod tarqalib ketsa — yangisi; eski kod bilan endi qo'shilib bo'lmaydi.</summary>
    public async Task<Outcome<string>> NewCodeAsync(Guid id, Guid teacherId)
    {
        var group = await OwnGroupAsync(id, teacherId);
        if (group is null) return NotFound<string>();
        group.JoinCode = await UniqueCodeAsync();
        await db.SaveChangesAsync();
        return Outcome<string>.Success(group.JoinCode);
    }

    public async Task<Outcome<bool>> DeleteGroupAsync(Guid id, Guid teacherId)
    {
        var group = await OwnGroupAsync(id, teacherId);
        if (group is null) return NotFound<bool>();
        db.Groups.Remove(group);   // a'zolik va vazifalar — cascade
        await db.SaveChangesAsync();
        return Outcome<bool>.Success(true);
    }

    public async Task<Outcome<bool>> RemoveMemberAsync(Guid id, Guid userId, Guid teacherId)
    {
        if (await OwnGroupAsync(id, teacherId) is null) return NotFound<bool>();
        db.GroupMembers.RemoveRange(await db.GroupMembers.Where(m => m.GroupId == id && m.UserId == userId).ToListAsync());
        await db.SaveChangesAsync();
        return Outcome<bool>.Success(true);
    }

    public async Task<Outcome<Guid>> CreateAssignmentAsync(Guid groupId, Guid teacherId, NewAssignment body)
    {
        if (await OwnGroupAsync(groupId, teacherId) is null) return NotFound<Guid>();
        if (!GroupLogic.IsValidKind(body.Kind)) return Outcome<Guid>.Error("assignment.bad_kind", 400);
        int? target = null;
        if (body.Kind == "review")
        {
            if (body.Target is not (>= 1 and <= 500)) return Outcome<Guid>.Error("assignment.bad_target", 400, 1, 500);
            target = body.Target;
        }
        var now = DateTime.UtcNow;
        DateTime? due = body.DueAtUtc is DateTime d ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : null;
        if (due is DateTime dd && (dd < now.AddMinutes(-5) || dd > now.AddDays(366))) return Outcome<Guid>.Error("assignment.bad_due", 400);
        if (await db.Assignments.CountAsync(a => a.GroupId == groupId) >= GroupLogic.MaxAssignmentsPerGroup)
            return Outcome<Guid>.Error("assignment.limit", 400, GroupLogic.MaxAssignmentsPerGroup);
        var instructions = (body.Instructions ?? "").Trim();
        if (instructions.Length > 1000) instructions = instructions[..1000];
        var a = new Assignment { Id = Guid.NewGuid(), GroupId = groupId, Kind = body.Kind!, Target = target, Instructions = instructions, DueAtUtc = due, CreatedAtUtc = now };
        db.Assignments.Add(a);
        await db.SaveChangesAsync();
        return Outcome<Guid>.Success(a.Id);
    }

    public async Task<Outcome<bool>> DeleteAssignmentAsync(Guid id, Guid teacherId)
    {
        var a = await OwnAssignmentAsync(id, teacherId);
        if (a is null) return NotFound<bool>();
        db.Assignments.Remove(a);
        await db.SaveChangesAsync();
        return Outcome<bool>.Success(true);
    }

    /// <summary>O'qituvchi o'quvchining SHU vazifaga mos urinishini ko'radi (masalan, insho matni va baholash).</summary>
    public async Task<Outcome<StudentAttempt>> StudentAttemptAsync(Guid assignmentId, Guid userId, Guid teacherId)
    {
        var a = await OwnAssignmentAsync(assignmentId, teacherId);
        if (a is null) return NotFound<StudentAttempt>();
        if (!await db.GroupMembers.AnyAsync(m => m.GroupId == a.GroupId && m.UserId == userId)) return NotFound<StudentAttempt>();
        var status = (await GroupQueries.ResultsAsync(db, [a], [userId]))[a.Id][userId];
        if (status.ActivityId is not Guid activityId) return Outcome<StudentAttempt>.Success(new StudentAttempt(status, null));
        var act = await db.Activities.FirstAsync(x => x.Id == activityId);
        using var prompt = JsonDocument.Parse(act.PromptData);
        using var result = JsonDocument.Parse(act.ResponseData);
        return Outcome<StudentAttempt>.Success(new StudentAttempt(status, new ActivityView(act.Id, act.Type.ToString(), act.CreatedAtUtc, prompt.RootElement.Clone(), result.RootElement.Clone())));
    }

    /// <summary>AI dars vazifasi xulosasi: kim bajardi, mini-test o'rtachasi, o'zlashtirish va sinfdagi eng ko'p xatolar.</summary>
    public async Task<Outcome<LessonInsightsView>> InsightsAsync(Guid assignmentId, Guid teacherId)
    {
        var a = await OwnAssignmentAsync(assignmentId, teacherId);
        if (a is null) return NotFound<LessonInsightsView>();
        if (GroupLogic.LessonTopic(a.Kind) is not { } topic) return Outcome<LessonInsightsView>.Error("assignment.not_lesson", 400);
        var memberIds = await db.GroupMembers.Where(m => m.GroupId == a.GroupId).Select(m => m.UserId).ToListAsync();
        var results = (await GroupQueries.ResultsAsync(db, [a], memberIds))[a.Id];
        var hitIds = results.Values.Where(st => st.ActivityId is not null).Select(st => st.ActivityId!.Value).ToList();
        var responses = hitIds.Count == 0 ? [] : await db.Activities.Where(x => hitIds.Contains(x.Id)).Select(x => x.ResponseData).ToListAsync();
        return Outcome<LessonInsightsView>.Success(new LessonInsightsView(topic, LessonInsights.Build(memberIds.Count, responses)));
    }

    // ---------------- O'quvchi ----------------

    private async Task<Group?> GroupByCodeAsync(string code)
    {
        var c = GroupLogic.NormalizeCode(code);
        return c is null ? null : await db.Groups.FirstOrDefaultAsync(g => g.JoinCode == c);
    }

    public async Task<Outcome<JoinInfo>> JoinInfoAsync(string code, Guid userId)
    {
        var group = await GroupByCodeAsync(code);
        if (group is null) return BadCode<JoinInfo>();
        var teacher = await db.Users.Where(u => u.Id == group.TeacherId).Select(u => new { u.DisplayName, u.Email }).FirstAsync();
        return Outcome<JoinInfo>.Success(new JoinInfo(group.Id, group.Name, GroupLogic.TeacherNameOf(teacher.DisplayName), group.TeacherId == userId,
            await db.GroupMembers.AnyAsync(m => m.GroupId == group.Id && m.UserId == userId)));
    }

    public async Task<Outcome<JoinResult>> JoinAsync(string code, Guid userId)
    {
        var group = await GroupByCodeAsync(code);
        if (group is null) return BadCode<JoinResult>();
        if (group.TeacherId == userId) return Outcome<JoinResult>.Error("group.own_group", 400);
        if (await db.GroupMembers.AnyAsync(m => m.GroupId == group.Id && m.UserId == userId)) return Outcome<JoinResult>.Success(new JoinResult(group.Id, false));
        if (await db.GroupMembers.CountAsync(m => m.GroupId == group.Id) >= GroupLogic.MaxMembersPerGroup)
            return Outcome<JoinResult>.Error("group.full", 400, GroupLogic.MaxMembersPerGroup);
        if (await db.GroupMembers.CountAsync(m => m.UserId == userId) >= GroupLogic.MaxGroupsPerStudent)
            return Outcome<JoinResult>.Error("group.student_limit", 400, GroupLogic.MaxGroupsPerStudent);
        db.GroupMembers.Add(new GroupMember { GroupId = group.Id, UserId = userId, JoinedAtUtc = DateTime.UtcNow });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (db.IsUniqueViolation(ex))
        {
            // Ikki marta bosildi: parallel so'rov a'zolikni allaqachon yozdi (kalit — GroupId+UserId).
            return Outcome<JoinResult>.Success(new JoinResult(group.Id, false));
        }
        return Outcome<JoinResult>.Success(new JoinResult(group.Id, true));
    }

    public async Task<StudentTasks> StudentTasksAsync(Guid userId)
    {
        var groupIds = await db.GroupMembers.Where(m => m.UserId == userId).Select(m => m.GroupId).ToListAsync();
        var groups = await db.Groups.Where(g => groupIds.Contains(g.Id)).Select(g => new { g.Id, g.Name, g.TeacherId }).ToListAsync();
        var teacherIds = groups.Select(g => g.TeacherId).Distinct().ToList();
        var teachers = await db.Users.Where(u => teacherIds.Contains(u.Id)).Select(u => new { u.Id, u.DisplayName, u.Email }).ToListAsync();
        var assignments = await db.Assignments.Where(a => groupIds.Contains(a.GroupId)).OrderByDescending(a => a.CreatedAtUtc).Take(200).ToListAsync();
        var results = await GroupQueries.ResultsAsync(db, assignments, [userId]);
        return new StudentTasks(
            groups.Select(g => new StudentGroupRow(g.Id, g.Name, GroupLogic.TeacherNameOf(teachers.First(x => x.Id == g.TeacherId).DisplayName))).ToList(),
            assignments.Select(a => new StudentAssignmentRow(a.Id, a.GroupId, groups.First(g => g.Id == a.GroupId).Name, a.Kind, a.Target, a.Instructions, a.DueAtUtc, a.CreatedAtUtc, results[a.Id][userId])).ToList());
    }

    public async Task LeaveAsync(Guid groupId, Guid userId)
    {
        db.GroupMembers.RemoveRange(await db.GroupMembers.Where(m => m.GroupId == groupId && m.UserId == userId).ToListAsync());
        await db.SaveChangesAsync();
    }
}
