using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ToDoListAPI.Middlewares
{
    // Implementamos la interfaz nativa de .NET
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // 1. Logueamos el error (¡Fundamental para ti como desarrollador!)
            _logger.LogError(
                exception, "Ocurrió un error inesperado: {Message}", exception.Message);

            // 2. Definimos la respuesta estandarizada (ProblemDetails)
            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Error Interno del Servidor",
                Detail = "Ocurrió un problema inesperado. Por favor intenta más tarde."
                // Nota: En producción, NO debes devolver 'exception.Message' aquí 
                // para no exponer datos sensibles.
            };

            // 3. Personalizamos según el tipo de error (Opcional pero recomendado)
            // Aquí puedes capturar tus propias excepciones de dominio.
            if (exception is ArgumentException) // Ejemplo
            {
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Solicitud Incorrecta";
                problemDetails.Detail = exception.Message; // Aquí sí es seguro mostrar el mensaje
            }
            else if (exception is KeyNotFoundException)
            {
                problemDetails.Status = StatusCodes.Status404NotFound;
                problemDetails.Title = "Recurso no encontrado";
            }
            // (Puedes añadir más 'else if' para tus excepciones personalizadas)

            // 4. Escribimos la respuesta
            httpContext.Response.StatusCode = problemDetails.Status.Value;

            await httpContext.Response
                .WriteAsJsonAsync(problemDetails, cancellationToken);

            // Retornamos 'true' para decirle a .NET: "Yo ya manejé este error, no hagas nada más".
            return true;
        }
    }
}