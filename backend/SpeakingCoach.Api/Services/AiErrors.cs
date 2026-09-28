namespace SpeakingCoach.Api.Services;

/// <summary>
/// AI endpoint'lari uchun umumiy xato qoidalari. "AI band" (AiBusyException)
/// endpoint'da ushlanmaydi — markaziy middleware (UseAiErrors) hamma joyda bir
/// xil javob beradi: 503, { error, code: "ai_busy", retryAfterSeconds } va
/// Retry-After sarlavhasi. Qolgan AI xatolari — avvalgidek 502 "ai_unavailable".
/// </summary>
public static class AiErrors
{
    public const string BusyCode = "ai_busy";

    /// <summary>
    /// catch filtri: AI xatosi shu endpoint'da 502 ga aylantirilsinmi.
    /// AiBusyException — yo'q (middleware 503 qiladi); foydalanuvchi so'rovni
    /// o'zi bekor qilgan bo'lsa — ham yo'q.
    /// </summary>
    public static bool Handles(Exception ex, HttpRequest request) =>
        ex is not AiBusyException
        && (ex is not OperationCanceledException || !request.HttpContext.RequestAborted.IsCancellationRequested);

    public static object BusyBody(HttpRequest request, int retryAfterSeconds) =>
        new { error = request.T(BusyCode), code = BusyCode, retryAfterSeconds };

    /// <summary>Endpoint o'zi javob qaytarishi kerak bo'lganda (masalan, oqim ichida).</summary>
    public static IResult Busy(HttpRequest request, AiBusyException ex)
    {
        var seconds = ex.RetryAfterSeconds(DateTime.UtcNow);
        request.HttpContext.Response.Headers.RetryAfter = seconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return Results.Json(BusyBody(request, seconds), statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    /// <summary>
    /// Markaziy middleware: AiBusyException → 503 + Retry-After; hajmi katta so'rov
    /// (Kestrel/forma chegarasi) → 413 JSON. CORS'dan keyin ulanadi — sarlavhalar saqlanadi.
    /// </summary>
    public static IApplicationBuilder UseAiErrors(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        try
        {
            await next(context);
        }
        catch (AiBusyException ex) when (!context.Response.HasStarted)
        {
            context.RequestServices.GetService<ILoggerFactory>()?.CreateLogger("AiBusy")
                .LogWarning("AI band ({Reason}): {Path}", ex.Reason, context.Request.Path.Value);
            var seconds = ex.RetryAfterSeconds(DateTime.UtcNow);
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.Headers.RetryAfter = seconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
            await context.Response.WriteAsJsonAsync(BusyBody(context.Request, seconds));
        }
        catch (BadHttpRequestException ex) when (ex.StatusCode == StatusCodes.Status413PayloadTooLarge && !context.Response.HasStarted)
        {
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await context.Response.WriteAsJsonAsync(context.Request.Error("request.too_large"));
        }
        catch (InvalidDataException ex) when (!context.Response.HasStarted && context.Request.HasFormContentType
            && ex.Message.Contains("limit", StringComparison.OrdinalIgnoreCase))
        {
            // Forma chegarasi (MultipartBodyLengthLimit va h.k.) oshdi.
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
            await context.Response.WriteAsJsonAsync(context.Request.Error("request.too_large"));
        }
    });
}
