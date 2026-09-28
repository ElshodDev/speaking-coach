using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Tests;

public class FrontendOriginsTests
{
    [Fact]
    public void Several_origins_are_split_and_the_first_is_the_main_one()
    {
        var o = FrontendOrigins.Parse(" https://fluentuz.app/ , https://speaking-coach-theta.vercel.app");
        Assert.Equal(2, o.Length);
        Assert.Equal("https://fluentuz.app", o[0]);
        Assert.Equal("https://speaking-coach-theta.vercel.app", o[1]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ,  ")]
    [InlineData("fluentuz.app")]
    public void Missing_or_invalid_value_falls_back_to_localhost(string? value)
    {
        Assert.Equal([FrontendOrigins.Default], FrontendOrigins.Parse(value));
    }

    [Fact]
    public void Duplicates_are_removed()
    {
        Assert.Single(FrontendOrigins.Parse("https://a.uz,https://A.uz/"));
    }
}
