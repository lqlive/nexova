using Nexova.Datasets.Models;
using Nexova.Extensions;

namespace Nexova.Datasets.Http;

public static class DatasetHttpEndpointsBuilder
{
    public static RouteGroupBuilder MapDatasetApiV1(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/datasets");

        api.MapGet("/", List);
        api.MapGet("/{id:guid}", Get);
        api.MapPost("/", Create)
            .AddRequestValidation<DatasetRequest>();
        api.MapPut("/{id:guid}", Update)
            .AddRequestValidation<DatasetRequest>();
        api.MapDelete("/{id:guid}", Delete);

        return api;
    }

    private static async Task<IResult> List(
        DatasetService service,
        CancellationToken cancellationToken)
    {
        var datasets = await service.ListAsync(cancellationToken);
        return Results.Ok(datasets);
    }

    private static async Task<IResult> Get(
        DatasetService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        var dataset = await service.GetAsync(id, cancellationToken);
        return dataset is null ? Results.NotFound() : Results.Ok(dataset);
    }

    private static async Task<IResult> Create(
        DatasetService service,
        DatasetRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return result.Match(
            dataset => Results.Created($"/api/datasets/{dataset.Id}", dataset),
            errors => errors.ToProblem());
    }

    private static async Task<IResult> Update(
        DatasetService service,
        Guid id,
        DatasetRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.UpdateAsync(id, request, cancellationToken);
        return result.Match(
            Results.Ok,
            errors => errors.ToProblem());
    }

    private static async Task<IResult> Delete(
        DatasetService service,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, cancellationToken);
        return result.Match(
            _ => Results.NoContent(),
            errors => errors.ToProblem());
    }
}