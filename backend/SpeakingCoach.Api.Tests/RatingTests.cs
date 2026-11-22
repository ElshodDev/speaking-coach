namespace SpeakingCoach.Api.Tests;

public class RatingTests
{
    [Theory]
    [InlineData("first_practice:up")]
    [InlineData("lesson:up")]
    [InlineData("lesson:down")]
    [InlineData("first_practice:down")]
    [InlineData("ai_fair:speaking:fair")]
    [InlineData("ai_fair:writing:high")]
    [InlineData("ai_fair:part2:low")]
    [InlineData("ai_fair:mock:fair")]
    [InlineData("source:instagram")]
    [InlineData("source:telegram")]
    [InlineData("source:friend")]
    [InlineData("source:teacher")]
    [InlineData("source:search")]
    [InlineData("source:other")]
    public void Known_keys_are_accepted(string key)
    {
        Assert.True(RatingRules.IsValid(key));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("first_practice:maybe")]
    [InlineData("lesson:maybe")]
    [InlineData("lesson_start:free")]                      // boshlanish hodisasini faqat server yozadi: soxtalashtirib bo'lmaydi
    [InlineData("lesson_start:assigned")]
    [InlineData("lesson:up\nlesson:down")]
    [InlineData("ai_fair:listening:fair")]
    [InlineData("ai_fair:speaking:great")]
    [InlineData("source:tiktok")]
    [InlineData("source:instagram ")]                      // bo'sh joy — qat'iy
    [InlineData("SOURCE:instagram")]                       // katta-kichik harf — qat'iy
    [InlineData("first_practice:up\nsource:other")]        // qator ko'chirish orqali aldash
    [InlineData("first_practice:up\n")]                    // oxiridagi qator ko'chirish ham mumkin emas
    [InlineData("<script>alert(1)</script>")]
    [InlineData("first_practice:up; DROP TABLE Feedbacks")]
    public void Unknown_or_tricky_keys_are_rejected(string? key)
    {
        Assert.False(RatingRules.IsValid(key));
    }

    [Fact]
    public void Overlong_keys_are_rejected()
    {
        Assert.False(RatingRules.IsValid("source:" + new string('a', 200)));
    }

    [Theory]
    [InlineData("first_practice:up", "first_practice:")]
    [InlineData("first_practice:down", "first_practice:")]
    [InlineData("source:friend", "source:")]
    [InlineData("ai_fair:speaking:fair", null)]   // har natijaga alohida baho berish mumkin
    public void Once_only_ratings_are_identified(string key, string? expected)
    {
        Assert.Equal(expected, RatingRules.OncePrefix(key));
    }

    [Fact]
    public void Rating_kind_is_not_one_of_the_written_feedback_kinds()
    {
        // /api/feedback "rating" turini qabul qilmaydi: baholar faqat qat'iy ro'yxatli /api/rating orqali keladi.
        Assert.DoesNotContain(RatingRules.Kind, FeedbackRules.Kinds);
        Assert.NotNull(FeedbackRules.Validate(RatingRules.Kind, "first_practice:up"));
    }
}
