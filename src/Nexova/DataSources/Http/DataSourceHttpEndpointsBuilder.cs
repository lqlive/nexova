namespace Nexova.DataSources.Http;

public static class DataSourceHttpEndpointsBuilder
{
    public static RouteGroupBuilder MapDataSourceApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/datasources");
        api.MapGet("/", List);

        return api;
    }

    private static async Task<IResult> List(
        DataSourceService service,
        CancellationToken cancellationToken)
    {
        var dataSources = await service.ListAsync(cancellationToken);
        return Results.Ok(dataSources);
    }
}