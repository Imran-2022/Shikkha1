using System.Text.Json;
using SchoolAdmission.Application.Common.Exceptions;
using SchoolAdmission.Domain.Exceptions;
using ValidationException = SchoolAdmission.Application.Common.Exceptions.ValidationException;

namespace SchoolAdmission.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail, errors) = exception switch
        {
            ValidationException ve => (
                StatusCodes.Status422UnprocessableEntity,
                "Validation Failed",
                "One or more validation errors occurred.",
                ve.Errors),

            NotFoundException nfe => (
                StatusCodes.Status404NotFound,
                "Not Found",
                nfe.Message,
                (IDictionary<string, string[]>?)null),

            ConflictException ce => (
                StatusCodes.Status409Conflict,
                "Conflict",
                ce.Message,
                (IDictionary<string, string[]>?)null),

            ForbiddenException fe => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                fe.Message,
                (IDictionary<string, string[]>?)null),

            NoSeatsAvailableException nsae => (
                StatusCodes.Status409Conflict,
                "No Seats Available",
                nsae.Message,
                (IDictionary<string, string[]>?)null),

            DuplicateActiveHoldException dahe => (
                StatusCodes.Status409Conflict,
                "Duplicate Active Hold",
                dahe.Message,
                (IDictionary<string, string[]>?)null),

            HoldExpiredException hee => (
                StatusCodes.Status409Conflict,
                "Hold Expired",
                hee.Message,
                (IDictionary<string, string[]>?)null),

            HoldNotCancellableException hnce => (
                StatusCodes.Status409Conflict,
                "Hold Cannot Be Cancelled",
                hnce.Message,
                (IDictionary<string, string[]>?)null),

            UnauthorizedHoldActionException uhae => (
                StatusCodes.Status403Forbidden,
                "Forbidden",
                uhae.Message,
                (IDictionary<string, string[]>?)null),

            DomainException de => (
                StatusCodes.Status400BadRequest,
                "Business Rule Violation",
                de.Message,
                (IDictionary<string, string[]>?)null),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred. Please try again later.",
                (IDictionary<string, string[]>?)null)
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        else
            _logger.LogWarning(exception, "Handled exception [{Status}]: {Message}", statusCode, exception.Message);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new
        {
            type = $"https://httpstatuses.io/{statusCode}",
            title,
            status = statusCode,
            detail,
            errors,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails, JsonOptions));
    }
}
