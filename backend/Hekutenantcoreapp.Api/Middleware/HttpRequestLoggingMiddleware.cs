using Hekutenantcoreapp.Application.Interfaces;
using Hekutenantcoreapp.Domain.Enums;
using System.Diagnostics;
using LogLevel = Hekutenantcoreapp.Domain.Enums.LogLevel;

namespace Hekutenantcoreapp.Api.Middleware;

// Logs method/path/status/duration for every request under the Http category — deliberately
// metadata only, never Request.Body/Response.Body. That omission is what makes "no password ever
// reaches a log" structural rather than a matching rule (the destructuring-masking policy in
// Hekutenantcoreapp.Infrastructure/Logging is the separate safety net for the case something still
// gets destructured elsewhere). Wraps the whole pipeline so its duration and final status code
// (incl. a 401 from auth failing downstream) are both accurate.
public class HttpRequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public HttpRequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICategoryLogger categoryLogger)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var level = context.Response.StatusCode >= 500 ? LogLevel.Error
                : context.Response.StatusCode >= 400 ? LogLevel.Warning
                : LogLevel.Information;

            categoryLogger.Log(LogCategory.Http, level,
                $"{context.Request.Method} {context.Request.Path} responded {context.Response.StatusCode} in {stopwatch.ElapsedMilliseconds}ms");
        }
    }
}
