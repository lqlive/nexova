using ErrorOr;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Vistora.Extensions;

public static class ErrorOrExtensions
{
    public static ProblemHttpResult ToProblem(this List<Error> errors)
    {
        if (errors.Count is 0)
        {
            return TypedResults.Problem();
        }

        var validationErrors = errors.Where(error => error.Type == ErrorType.Validation).ToList();
        if (validationErrors.Count > 0)
        {
            return TypedResults.Problem(
                detail: string.Join("; ", validationErrors.Select(error => $"[{error.Code}] {error.Description}")),
                statusCode: StatusCodes.Status422UnprocessableEntity,
                extensions: new Dictionary<string, object?>
                {
                    ["errors"] = validationErrors
                        .GroupBy(error => error.Code.Split('.')[0])
                        .ToDictionary(
                            group => group.Key,
                            group => group.Select(error => $"[{error.Code}] {error.Description}").ToArray())
                });
        }

        var error = errors[0];
        var statusCode = error.Type switch
        {
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Unauthorized => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        return TypedResults.Problem(
            title: error.Code,
            detail: error.Description,
            statusCode: statusCode);
    }
}
