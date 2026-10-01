using System.Text.Json;

namespace TutorialPlatform.Api.Middleware;

/// <summary>
/// Captura cualquier excepción no controlada y devuelve un 500 con un cuerpo
/// seguro (sin stack trace ni detalles internos) + un id para correlacionar
/// con los logs del servidor.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
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

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var errorId = Guid.NewGuid().ToString("N")[..8];

        // El detalle completo solo va al log, nunca al cliente
        _logger.LogError(exception, "[{ErrorId}] Excepción no controlada en {Path}",
            errorId, context.Request.Path);

        if (context.Response.HasStarted)
        {
            _logger.LogWarning("[{ErrorId}] La respuesta ya había iniciado, no se puede modificar.", errorId);
            throw exception;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var payload = new Dictionary<string, object?>
        {
            ["status"] = 500,
            ["message"] = "Ocurrió un error interno. Inténtalo de nuevo.",
            ["errorId"] = errorId
        };

        // En Development sí se expone el detalle para facilitar la depuración
        if (_env.IsDevelopment())
        {
            payload["message"] = exception.Message;
            payload["detail"] = exception.StackTrace;
        }

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }
}
