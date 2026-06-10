using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace NajaEcho.Api.Common;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext ctx,
        Exception exception,
        CancellationToken ct)
    {
        logger.LogError(exception, "Unhandled exception on {Method} {Path}", ctx.Request.Method, ctx.Request.Path);

        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await ctx.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7807",
            Title = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
            Instance = ctx.Request.Path,
        }, ct);

        return true;
    }
}
