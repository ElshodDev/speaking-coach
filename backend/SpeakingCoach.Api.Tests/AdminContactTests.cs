using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Tests;

public class AdminContactTests
{
    [Theory]
    [InlineData("ali@mail.com", null, null, "ali@mail.com")]
    [InlineData("ali@mail.com", "ali_uz", null, "ali@mail.com · @ali_uz")]
    [InlineData("tg-777@telegram.invalid", "aziza", "Aziza", "@aziza")]
    [InlineData("tg-777@telegram.invalid", "@aziza", null, "@aziza")]
    [InlineData("tg-777@telegram.invalid", null, "Aziza", "Aziza (Telegram ID 777)")]
    [InlineData("tg-777@telegram.invalid", "  ", null, "Telegram ID 777")]
    public void Admin_sees_the_full_email_or_the_telegram_identity(string email, string? tg, string? name, string expected)
    {
        Assert.Equal(expected, AdminService.Contact(email, tg, name));
    }
}
