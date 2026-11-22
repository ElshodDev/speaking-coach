namespace SpeakingCoach.Application.Abstractions;

/// <summary>Tuzatishlar olinadigan so'rov: faqat "topCorrections" (natijaning qolgan qismi bazadan tortilmaydi).</summary>
public sealed class CorrectionsRow
{
    public DateTime CreatedAtUtc { get; set; }
    public string Json { get; set; } = "{}";
}

public static class MistakeQuery
{
    /// <summary>Ko'pi bilan shuncha oxirgi (tuzatishi bor) faoliyat tahlil qilinadi.</summary>
    public const int MaxActivities = 400;

    /// <summary>Tur ro'yxati "Mening xatolarim" manbalari (Speaking, Writing, mock, AI suhbat) bilan bir xil (testda tekshiriladi).</summary>
    public const string CorrectionsSql = """
        SELECT "CreatedAtUtc", jsonb_build_object('topCorrections', "ResponseData"->'topCorrections')::text AS "Json"
        FROM "Activities"
        WHERE "UserId" = {0} AND "Type" IN ('Speaking', 'Writing', 'MockExam', 'Conversation')
          AND jsonb_typeof("ResponseData"->'topCorrections') = 'array'
        ORDER BY "CreatedAtUtc" DESC
        LIMIT {1}
        """;
}

/// <summary>Foydalanuvchining oxirgi tuzatishlari (xato turlarini hisoblash uchun). Amalga oshirish — Infrastructure.</summary>
public interface IMistakeSource
{
    Task<List<MistakeFact>> RecentAsync(Guid userId, CancellationToken ct = default);
}
