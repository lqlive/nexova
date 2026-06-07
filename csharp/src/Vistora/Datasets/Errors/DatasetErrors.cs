using ErrorOr;

namespace Vistora.Datasets.Errors;

public static class DatasetErrors
{
    public static Error NotFound => Error.NotFound(
        code: "Dataset.NotFound",
        description: "Dataset not found");

    public static Error NameRequired => Error.Validation(
        code: "Dataset.NameRequired",
        description: "Dataset name is required");

    public static Error NameTooLong => Error.Validation(
        code: "Dataset.NameTooLong",
        description: "Dataset name must not exceed 256 characters");

    public static Error SqlRequired => Error.Validation(
        code: "Dataset.SqlRequired",
        description: "Dataset SQL is required");

    public static Error DataSourcesRequired => Error.Validation(
        code: "Dataset.DataSourcesRequired",
        description: "Dataset data sources are required");

    public static Error DataSourcesEmpty => Error.Validation(
        code: "Dataset.DataSourcesEmpty",
        description: "At least one dataset data source must be provided");

    public static Error ColumnNameRequired => Error.Validation(
        code: "Dataset.ColumnNameRequired",
        description: "Dataset column name is required");

    public static Error ColumnNameTooLong => Error.Validation(
        code: "Dataset.ColumnNameTooLong",
        description: "Dataset column name must not exceed 256 characters");

    public static Error ColumnTypeRequired => Error.Validation(
        code: "Dataset.ColumnTypeRequired",
        description: "Dataset column type is required");

    public static Error ColumnTypeTooLong => Error.Validation(
        code: "Dataset.ColumnTypeTooLong",
        description: "Dataset column type must not exceed 128 characters");

    public static Error NameAlreadyExists => Error.Conflict(
        code: "Dataset.NameAlreadyExists",
        description: "A dataset with the same name already exists");
}
