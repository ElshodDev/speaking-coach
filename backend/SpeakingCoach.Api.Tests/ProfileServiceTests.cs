using Microsoft.EntityFrameworkCore;
namespace SpeakingCoach.Api.Tests;

/// <summary>ProfileService: tanishtiruv va profil sozlamalari (xotiradagi baza bilan).</summary>
public class ProfileServiceTests
{
    private static (ProfileService Svc, AppDbContext Db) Make()
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        return (new ProfileService(db), db);
    }

    private static async Task<User> AddUser(AppDbContext db, string email = "a@site.uz", string? name = null)
    {
        var u = new User { Id = Guid.NewGuid(), Email = email, DisplayName = name, Level = "B1" };
        db.Users.Add(u);
        await db.SaveChangesAsync();
        return u;
    }

    private static OnboardingInput Onb(string goal = "ielts", string? level = "B2", string? target = "6.5", string? date = null, int? minutes = 20, bool skip = false) =>
        new(skip, goal, level, target, date, minutes, 0);

    // ---- Tanishtiruv ----

    [Fact]
    public async Task Onboarding_saves_the_goal_and_marks_the_user_onboarded()
    {
        var (svc, db) = Make();
        var u = await AddUser(db);
        var soon = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)).ToString("yyyy-MM-dd");
        var r = await svc.SaveOnboardingAsync(u, Onb(target: " 6.5 ", date: soon));
        Assert.True(r.Ok);
        Assert.Equal(("ielts", "B2", "6.5", true), (r.Value!.Goal, r.Value.Level, r.Value.TargetScore, r.Value.Onboarded));
        var saved = await db.Users.SingleAsync();
        Assert.Equal(20, saved.DailyMinutes);
        Assert.NotNull(saved.OnboardedAtUtc);
        Assert.Equal(soon, saved.ExamDate!.Value.ToString("yyyy-MM-dd"));
    }

    [Theory]
    [InlineData("nonsense", "B2", "6.5", null, 20, "goal.bad_goal")]
    [InlineData("ielts", "Z9", "6.5", null, 20, "profile.bad_level")]
    [InlineData("ielts", "B2", "9.9", null, 20, "goal.bad_target")]
    [InlineData("ielts", "B2", "6.5", "2001-01-01", 20, "goal.bad_date")]       // o'tib ketgan sana
    [InlineData("ielts", "B2", "6.5", "not-a-date", 20, "goal.bad_date")]
    [InlineData("ielts", "B2", "6.5", null, 7, "goal.bad_minutes")]
    public async Task Onboarding_rejects_bad_input_and_changes_nothing(string goal, string level, string target, string? date, int minutes, string key)
    {
        var (svc, db) = Make();
        var u = await AddUser(db);
        var r = await svc.SaveOnboardingAsync(u, Onb(goal, level, target, date, minutes));
        Assert.False(r.Ok);
        Assert.Equal(key, r.Failure!.Key);
        Assert.Equal(400, r.Failure.Status);
        Assert.Null((await db.Users.SingleAsync()).OnboardedAtUtc);
    }

    [Fact]
    public async Task Onboarding_for_general_english_has_no_exam_date_and_skip_only_marks_it_done()
    {
        var (svc, db) = Make();
        var u = await AddUser(db);
        var soon = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)).ToString("yyyy-MM-dd");
        Assert.Equal("goal.bad_date", (await svc.SaveOnboardingAsync(u, Onb("general", "B1", null, soon))).Failure!.Key);

        var skipped = await svc.SaveOnboardingAsync(u, Onb(skip: true, goal: "nonsense"));       // Skip: maydonlar tekshirilmaydi
        Assert.True(skipped.Ok);
        var saved = await db.Users.SingleAsync();
        Assert.Null(saved.Goal);
        Assert.NotNull(saved.OnboardedAtUtc);
    }

    // ---- Profil ----

    [Fact]
    public async Task Profile_update_saves_nickname_level_and_leaderboard_flag()
    {
        var (svc, db) = Make();
        var u = await AddUser(db);
        var r = await svc.UpdateAsync(u, "  Elshod_1  ", "c1", true);
        Assert.True(r.Ok);
        Assert.Equal(("Elshod_1", "C1", true), (r.Value!.DisplayName, r.Value.Level, r.Value.ShowOnLeaderboard));
        Assert.Equal("Elshod_1", (await db.Users.SingleAsync()).DisplayName);
    }

    [Theory]
    [InlineData("a", "bad_nickname")]                        // juda qisqa
    [InlineData("name<script>", "bad_nickname")]             // taqiqlangan belgilar
    public async Task Profile_update_rejects_bad_nicknames(string name, string keyEnd)
    {
        var (svc, db) = Make();
        var u = await AddUser(db);
        Assert.Equal("profile." + keyEnd, (await svc.UpdateAsync(u, name, null, false)).Failure!.Key);
    }

    [Fact]
    public async Task Profile_update_checks_level_leaderboard_rules_and_unique_nickname_case_insensitively()
    {
        var (svc, db) = Make();
        var taken = await AddUser(db, "x@site.uz", "Ali");
        var me = await AddUser(db, "me@site.uz");
        var demo = await AddUser(db, "d1@" + DemoAccount.Domain);

        Assert.Equal("profile.bad_level", (await svc.UpdateAsync(me, "Valid", "Z9", false)).Failure!.Key);
        Assert.Equal("profile.nickname_required", (await svc.UpdateAsync(me, null, null, true)).Failure!.Key);   // reytingga chiqish uchun taxallus kerak
        Assert.Equal("demo.not_allowed", (await svc.UpdateAsync(demo, "Demo1", null, true)).Failure!.Key);
        Assert.Equal("profile.nickname_taken", (await svc.UpdateAsync(me, "ALI", null, false)).Failure!.Key);
        Assert.True((await svc.UpdateAsync(taken, "ali", null, false)).Ok);                                     // o'z taxallusini o'zgartirish mumkin
        Assert.Null((await db.Users.SingleAsync(x => x.Id == me.Id)).DisplayName);
    }

    [Fact]
    public async Task Clearing_the_nickname_is_allowed_when_not_on_the_leaderboard()
    {
        var (svc, db) = Make();
        var u = await AddUser(db, name: "Ali");
        var r = await svc.UpdateAsync(u, "   ", null, false);
        Assert.True(r.Ok);
        Assert.Null(r.Value!.DisplayName);
    }
}
