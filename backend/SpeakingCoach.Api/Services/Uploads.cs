using Microsoft.AspNetCore.Http.Features;

namespace SpeakingCoach.Api.Services;

/// <summary>
/// Audio yuklash qoidalari (Render: 512 MB xotira). Tartib muhim:
/// <list type="number">
/// <item>Content-Length — forma o'qilishidan OLDIN tekshiriladi (katta so'rov darhol 413);</item>
/// <item>"katta so'rov" joyi (1 MB dan katta so'rovlar uchun, bir vaqtda ko'pi bilan 3 ta) — audio xotiraga o'qilishidan OLDIN olinadi;</item>
/// <item>har bir yozuv — ko'pi bilan 8 MB (brauzer ~1 daqiqaga 0,5–1 MB yozadi).</item>
/// </list>
/// Umumiy (Kestrel) chegara — 2 MB; audio endpoint'lar o'z chegarasini metadata bilan beradi.
/// </summary>
public static class Uploads
{
    public const long Mb = 1024 * 1024;

    /// <summary>Bitta yozuv (Speaking mashqi, mock javobi).</summary>
    public const long MaxRecordingBytes = 8 * Mb;

    /// <summary>Oddiy JSON so'rovlar uchun umumiy chegara (Kestrel).</summary>
    public const long DefaultBodyBytes = 2 * Mb;

    /// <summary>Multipart formadagi fayllar + maydonlar uchun qo'shimcha joy.</summary>
    public const long FormOverheadBytes = 256 * 1024;

    /// <summary>Audio'ni xotiraga o'qib, Gemini'ga yuboradigan so'rovlar bir vaqtda nechta.</summary>
    public const int MaxConcurrent = 3;

    private static readonly SemaphoreSlim Slots = new(MaxConcurrent, MaxConcurrent);

    /// <summary>Content-Length ma'lum va chegaradan katta — formani o'qimasdan rad etamiz.</summary>
    public static bool DeclaredTooLarge(HttpRequest request, long maxBodyBytes) =>
        request.ContentLength is long n && n > maxBodyBytes;

    /// <summary>Shundan kichik so'rovlar (matnli javob, qisqa shadowing gapi) joy kutmaydi — umumiy AI cheklovi yetarli.</summary>
    public const long SmallBodyBytes = 1 * Mb;

    /// <summary>"Katta so'rov" joyi: Dispose qilinganda bo'shaydi.</summary>
    public static async Task<IDisposable> EnterAsync(CancellationToken ct)
    {
        await Slots.WaitAsync(ct);
        return new Slot();
    }

    /// <summary>
    /// So'rov katta (yoki hajmi noma'lum) bo'lsa — joy oladi, kichik bo'lsa — null
    /// (<c>using var slot = await Uploads.EnterIfLargeAsync(...)</c> — null ham to'g'ri ishlaydi).
    /// </summary>
    public static async Task<IDisposable?> EnterIfLargeAsync(HttpRequest request, CancellationToken ct) =>
        request.ContentLength is long n && n <= SmallBodyBytes ? null : await EnterAsync(ct);

    /// <summary>Fayl hajmi aniq ma'lum — bufer bir marta, kerakli o'lchamda ajratiladi.</summary>
    public static async Task<byte[]> ReadAsync(IFormFile file, CancellationToken ct)
    {
        var bytes = new byte[file.Length];
        await using var stream = file.OpenReadStream();
        await stream.ReadExactlyAsync(bytes, ct);
        return bytes;
    }

    /// <summary>Endpoint uchun so'rov hajmi chegarasi (Kestrel umumiy chegarasini almashtiradi) va multipart chegarasi.</summary>
    public static RouteHandlerBuilder WithBodyLimit(this RouteHandlerBuilder builder, long maxBodyBytes) =>
        builder
            .WithMetadata(new Microsoft.AspNetCore.Mvc.RequestSizeLimitAttribute(maxBodyBytes))
            .WithFormOptions(multipartBodyLengthLimit: maxBodyBytes);

    /// <summary>Kestrel va forma o'qishning umumiy sozlamalari (Program.cs).</summary>
    public static void ConfigureDefaults(WebApplicationBuilder builder)
    {
        builder.WebHost.ConfigureKestrel(k => k.Limits.MaxRequestBodySize = DefaultBodyBytes);
        builder.Services.Configure<FormOptions>(o =>
        {
            // Eng katta ruxsat etilgan forma (mock speaking) — qolganlari endpoint'da kichikroq.
            o.MultipartBodyLengthLimit = 16 * Mb;
            o.ValueLengthLimit = 64 * 1024;
            o.ValueCountLimit = 256;
        });
    }

    private sealed class Slot : IDisposable
    {
        private int _disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) Slots.Release();
        }
    }
}
