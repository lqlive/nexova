using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using Nexova.Core.Storage;
using Nexova.DataSources.Errors;
using Nexova.DataSources.Models;
using Nexova.Extensions;

namespace Nexova.DataSources.Http;

public static class DataSourceHttpEndpointsBuilder
{
    public static RouteGroupBuilder MapDataSourceApiV1(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/datasources");

        api.MapGet("/", List);
        api.MapGet("/{id:guid}", Get);
        api.MapPost("/", Create)
            .AddRequestValidation<DataSourceRequest>();
        api.MapPost("/upload", UploadFile)
            .DisableAntiforgery();
        api.MapPost("/{id:guid}/files", AddDataSourceFile);
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
        IStorageService storage,
        DataSourceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, storage, cancellationToken);
        return result.Match<IResult>(
            dataSource => Results.Created($"/api/datasources/{dataSource.Id}", dataSource),
            errors => errors.ToProblem());
    }

    private static async Task<IResult> UploadFile(
        IStorageService storage,
        [FromForm] IFormFile? file,
        [FromForm] string? storageDirectory,
        CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return ListOf(DataSourceErrors.FileRequired).ToProblem();
        }

        if (file.Length <= 0)
        {
            return ListOf(DataSourceErrors.FileEmpty).ToProblem();
        }

        var uploadId = Guid.NewGuid();
        var safeFileName = SanitizeFileName(file.FileName);
        var directory = string.IsNullOrWhiteSpace(storageDirectory)
            ? $"data-sources/{uploadId:N}"
            : storageDirectory.Trim().TrimEnd('/', '\\');
        var storagePath = $"{directory}/{safeFileName}";
        var contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;

        await using var stream = file.OpenReadStream();
        var putResult = await storage.PutAsync(
            storagePath,
            stream,
            contentType,
            cancellationToken);

        if (putResult is StoragePutResult.Conflict)
        {
            return ListOf(DataSourceErrors.FileAlreadyExists).ToProblem();
        }

        var downloadUri = await storage.GetDownloadUriAsync(storagePath, cancellationToken);
        var path = downloadUri.IsFile ? downloadUri.LocalPath : downloadUri.ToString();
        var response = new FileUploadResponse(
            safeFileName,
            contentType,
            file.Length,
            storagePath,
            path);

        return Results.Ok(response);
    }

    private static async Task<IResult> AddDataSourceFile(
        DataSourceService service,
        Guid id,
        AddDataSourceFileRequest request,
        CancellationToken cancellationToken)
    {
        var addFileResult = await service.AddFileAsync(
            id,
            request.FileName,
            request.StoragePath,
            request.Path,
            request.ContentType,
            request.Size,
            request.HasHeader,
            request.Delimiter,
            request.Sheet,
            cancellationToken);

        return addFileResult.Match<IResult>(
            Results.Ok,
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

    private static List<Error> ListOf(Error error)
        => [error];

    private static string SanitizeFileName(string fileName)
    {
        var sanitizedFileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(sanitizedFileName))
        {
            return "upload";
        }

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            sanitizedFileName = sanitizedFileName.Replace(invalidChar, '_');
        }

        return sanitizedFileName;
    }
}
