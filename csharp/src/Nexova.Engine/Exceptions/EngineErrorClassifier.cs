using Apache.DataFusion;

namespace Nexova.Engine.Exceptions;

/// <summary>
/// Maps native failures onto the engine error taxonomy by dispatching on the typed
/// <see cref="DataFusionException"/> hierarchy. The binding only surfaces a coarse error
/// category as the concrete exception subtype (there is no error code on the exception),
/// so classification is purely type-based — no message string matching.
/// </summary>
public static class EngineErrorClassifier
{
    public static EngineException Classify(Exception exception) => exception switch
    {
        EngineException engine => engine,
        DataFusionPlanException ex => new(EngineErrorKind.InvalidSql, ex.Message, ex),
        DataFusionExecutionException ex => new(EngineErrorKind.QueryExecution, ex.Message, ex),
        DataFusionIoException ex => new(EngineErrorKind.FileSource, ex.Message, ex),
        DataFusionNotImplementedException ex => new(EngineErrorKind.UnsupportedDataSource, ex.Message, ex),
        DataFusionConfigurationException ex => new(EngineErrorKind.InvalidConnection, ex.Message, ex),
        DataFusionResourcesExhaustedException ex => new(EngineErrorKind.QueryExecution, ex.Message, ex),
        DataFusionException ex => new(EngineErrorKind.QueryExecution, ex.Message, ex),
        _ => new(EngineErrorKind.QueryExecution, exception.Message, exception),
    };

    /// <summary>
    /// Used only by the test-connection probe: any failure to reach the source is, by definition,
    /// a connection failure, so there is nothing to disambiguate from the message.
    /// </summary>
    public static EngineException ClassifyConnection(Exception exception) =>
        exception as EngineException
        ?? new EngineException(EngineErrorKind.InvalidConnection, exception.Message, exception);
}
