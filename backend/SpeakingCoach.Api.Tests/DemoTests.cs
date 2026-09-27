using System.Text.Json;
using SpeakingCoach.Api.Data;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Tests;

public class DemoTests
{
    private static readonly Guid User = Guid.Parse("99999999-2222-3333-4444-555555555555");

    [Theory]
    [InlineData(-300, 14, 0)]   // Toshkent, kunduzi
    [InlineData(-300, 19, 5)]   // Toshkent, yarim tunga yaqin (00:05 mahalliy)
    [InlineData(240, 3, 0)]     // Nyu-York (UTC−4)
    public void Demo_data_has_a_streak_a_partly_done_day_and_due_cards(int tz, int utcHour, int minute)
    {
        var now = new DateTime(2026, 9, 27, utcHour, minute, 0, DateTimeKind.Utc);
        var data = DemoSeed.Build(User, now, tz);

        var times = data.Logs.Select(l => l.ReviewedAtUtc).Concat(data.Activities.Select(a => a.CreatedAtUtc));
        Assert.Equal(DemoSeed.StreakDays + 1, ReviewScheduler.ComputeStreak(times, now, tz));

        var today = ReviewScheduler.ToLocalDate(now, tz);
        Assert.Equal(DemoSeed.ReviewedToday, data.Logs.Count(l => ReviewScheduler.ToLocalDate(l.ReviewedAtUtc, tz) == today));

        var due = data.Cards.Count(c => c.DueAtUtc <= now);
        Assert.True(due >= 5 && due < data.Cards.Count, $"due {due} of {data.Cards.Count}");
        Assert.All(data.Activities, a => Assert.True(a.CreatedAtUtc < now));
        Assert.All(data.Logs, l => Assert.True(l.ReviewedAtUtc <= now));
    }

    [Fact]
    public void Demo_data_uses_the_real_result_shapes()
    {
        var data = DemoSeed.Build(User, new DateTime(2026, 9, 27, 12, 0, 0, DateTimeKind.Utc), -300);

        Assert.Equal(3, data.Activities.Count(a => a.Type == ActivityType.Speaking));
        Assert.Equal(2, data.Activities.Count(a => a.Type == ActivityType.Writing));
        Assert.Equal(1, data.Activities.Count(a => a.Type == ActivityType.Reading));
        Assert.Equal(1, data.Activities.Count(a => a.Type == ActivityType.Listening));
        Assert.Equal(1, data.Activities.Count(a => a.Type == ActivityType.Shadowing));

        foreach (var a in data.Activities)
        {
            Assert.Equal(User, a.UserId);
            switch (a.Type)
            {
                case ActivityType.Speaking:
                    GeminiClient.DeserializeStrict<SpeakingEvaluationResult>(a.ResponseData);
                    Assert.False(string.IsNullOrEmpty(JsonDocument.Parse(a.PromptData).RootElement.GetProperty("topic").GetString()));
                    break;
                case ActivityType.Writing:
                    GeminiClient.DeserializeStrict<WritingEvaluationResult>(a.ResponseData);
                    break;
                case ActivityType.Reading or ActivityType.Listening:
                    var r = GeminiClient.DeserializeStrict<ComprehensionResult>(a.ResponseData);
                    Assert.Equal(4, r.Total);
                    GeminiClient.DeserializeStrict<ComprehensionExercise>(a.PromptData);
                    break;
            }
        }

        // Kartalar: tuzatishlar, xato javoblar va lug'at so'zlari; har bir log mavjud kartaga tegishli.
        Assert.Contains(data.Cards, c => c.Kind == ReviewCardKind.Correction);
        Assert.Contains(data.Cards, c => c.Kind == ReviewCardKind.Question);
        Assert.Equal(6, data.Cards.Count(c => c.Kind == ReviewCardKind.Word));
        Assert.Equal(data.Cards.Count, data.Cards.Select(c => c.Front.ToLowerInvariant()).Distinct().Count());
        var ids = data.Cards.Select(c => c.Id).ToHashSet();
        Assert.All(data.Logs, l => Assert.Contains(l.CardId, ids));
        Assert.All(data.Cards, c => Assert.Equal(User, c.UserId));
    }

    [Fact]
    public void Demo_emails_are_recognised_and_never_real()
    {
        var email = DemoAccount.NewEmail();
        Assert.True(DemoAccount.IsDemo(email));
        Assert.True(email.EndsWith(".invalid", StringComparison.Ordinal));
        Assert.NotEqual(email, DemoAccount.NewEmail());
        Assert.False(DemoAccount.IsDemo("ali@mail.com"));
        Assert.False(DemoAccount.IsDemo("demo@speakingcoach.invalid.com"));
        Assert.False(DemoAccount.IsDemo(null));
    }

    [Fact]
    public void Demo_gate_limits_per_ip_and_per_day()
    {
        var gate = new DemoGate();
        for (var i = 0; i < DemoGate.PerIpPerDay; i++) Assert.True(gate.TryTake("1.1.1.1"));
        Assert.False(gate.TryTake("1.1.1.1"));
        Assert.True(gate.TryTake("2.2.2.2"));
    }
}
