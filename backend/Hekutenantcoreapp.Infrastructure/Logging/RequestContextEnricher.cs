using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;
using System.Security.Claims;

namespace Hekutenantcoreapp.Infrastructure.Logging;

// Tags every log event with TenantId/UserId/TraceId off the current HttpContext, mirroring
// HekutenantcoreappDbContext.CurrentTenantId's exact claim resolution — this is what makes the
// SuperAdmin log viewer's "filter by tenant" possible. TenantId is null (not 0) when unresolvable,
// since 0 is meaningful there as "no tenant" only in the DbContext's query-filter sense, not here.
public class RequestContextEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestContextEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null) return;

        if (int.TryParse(httpContext.User.FindFirstValue("tenant_id"), out var tenantId))
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TenantId", tenantId));

        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(userId))
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", userId));

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("TraceId", httpContext.TraceIdentifier));
    }
}
