using System.Text.Json;

namespace SpeakingCoach.Application.Mock;

/// <summary>Listening/Reading: brauzerga beriladigan ko'rinish va javoblarni serverda tekshirish (AI'siz, toza mantiq).</summary>
public static class MockGrading
{
    public static object ClientPayload(string module, Guid id, string payload) => module == "reading"
        ? ClientView.Reading(id, JsonSerializer.Deserialize<ReadingTest>(payload, GeminiMockGenerator.Web)!)
        : ClientView.Listening(id, JsonSerializer.Deserialize<ListeningTest>(payload, GeminiMockGenerator.Web)!);

    public static ObjectiveResult GradeObjective(string module, MockTest test, IReadOnlyDictionary<string, string> answers, int seconds)
    {
        List<QuestionGroup> groups;
        (int, decimal)[] anchors;
        if (module == "reading")
        {
            var t = JsonSerializer.Deserialize<ReadingTest>(test.Payload, GeminiMockGenerator.Web)!;
            groups = t.Passages.SelectMany(p => p.Groups).ToList();
            anchors = t.Variant == IeltsBank.General ? ObjectiveGrading.GeneralReadingAnchors : ObjectiveGrading.AcademicReadingAnchors;
        }
        else
        {
            var t = JsonSerializer.Deserialize<ListeningTest>(test.Payload, GeminiMockGenerator.Web)!;
            groups = t.Parts.SelectMany(p => p.Groups).ToList();
            anchors = ObjectiveGrading.ListeningAnchors;
        }
        var review = ObjectiveGrading.Grade(groups, answers);
        var score = review.Count(q => q.Correct);
        return new ObjectiveResult("ielts", module, test.Id.ToString(), test.Variant, score, review.Count,
            ObjectiveGrading.BandFor(score, anchors, review.Count), seconds, review);
    }

    /// <summary>CEFR Listening/Reading: to'g'ri javoblar soni, 75 ballik taxminiy natija va daraja.</summary>
    public static CefrObjectiveResult GradeCefrObjective(string module, MockTest test, IReadOnlyDictionary<string, string> answers, int seconds)
    {
        var layout = module == "reading" ? CefrObjective.ReadingLayout : CefrObjective.ListeningLayout;
        var groups = module == "reading"
            ? JsonSerializer.Deserialize<ReadingTest>(test.Payload, GeminiMockGenerator.Web)!.Passages.SelectMany(p => p.Groups).ToList()
            : JsonSerializer.Deserialize<ListeningTest>(test.Payload, GeminiMockGenerator.Web)!.Parts.SelectMany(p => p.Groups).ToList();
        var review = ObjectiveGrading.Grade(groups, answers);
        var score = review.Count(q => q.Correct);
        var overall = CefrObjective.Score(score, review.Count);
        var parts = layout.Select(l => new CefrPartScore(l.Part,
            review.Count(q => q.Correct && q.Number >= l.First && q.Number < l.First + l.Count), l.Count)).ToList();
        return new CefrObjectiveResult("cefr", module, test.Id.ToString(), "", score, review.Count, overall, CefrScale.Level(overall), seconds, parts, review);
    }
}
