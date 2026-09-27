using System.Text.Json;

namespace SpeakingCoach.Api.Services.Mock;

/// <summary>
/// Bot orqali test qo'shish: o'qituvchi materialini (matn, PDF, rasm)
/// bizning formatga o'giradi yoki mavzu bo'yicha yangi test yaratadi.
/// Listening/Reading qism-qism (parallel) — har qism mavjud qat'iy
/// tekshiruvlardan o'tadi; Speaking/Writing — bitta so'rov.
/// </summary>
public partial class GeminiMockGenerator : IMockAuthoring
{
    public async Task<AuthoredTest> CreateAsync(AuthorKind kind, AuthorSource source, CancellationToken ct = default)
    {
        var extra = source.IsTopic ? null : MockAuthoringRules.SourceParts(source);
        if (!source.IsTopic && (extra is null || extra.Length == 0)) throw new InvalidOperationException("Material bo'sh");
        var topic = source.Topic ?? "the topic of the attached material";
        const int attempts = 3;

        object content = (kind.Exam, kind.Module) switch
        {
            ("ielts", "speaking") => await AskAsync<SpeakingSet>(MockAuthoringRules.IeltsSpeakingPrompt(source), MockAuthoringRules.ValidateIeltsSpeaking, ct, attempts, extra),
            ("ielts", "writing") => await AskAsync<WritingSet>(MockAuthoringRules.IeltsWritingPrompt(kind.Variant, source), w => MockAuthoringRules.ValidateIeltsWriting(w, kind.Variant), ct, attempts, extra),
            ("cefr", "speaking") => await AskAsync<CefrSpeakingSet>(MockAuthoringRules.CefrSpeakingPrompt(source), MockAuthoringRules.ValidateCefrSpeaking, ct, attempts, extra),
            ("cefr", "writing") => await AskAsync<CefrWritingSet>(MockAuthoringRules.CefrWritingPrompt(source), MockAuthoringRules.ValidateCefrWriting, ct, attempts, extra),
            ("ielts", "reading") => new ReadingTest(kind.Variant, (await Task.WhenAll(IeltsReadingLayout.Select((l, i) => AskAsync<ReadingPassage>(
                ReadingPrompt(kind.Variant, i + 1, l.First, l.Count, topic, source.IsTopic ? null : MockAuthoringRules.SourceNote($"Reading Passage {i + 1} (Section {i + 1})")),
                p => ValidatePassage(p, l.First, l.Count), ct, attempts, extra)))).ToList()),
            ("ielts", "listening") => new ListeningTest((await Task.WhenAll(Enumerable.Range(1, 4).Select(part => AskAsync<ListeningPart>(
                ListeningPrompt(part, (part - 1) * 10 + 1, topic, source.IsTopic ? null : MockAuthoringRules.SourceNote($"Part {part} (Section {part}, questions {(part - 1) * 10 + 1}-{part * 10})")),
                p => ValidatePart(p, part), ct, attempts, extra)))).ToList()),
            ("cefr", "reading") => new ReadingTest("", (await Task.WhenAll(CefrObjective.ReadingLayout.Select(l => AskAsync<ReadingPassage>(
                CefrObjective.ReadingPrompt(l.Part, topic, source.IsTopic ? null : MockAuthoringRules.SourceNote($"Part {l.Part} (questions {l.First}-{l.First + l.Count - 1})")),
                p => CefrObjective.ValidateReading(p, l.Part), ct, attempts, extra)))).ToList()),
            ("cefr", "listening") => new ListeningTest((await Task.WhenAll(CefrObjective.ListeningLayout.Select(l => AskAsync<ListeningPart>(
                CefrObjective.ListeningPrompt(l.Part, topic, source.IsTopic ? null : MockAuthoringRules.SourceNote($"Part {l.Part} (questions {l.First}-{l.First + l.Count - 1})")),
                p => CefrObjective.ValidateListening(p, l.Part), ct, attempts, extra)))).ToList()),
            _ => throw new InvalidOperationException($"Noma'lum tur: {kind.Key}"),
        };

        var payload = JsonSerializer.Serialize(content, content.GetType(), Web);
        return new AuthoredTest(kind, payload, MockAuthoringRules.TitleOf(kind, payload));
    }

    /// <summary>IELTS Reading: 3 matn, 13 + 13 + 14 = 40 savol.</summary>
    public static readonly (int First, int Count)[] IeltsReadingLayout = [(1, 13), (14, 13), (27, 14)];
}
