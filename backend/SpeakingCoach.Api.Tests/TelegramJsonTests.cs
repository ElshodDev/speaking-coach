using System.Text.Json;
using SpeakingCoach.Api.Services.Telegram;

namespace SpeakingCoach.Api.Tests;

/// <summary>Telegram yuboradigan haqiqiy JSON shakli bizning turlarga to'g'ri o'qiladimi.</summary>
public class TelegramJsonTests
{
    [Fact]
    public void Message_update_is_parsed()
    {
        const string json = """
            {"update_id":10000,
             "message":{"message_id":1365,"date":1441645532,
               "chat":{"id":1111111,"type":"private","first_name":"Aziza"},
               "from":{"id":1111111,"is_bot":false,"first_name":"Aziza","username":"aziza_uz","language_code":"ru"},
               "text":"/start abc123"}}
            """;

        var u = JsonSerializer.Deserialize<TgUpdate>(json)!;

        Assert.Equal(10000, u.UpdateId);
        Assert.Equal(1111111, u.Message!.Chat.Id);
        Assert.Equal("private", u.Message.Chat.Type);
        Assert.Equal("ru", u.Message.From!.LanguageCode);
        Assert.Equal("aziza_uz", u.Message.From.Username);
        Assert.Equal("abc123", BotLogic.StartPayload(u.Message.Text));
        Assert.Null(u.CallbackQuery);
    }

    [Fact]
    public void Callback_update_is_parsed()
    {
        const string json = """
            {"update_id":10001,
             "callback_query":{"id":"4382bfdwdsb323b2d9","chat_instance":"-1",
               "from":{"id":1111111,"is_bot":false,"first_name":"Aziza"},
               "message":{"message_id":1366,"chat":{"id":1111111,"type":"private"},"text":"reliable"},
               "data":"rv:start"}}
            """;

        var u = JsonSerializer.Deserialize<TgUpdate>(json)!;

        Assert.Equal("4382bfdwdsb323b2d9", u.CallbackQuery!.Id);
        Assert.Equal(1366, u.CallbackQuery.Message!.MessageId);
        Assert.IsType<BotCallback.StartReview>(BotLogic.Decode(u.CallbackQuery.Data));
    }

    [Fact]
    public void Buttons_serialize_only_the_fields_that_are_set()
    {
        var options = new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
        var cb = JsonSerializer.Serialize(new TgButton("Show", "rv:start"), options);
        var url = JsonSerializer.Serialize(new TgButton("Open", Url: "https://example.com"), options);

        Assert.Equal("""{"text":"Show","callback_data":"rv:start"}""", cb);
        Assert.Equal("""{"text":"Open","url":"https://example.com"}""", url);
    }

    [Fact]
    public void Updates_are_processed_only_once()
    {
        var id = Random.Shared.NextInt64(1_000_000_000, 2_000_000_000);

        Assert.True(TelegramBot.MarkSeen(id));
        Assert.False(TelegramBot.MarkSeen(id));
    }
}
