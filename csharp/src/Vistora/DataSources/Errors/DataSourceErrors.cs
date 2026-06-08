using ErrorOr;

namespace Vistora.DataSources.Errors;

public static class DataSourceErrors
{
    public static Error NotFound => Error.NotFound(
        code: "DataSource.NotFound",
        description: "Data source not found");

    public static Error NameRequired => Error.Validation(
        code: "DataSource.NameRequired",
        description: "Data source name is required");

    public static Error NameTooLong => Error.Validation(
        code: "DataSource.NameTooLong",
        description: "Data source name must not exceed 256 characters");

    public static Error TypeRequired => Error.Validation(
        code: "DataSource.TypeRequired",
        description: "Data source type is required");

    public static Error TypeTooLong => Error.Validation(
        code: "DataSource.TypeTooLong",
        description: "Data source type must not exceed 64 characters");

    public static Error NameAlreadyExists => Error.Conflict(
        code: "DataSource.NameAlreadyExists",
        description: "A data source with the same name already exists");

    public static Error FileRequired => Error.Validation(
        code: "DataSource.FileRequired",
        description: "A file is required");

    public static Error FileEmpty => Error.Validation(
        code: "DataSource.FileEmpty",
        description: "The uploaded file is empty");

    public static Error FileAlreadyExists => Error.Conflict(
        code: "DataSource.FileAlreadyExists",
        description: "A different file already exists at the generated storage path");

    public static Error NotFileDataSource => Error.Validation(
        code: "DataSource.NotFileDataSource",
        description: "The data source is not a file data source");
}
