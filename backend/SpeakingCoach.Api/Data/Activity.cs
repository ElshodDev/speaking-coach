namespace SpeakingCoach.Api.Data;

public enum ActivityType
{
    Speaking = 0,
    Writing = 1,
    Reading = 2,
    Listening = 3,
}

public class Activity
{
    public Guid Id { get; set; }
    public ActivityType Type { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string PromptData { get; set; } = "{}";
    public string ResponseData { get; set; } = "{}";
}
