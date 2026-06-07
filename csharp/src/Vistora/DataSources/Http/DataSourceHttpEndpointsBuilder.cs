using Vistora.DataSources.Models;
using Vistora.Extensions;

namespace Vistora.DataSources.Http;

public static class DataSourceHttpEndpointsBuilder
{
    public static RouteGroupBuilder MapDataSourceApiV1(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/data-sources");

        api.MapGet("/", List);
        api.MapGet("/{id:guid}", Get);
        api.MapPost("/", Create)
            .AddRequestValidation<DataSourceRequest>();
        api.MapPut("/{id:guid}", Update)
            .AddRequestValidation<DataSourceRequest>();
        api.MapDelete("/{id:guid}", Delete);

        return api;
    }

    private static async Task<IResult> List(
        DataSourceService service,
        CancellationToken cancellationToken)
    {
        var dataSources = await service.ListAsync(cancellationToken);
        return Results.Ok(dataSources);
    }

    private static async Task<IResult> Get(
        DataSourceService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        var dataSource = await service.GetAsync(id, cancellationToken);
        return dataSource is null ? Results.NotFound() : Results.Ok(dataSource);
    }

    private static async Task<IResult> Create(
        DataSourceService service,
        DataSourceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.Match<IResult>(
            dataSource => Results.Created($"/api/data-sources/{dataSource.Id}", dataSource),
            errors => errors.ToProblem());
    }

    private static async Task<IResult> Update(
        DataSourceService service,
        Guid id,
        DataSourceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken);
        return result.Match<IResult>(
            Results.Ok,
            errors => errors.ToProblem());
    }

    private static async Task<IResult> Delete(
        DataSourceService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, cancellationToken);
        return result.Match<IResult>(
            _ => Results.NoContent(),
            errors => errors.ToProblem());
    }
}
