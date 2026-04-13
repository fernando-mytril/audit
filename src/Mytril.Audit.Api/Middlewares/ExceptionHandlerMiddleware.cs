using System.Diagnostics;
using System.Text.Json;
using Mytril.Audit.Api.Responses;
using Mytril.Audit.Application.Diagnostics;
using Mytril.Audit.Domain.Exceptions;

namespace Mytril.Audit.Api.Middlewares;

public sealed class ExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlerMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex)
        {
            logger.LogWarning(ex, "Domain exception — Code: {Code}, Message: {Message}", ex.Code, ex.Message);
            await WriteDomainErrorAsync(context, ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            Activity.Current?.RecordException(ex);
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError,
                new ErrorResponse("INTERNAL_ERROR", "An unexpected error occurred."));
        }
    }

    private static Task WriteDomainErrorAsync(HttpContext context, DomainException ex)
    {
        var statusCode = ex switch
        {
            InvalidAuditEnvelopeException => StatusCodes.Status400BadRequest,
            DuplicateAuditEventException => StatusCodes.Status409Conflict,
            ForbiddenPayloadFieldException => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        return WriteErrorAsync(context, statusCode, new ErrorResponse(ex.Code, ex.Message));
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, ErrorResponse error)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(error, JsonOptions));
    }
}
