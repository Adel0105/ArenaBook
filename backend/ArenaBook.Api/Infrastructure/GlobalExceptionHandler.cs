using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ArenaBook.Application.Common.Exceptions;

namespace ArenaBook.Api.Infrastructure;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IHostEnvironment environment, ILogger<GlobalExceptionHandler> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, detail, errors) = exception switch
        {
            NotFoundException notFound => (StatusCodes.Status404NotFound, "Nije pronađeno", notFound.Message, null),
            ConflictException conflict => (StatusCodes.Status409Conflict, "Konflikt", conflict.Message, null),
            ValidationException validation => (StatusCodes.Status400BadRequest, "Neispravan unos", validation.Message, validation.Errors),
            InvalidOperationException invalid => (StatusCodes.Status503ServiceUnavailable, "Servis nije spreman", invalid.Message, null),
            _ => (StatusCodes.Status500InternalServerError, "Interna greška servera", _environment.IsDevelopment() ? exception.Message : null, null),
        };

        if (status >= 500)
            _logger.LogError(exception, "Neobrađena greška");
        else
            _logger.LogWarning(exception, "Greška zahtjeva (HTTP {Status})", status);

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path.Value,
        };

        if (errors is not null)
            problem.Extensions["errors"] = errors;

        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}

