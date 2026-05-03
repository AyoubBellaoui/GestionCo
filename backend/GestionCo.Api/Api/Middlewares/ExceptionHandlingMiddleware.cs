using GestionCo.Api.Application.Common.Exceptions;
using System.Net;
using System.Text.Json;

namespace GestionCo.Api.Api.Middlewares;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next; _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        var (code, message, errors) = ex switch
        {
            ValidationException v => (HttpStatusCode.BadRequest, "Erreurs de validation", (object?)v.Errors),
            NotFoundException n => (HttpStatusCode.NotFound, n.Message, null),
            BusinessException b => (HttpStatusCode.BadRequest, b.Message, null),
            UnauthorizedException u => (HttpStatusCode.Unauthorized, u.Message, null),
            _ => ((HttpStatusCode)500, "Une erreur interne est survenue", null)
        };

        if ((int)code >= 500)
            _logger.LogError(ex, "Erreur non gérée");
        else
            _logger.LogWarning("{Type}: {Message}", ex.GetType().Name, ex.Message);

        context.Response.StatusCode = (int)code;

        var response = JsonSerializer.Serialize(new
        {
            status = (int)code,
            message,
            errors,
            timestamp = DateTime.UtcNow
        }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(response);
    }
}
