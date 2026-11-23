using System.Net;
using System.Text.Json;
namespace SpeakingCoach.Api.Tests.Integration;

/// <summary>O'qituvchi bo'limi HTTP orqali: marshrutlar, status kodlari va JSON shakllari (frontend aynan shularga tayanadi).</summary>
public class GroupHttpTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<JsonElement> Ok(HttpResponseMessage r)
    {
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        return await r.JsonAsync();
    }

    [Fact]
    public async Task Teacher_endpoints_need_a_login()
    {
        var guest = factory.CreateClient();
        foreach (var path in new[] { "/api/teacher/groups", "/api/student/assignments", $"/api/teacher/groups/{Guid.NewGuid()}" })
        {
            var res = await guest.GetAsync(path);
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
            Assert.Equal(Msg.Err("login.required"), (await res.JsonAsync()).GetProperty("error").GetString());
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await guest.PostAsync("/api/teacher/groups", JsonExtensions.Json(new { name = "X group" }))).StatusCode);
    }

    [Fact]
    public async Task A_group_goes_through_its_whole_life_with_stable_json_shapes()
    {
        var teacher = await factory.CreateUserAsync(displayName: "Ustoz Ali");
        var student = await factory.CreateUserAsync(displayName: "Aziza");

        // Yaratish
        var created = await Ok(await teacher.Client.PostAsync("/api/teacher/groups", JsonExtensions.Json(new { name = "  IELTS 6.5 kechki  " })));
        var groupId = created.GetProperty("id").GetGuid();
        var code = created.GetProperty("joinCode").GetString()!;
        Assert.Equal("IELTS 6.5 kechki", created.GetProperty("name").GetString());
        Assert.Equal(6, code.Length);

        // Ro'yxat
        var list = await Ok(await teacher.Client.GetAsync("/api/teacher/groups"));
        var row = list.EnumerateArray().Single();
        Assert.Equal(groupId, row.GetProperty("id").GetGuid());
        Assert.Equal(0, row.GetProperty("members").GetInt32());
        Assert.Equal(0, row.GetProperty("assignments").GetInt32());
        Assert.Equal(code, row.GetProperty("joinCode").GetString());

        // O'quvchi koddan qaraydi va qo'shiladi (kod kichik harf va tire bilan ham qabul qilinadi)
        var info = await Ok(await student.Client.GetAsync($"/api/groups/join/{code.ToLowerInvariant()}"));
        Assert.Equal("IELTS 6.5 kechki", info.GetProperty("name").GetString());
        Assert.Equal("Ustoz Ali", info.GetProperty("teacher").GetString());
        Assert.False(info.GetProperty("isTeacher").GetBoolean());
        Assert.False(info.GetProperty("isMember").GetBoolean());
        var joined = await Ok(await student.Client.PostAsync($"/api/groups/join/{code}", null));
        Assert.True(joined.GetProperty("joined").GetBoolean());
        Assert.False((await Ok(await student.Client.PostAsync($"/api/groups/join/{code}", null))).GetProperty("joined").GetBoolean());   // ikkinchi marta — o'zgarmaydi

        // Vazifa
        var assignment = await Ok(await teacher.Client.PostAsync($"/api/teacher/groups/{groupId}/assignments",
            JsonExtensions.Json(new { kind = "practice:writing", instructions = "  Write 150 words  " })));
        var assignmentId = assignment.GetProperty("id").GetGuid();

        // Guruh tafsiloti
        var detail = await Ok(await teacher.Client.GetAsync($"/api/teacher/groups/{groupId}"));
        Assert.Equal(code, detail.GetProperty("group").GetProperty("joinCode").GetString());
        var member = detail.GetProperty("members").EnumerateArray().Single();
        Assert.Equal("Aziza", member.GetProperty("name").GetString());
        Assert.Equal(student.Id, member.GetProperty("userId").GetGuid());
        var a = detail.GetProperty("assignments").EnumerateArray().Single();
        Assert.Equal("practice:writing", a.GetProperty("kind").GetString());
        Assert.Equal("Write 150 words", a.GetProperty("instructions").GetString());
        Assert.Equal(0, a.GetProperty("done").GetInt32());
        var status = detail.GetProperty("results").GetProperty(assignmentId.ToString()).GetProperty(student.Id.ToString());
        Assert.False(status.GetProperty("done").GetBoolean());

        // O'quvchining vazifalari
        var tasks = await Ok(await student.Client.GetAsync("/api/student/assignments"));
        Assert.Equal("Ustoz Ali", tasks.GetProperty("groups")[0].GetProperty("teacher").GetString());
        var task = tasks.GetProperty("assignments")[0];
        Assert.Equal(assignmentId, task.GetProperty("id").GetGuid());
        Assert.Equal("IELTS 6.5 kechki", task.GetProperty("groupName").GetString());
        Assert.False(task.GetProperty("status").GetProperty("done").GetBoolean());

        // O'qituvchi o'quvchining urinishini so'raydi (hali bajarilmagan): status bor, activity null
        var attempt = await Ok(await teacher.Client.GetAsync($"/api/teacher/assignments/{assignmentId}/students/{student.Id}"));
        Assert.Equal(JsonValueKind.Null, attempt.GetProperty("activity").ValueKind);

        // Nomini o'zgartirish, yangi kod
        var renamed = await Ok(await teacher.Client.PutAsync($"/api/teacher/groups/{groupId}", JsonExtensions.Json(new { name = "Yangi nom" })));
        Assert.Equal("Yangi nom", renamed.GetProperty("name").GetString());
        var newCode = (await Ok(await teacher.Client.PostAsync($"/api/teacher/groups/{groupId}/code", null))).GetProperty("joinCode").GetString()!;
        Assert.NotEqual(code, newCode);
        var oldCode = await student.Client.GetAsync($"/api/groups/join/{code}");
        Assert.Equal(HttpStatusCode.NotFound, oldCode.StatusCode);                                    // eski kod endi ishlamaydi

        // Tozalash: vazifa, a'zo, guruh
        Assert.True((await Ok(await teacher.Client.DeleteAsync($"/api/teacher/assignments/{assignmentId}"))).GetProperty("deleted").GetBoolean());
        Assert.True((await Ok(await teacher.Client.DeleteAsync($"/api/teacher/groups/{groupId}/members/{student.Id}"))).GetProperty("removed").GetBoolean());
        Assert.True((await Ok(await student.Client.DeleteAsync($"/api/student/groups/{groupId}"))).GetProperty("left").GetBoolean());
        Assert.True((await Ok(await teacher.Client.DeleteAsync($"/api/teacher/groups/{groupId}"))).GetProperty("deleted").GetBoolean());
        var gone = await teacher.Client.GetAsync($"/api/teacher/groups/{groupId}");
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
        Assert.Equal(Msg.Err("group.not_found"), (await gone.JsonAsync()).GetProperty("error").GetString());
    }

    [Fact]
    public async Task Validation_errors_come_back_as_400_with_a_localized_message()
    {
        var teacher = await factory.CreateUserAsync();
        var bad = await teacher.Client.PostAsync("/api/teacher/groups", JsonExtensions.Json(new { name = "x" }));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Equal(Msg.Err("group.name_invalid"), (await bad.JsonAsync()).GetProperty("error").GetString());

        var group = await (await teacher.Client.PostAsync("/api/teacher/groups", JsonExtensions.Json(new { name = "Group A" }))).JsonAsync();
        var id = group.GetProperty("id").GetGuid();

        var kind = await teacher.Client.PostAsync($"/api/teacher/groups/{id}/assignments", JsonExtensions.Json(new { kind = "nonsense" }));
        Assert.Equal(HttpStatusCode.BadRequest, kind.StatusCode);
        Assert.Equal(Msg.Err("assignment.bad_kind"), (await kind.JsonAsync()).GetProperty("error").GetString());

        var own = await teacher.Client.PostAsync($"/api/groups/join/{group.GetProperty("joinCode").GetString()}", null);
        Assert.Equal(HttpStatusCode.BadRequest, own.StatusCode);                                       // o'z guruhiga qo'shilib bo'lmaydi
        Assert.Equal(Msg.Err("group.own_group"), (await own.JsonAsync()).GetProperty("error").GetString());

        var unknown = await teacher.Client.GetAsync("/api/groups/join/ZZZZZZ");
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(Msg.Err("group.bad_code"), (await unknown.JsonAsync()).GetProperty("error").GetString());
    }

    [Fact]
    public async Task Only_the_owner_can_see_or_change_a_group()
    {
        var owner = await factory.CreateUserAsync();
        var stranger = await factory.CreateUserAsync();
        var id = (await (await owner.Client.PostAsync("/api/teacher/groups", JsonExtensions.Json(new { name = "Private" }))).JsonAsync()).GetProperty("id").GetGuid();

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.Client.GetAsync($"/api/teacher/groups/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.Client.PutAsync($"/api/teacher/groups/{id}", JsonExtensions.Json(new { name = "Hacked" }))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.Client.DeleteAsync($"/api/teacher/groups/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.Client.PostAsync($"/api/teacher/groups/{id}/assignments", JsonExtensions.Json(new { kind = "review", target = 5 }))).StatusCode);
        Assert.Empty((await stranger.Client.GetAsync("/api/teacher/groups").ContinueWith(t => t.Result.JsonAsync()).Unwrap()).EnumerateArray());
        Assert.Single((await owner.Client.GetAsync("/api/teacher/groups").ContinueWith(t => t.Result.JsonAsync()).Unwrap()).EnumerateArray());
    }

    [Fact]
    public async Task A_review_assignment_needs_a_target_between_1_and_500()
    {
        var teacher = await factory.CreateUserAsync();
        var id = (await (await teacher.Client.PostAsync("/api/teacher/groups", JsonExtensions.Json(new { name = "Reviewers" }))).JsonAsync()).GetProperty("id").GetGuid();
        var bad = await teacher.Client.PostAsync($"/api/teacher/groups/{id}/assignments", JsonExtensions.Json(new { kind = "review", target = 501 }));
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Equal(Msg.Err("assignment.bad_target", 1, 500), (await bad.JsonAsync()).GetProperty("error").GetString());
        Assert.Equal(HttpStatusCode.OK, (await teacher.Client.PostAsync($"/api/teacher/groups/{id}/assignments", JsonExtensions.Json(new { kind = "review", target = 20 }))).StatusCode);
    }
}
