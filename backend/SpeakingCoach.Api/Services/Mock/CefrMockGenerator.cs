namespace SpeakingCoach.Api.Services.Mock;

/// <summary>CEFR Listening (6 qism) va Reading (5 qism) — har qism alohida so'rov, parallel.</summary>
public partial class GeminiMockGenerator
{
    public async Task<ListeningTest> GenerateCefrListeningAsync(CancellationToken ct = default)
    {
        var topics = CefrObjective.Topics.OrderBy(_ => Random.Shared.Next()).Take(6).ToArray();
        topics[3] = CefrObjective.MapPlaces[Random.Shared.Next(CefrObjective.MapPlaces.Length)];
        var tasks = CefrObjective.ListeningLayout.Select(l => AskAsync<ListeningPart>(
            CefrObjective.ListeningPrompt(l.Part, topics[l.Part - 1]),
            p => CefrObjective.ValidateListening(p, l.Part), ct, attempts: 3));
        var parts = await Task.WhenAll(tasks);
        return new ListeningTest(parts.ToList());
    }

    public async Task<ReadingTest> GenerateCefrReadingAsync(CancellationToken ct = default)
    {
        var topics = CefrObjective.Topics.OrderBy(_ => Random.Shared.Next()).Take(5).ToArray();
        var tasks = CefrObjective.ReadingLayout.Select(l => AskAsync<ReadingPassage>(
            CefrObjective.ReadingPrompt(l.Part, topics[l.Part - 1]),
            p => CefrObjective.ValidateReading(p, l.Part), ct, attempts: 3));
        var passages = await Task.WhenAll(tasks);
        return new ReadingTest("", passages.ToList());
    }
}
