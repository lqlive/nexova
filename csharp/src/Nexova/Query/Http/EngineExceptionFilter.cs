using Nexova.Engine.Exceptions;

namespace Nexova.Query.Http;

/// <summary>
/// Translates <see cref="EngineException"/> thrown by the query engine into a ProblemDetails
/// response, preserving the engine's stable error contract (code/category/detail/status).
/// </summary>
public sealed class EngineExceptionFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (EngineException exception)
        {
            return Results.Problem(
                title: exception.Code,
                detail: exception.Detail,
                statusCode: (int)exception.StatusCode,
                extensions: new Dictionary<string, object?>
                {
                    ["category"] = exception.Category,
                    ["userMessage"] = exception.UserMessage,
                });
        }
    }
}
