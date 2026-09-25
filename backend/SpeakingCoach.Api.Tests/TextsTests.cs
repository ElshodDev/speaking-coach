using System.Text.RegularExpressions;
using SpeakingCoach.Api.Services;

namespace SpeakingCoach.Api.Tests;

public class TextsTests
{
    [Theory]
    [InlineData(null, "uz")]
    [InlineData("", "uz")]
    [InlineData("ru", "ru")]
    [InlineData("ru-RU,ru;q=0.9,en;q=0.8", "ru")]
    [InlineData("en-GB", "en")]
    [InlineData("de-DE,en;q=0.5", "en")]
    [InlineData("uz-Latn-UZ", "uz")]
    [InlineData("fr-FR", "uz")]
    public void Language_is_taken_from_accept_language(string? header, string expected)
    {
        Assert.Equal(expected, Texts.ParseLang(header));
    }

    [Fact]
    public void Every_message_exists_in_all_languages_with_the_same_placeholders()
    {
        foreach (var key in Texts.Keys)
        {
            var uz = Texts.Get("uz", key);
            var ru = Texts.Get("ru", key);
            var en = Texts.Get("en", key);

            Assert.False(string.IsNullOrWhiteSpace(uz));
            Assert.NotEqual(uz, ru);
            Assert.NotEqual(uz, en);
            var placeholders = Placeholders(uz);
            Assert.Equal(placeholders, Placeholders(ru));
            Assert.Equal(placeholders, Placeholders(en));
        }
    }

    [Fact]
    public void Uzbek_messages_use_the_official_orthography()
    {
        // Rasmiy lotin yozuvi: oʻ/gʻ (U+02BB), tutuq belgisi ʼ (U+02BC) — oddiy ' emas.
        foreach (var key in Texts.Keys)
        {
            var uz = Texts.Get("uz", key);
            Assert.DoesNotMatch(new Regex("[oOgG]'"), uz);
        }
    }

    [Fact]
    public void Parameters_are_formatted_into_the_message()
    {
        Assert.Equal("Matn juda qisqa (kamida 20 ta belgi kerak).", Texts.Get("uz", "text.too_short", 20));
        Assert.Equal("Expected 4 answers but got 2.", Texts.Get("en", "exercise.answers_count", 4, 2));
    }

    [Fact]
    public void Unknown_key_is_returned_as_is()
    {
        Assert.Equal("no.such.key", Texts.Get("ru", "no.such.key"));
    }

    [Fact]
    public void User_input_exception_keeps_key_and_arguments()
    {
        var ex = new UserInputException("exercise.bad_answer", 3);

        Assert.Equal("exercise.bad_answer", ex.Key);
        Assert.Equal("Некорректный ответ на вопрос 3.", Texts.Get("ru", ex.Key, ex.Args));
    }

    [Theory]
    [InlineData("ru", "Russian translation")]
    [InlineData("en", "English synonym")]
    [InlineData("uz", "Uzbek translation")]
    public void Word_prompt_asks_for_a_translation_in_the_interface_language(string lang, string expected)
    {
        var prompt = GeminiWordService.BuildPrompt("bank", "", "B1", lang);

        Assert.Contains(expected, prompt);
    }

    private static string Placeholders(string s) =>
        string.Join(",", Regex.Matches(s, @"\{\d+\}").Select(m => m.Value).OrderBy(x => x));
}
