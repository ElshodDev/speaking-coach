using System.Text.Json;

namespace SpeakingCoach.Application.Mock;

/// <summary>Mock urinishlarining yengil ko'rinishi va PromptData'ni o'qish (toza, bazasiz).</summary>
public static class MockQueries
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Mock natijalarining yengil ko'rinishi: katta JSON (javoblar, izohlar) bazadan
    /// tortilmaydi — kerakli maydonlar (exam, module, setId, overall...) Postgres'ning
    /// o'zida jsonb'dan o'qiladi.
    /// </summary>
    public sealed class MockRow
    {
        public Guid Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public string? Exam { get; set; }
        public string? Module { get; set; }
        public string? SetId { get; set; }
        public string? Variant { get; set; }
        public string? SessionId { get; set; }
        public decimal? Overall { get; set; }

        public MockPrompt Prompt => new(Exam ?? "ielts", Module ?? "", SetId ?? "", Variant, SessionId);
    }

    public record MockPrompt(string Exam, string Module, string SetId, string? Variant, string? SessionId = null);

    public static MockPrompt ParsePrompt(string json)
    {
        try
        {
            var p = JsonSerializer.Deserialize<MockPrompt>(json, Web);
            return p is null ? new("ielts", "", "", null) : p with { Exam = p.Exam ?? "ielts", Module = p.Module ?? "", SetId = p.SetId ?? "" };
        }
        catch (JsonException)
        {
            return new("ielts", "", "", null);
        }
    }
}

/// <summary>
/// Foydalanuvchining mock urinishlari (eng yangisi birinchi). Postgres'ga xos jsonb
/// so'rovi Infrastructure'da amalga oshiriladi — Application faqat shu interfeysni biladi.
/// </summary>
public interface IMockRows
{
    Task<List<MockQueries.MockRow>> ForUserAsync(Guid userId, int take, CancellationToken ct = default);
}
