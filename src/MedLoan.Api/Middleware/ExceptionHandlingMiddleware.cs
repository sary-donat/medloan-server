using System.Text.Json;
using MedLoan.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace MedLoan.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // This middleware runs before CorrelationIdMiddleware, so its scope is already closed here.
            var correlationId = context.Items[CorrelationIdMiddleware.ItemKey]?.ToString() ?? "-";
            using var scope = logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId });

            var (status, title) = ex switch
            {
                OperationCanceledException when context.RequestAborted.IsCancellationRequested => (499, "Client Closed Request"),
                DomainException => (StatusCodes.Status409Conflict, "Conflict"),
                _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
            };

            if (status == StatusCodes.Status500InternalServerError)
                logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);
            else
                logger.LogWarning("Handled {ExceptionType} on {Method} {Path}: {Status}",
                    ex.GetType().Name, context.Request.Method, context.Request.Path, status);

            if (context.Response.HasStarted) return;

            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = status,
                Title = title,
                // ex.Message is never sent on a 500: it may expose internals.
                Detail = status == StatusCodes.Status500InternalServerError
                    ? "אירעה שגיאה בשרת. נא לפנות לתמיכה עם מספר המזהה."
                    : ex.Message,
                Instance = context.Request.Path,
                Extensions = { ["correlationId"] = correlationId }
            }, (JsonSerializerOptions?)null, "application/problem+json");
        }
    }
}
