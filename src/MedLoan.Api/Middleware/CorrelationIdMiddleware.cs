using System.Diagnostics;

namespace MedLoan.Api.Middleware;

public class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        // Only a short id of safe characters is accepted, so a client cannot forge log lines.
        var incoming = context.Request.Headers[HeaderName].ToString();
        var correlationId = incoming.Length is > 0 and <= 64
                            && incoming.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
            ? incoming
            : Guid.NewGuid().ToString();

        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        using (logger.BeginScope(new Dictionary<string, object> { [ItemKey] = correlationId }))
        {
            var sw = Stopwatch.StartNew();
            logger.LogInformation("HTTP {Method} {Path} started", context.Request.Method, context.Request.Path);

            await next(context);

            logger.LogInformation("HTTP {Method} {Path} finished with {StatusCode} in {ElapsedMs} ms",
                context.Request.Method, context.Request.Path, context.Response.StatusCode, sw.ElapsedMilliseconds);
        }
    }
}
