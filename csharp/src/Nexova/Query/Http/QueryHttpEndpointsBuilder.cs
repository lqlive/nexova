using Nexova.Engine;
using Nexova.Engine.Contracts;

namespace Nexova.Query.Http;

/// <summary>
/// Exposes the in-process query engine over HTTP. Requests carry the data source connection inline
/// (a faithful, stateless port of the Rust engine's API), and all engine failures are mapped to
/// ProblemDetails by <see cref="EngineExceptionFilter"/>.
/// </summary>
public static class QueryHttpEndpointsBuilder
{
    public static RouteGroupBuilder MapQueryApiV1(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/query");
        api.AddEndpointFilter<EngineExceptionFilter>();

        api.MapPost("/", Query);
        api.MapPost("/explain", Explain);
        api.MapPost("/federated", FederatedQuery);
        api.MapPost("/federated/explain", FederatedExplain);
        api.MapPost("/schema/tables", ListTables);
        api.MapPost("/schema/columns", ListColumns);
        api.MapPost("/test-connection", TestConnection);

        return api;
    }

    private static async Task<IResult> Query(
        IQueryEngine engine,
        QueryRequest request,
        CancellationToken cancellationToken)
        => Results.Ok(await engine.QueryAsync(request, cancellationToken));

    private static async Task<IResult> Explain(
        IQueryEngine engine,
        QueryRequest request,
        CancellationToken cancellationToken)
        => Results.Ok(await engine.ExplainAsync(request, cancellationToken));

    private static async Task<IResult> FederatedQuery(
        IQueryEngine engine,
        FederatedQueryRequest request,
        CancellationToken cancellationToken)
        => Results.Ok(await engine.FederatedQueryAsync(request, cancellationToken));

    private static async Task<IResult> FederatedExplain(
        IQueryEngine engine,
        FederatedQueryRequest request,
        CancellationToken cancellationToken)
        => Results.Ok(await engine.ExplainFederatedAsync(request, cancellationToken));

    private static async Task<IResult> ListTables(
        IQueryEngine engine,
        DataSourceRequest request,
        CancellationToken cancellationToken)
        => Results.Ok(await engine.ListTablesAsync(request.DataSource, cancellationToken));

    private static async Task<IResult> ListColumns(
        IQueryEngine engine,
        SchemaColumnsRequest request,
        CancellationToken cancellationToken)
        => Results.Ok(await engine.ListColumnsAsync(request.DataSource, request.Schema, request.Table, cancellationToken));

    private static async Task<IResult> TestConnection(
        IQueryEngine engine,
        DataSourceRequest request,
        CancellationToken cancellationToken)
    {
        await engine.TestConnectionAsync(request.DataSource, cancellationToken);
        return Results.Ok(new { ok = true });
    }
}
