namespace SpeakingCoach.Application;

/// <summary>Servis xatosi: tarjima kaliti, HTTP holat kodi va kalit argumentlari (HTTP'ga aylantirish — Api'da).</summary>
public record Fail(string Key, int Status, params object?[] Args);

/// <summary>Servis natijasi: muvaffaqiyat (Value) yoki xato (Fail). Application HTTP'ni bilmaydi.</summary>
public class Outcome<T>
{
    public T? Value { get; private init; }
    public Fail? Failure { get; private init; }
    public bool Ok => Failure is null;

    public static Outcome<T> Success(T value) => new() { Value = value };
    public static Outcome<T> Error(string key, int status, params object?[] args) => new() { Failure = new Fail(key, status, args) };
}
