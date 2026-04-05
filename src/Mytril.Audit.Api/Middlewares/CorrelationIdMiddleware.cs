using Mytril.Audit.Api.Constants;
using Serilog.Context;

namespace Mytril.Audit.Api.Middlewares;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[ApiConstants.CorrelationIdHeader].FirstOrDefault()
                            ?? Guid.NewGuid().ToString();

        context.Response.Headers[ApiConstants.CorrelationIdHeader] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
