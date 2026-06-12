using Apache.DataFusion;

namespace Nexova.Engine.Exceptions;

/// <summary>
/// Maps native <see cref="DataFusionException"/> messages (and provider failures) onto the
/// engine error taxonomy. Mirrors the keyword classification in the Rust engine's <c>error.rs</c>.
/// </summary>
public static class EngineErrorClassifier
{
    public static EngineException Classify(Exception exception)
    {
        if (exception is EngineException engineException)
        {
            return engineException;
        }

        return ClassifyQueryMessage(exception.Message, exception);
    }

    public static EngineException ClassifyProvider(string context, Exception exception)
    {
        var message = $"{context}: {exception.Message}";
        var lower = message.ToLowerInvariant();

        if (ContainsAny(lower, "connect", "connection", "pool", "authentication", "password", "login", "tls"))
        {
            return new EngineException(EngineErrorKind.InvalidConnection, message, exception);
        }

        return ClassifyQueryMessage(message, exception);
    }

    public static EngineException ClassifyQueryMessage(string message, Exception? inner = null)
    {
        var lower = message.ToLowerInvariant();

        if (ContainsAny(lower, "table not found", "no table named", "failed to resolve table"))
        {
            return new EngineException(EngineErrorKind.TableNotFound, message, inner);
        }

        if (ContainsAny(lower, "field not found", "no field named", "column not found", "no column named"))
        {
            return new EngineException(EngineErrorKind.ColumnNotFound, message, inner);
        }

        if (ContainsAny(lower, "syntax error", "parser error", "sql parser"))
        {
            return new EngineException(EngineErrorKind.SqlSyntax, message, inner);
        }

        if (ContainsAny(lower, "permission denied", "access denied", "not authorized", "read-only", "readonly"))
        {
            return new EngineException(EngineErrorKind.ReadOnlyViolation, message, inner);
        }

        return new EngineException(EngineErrorKind.QueryExecution, message, inner);
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        foreach (var needle in needles)
        {
            if (value.Contains(needle, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
