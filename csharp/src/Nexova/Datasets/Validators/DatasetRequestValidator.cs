using FluentValidation;
using Nexova.Datasets.Errors;
using Nexova.Datasets.Models;

namespace Nexova.Datasets.Validators;

public sealed class DatasetRequestValidator : AbstractValidator<DatasetRequest>
{
    public DatasetRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithMessage(DatasetErrors.NameRequired.Description)
            .WithErrorCode(DatasetErrors.NameRequired.Code)
            .MaximumLength(256)
            .WithMessage(DatasetErrors.NameTooLong.Description)
            .WithErrorCode(DatasetErrors.NameTooLong.Code);

        RuleFor(request => request.Sql)
            .NotEmpty()
            .WithMessage(DatasetErrors.SqlRequired.Description)
            .WithErrorCode(DatasetErrors.SqlRequired.Code);

        RuleFor(request => request.DataSources)
            .NotNull()
            .WithMessage(DatasetErrors.DataSourcesRequired.Description)
            .WithErrorCode(DatasetErrors.DataSourcesRequired.Code)
            .Must(dataSources => dataSources is not null && dataSources.Any())
            .WithMessage(DatasetErrors.DataSourcesEmpty.Description)
            .WithErrorCode(DatasetErrors.DataSourcesEmpty.Code);

        RuleForEach(request => request.Columns)
            .SetValidator(new DatasetColumnRequestValidator())
            .When(request => request.Columns is not null);
    }
}

public sealed class DatasetColumnRequestValidator : AbstractValidator<DatasetColumnRequest>
{
    public DatasetColumnRequestValidator()
    {
        RuleFor(column => column.Name)
            .NotEmpty()
            .WithMessage(DatasetErrors.ColumnNameRequired.Description)
            .WithErrorCode(DatasetErrors.ColumnNameRequired.Code)
            .MaximumLength(256)
            .WithMessage(DatasetErrors.ColumnNameTooLong.Description)
            .WithErrorCode(DatasetErrors.ColumnNameTooLong.Code);

        RuleFor(column => column.Type)
            .NotEmpty()
            .WithMessage(DatasetErrors.ColumnTypeRequired.Description)
            .WithErrorCode(DatasetErrors.ColumnTypeRequired.Code)
            .MaximumLength(128)
            .WithMessage(DatasetErrors.ColumnTypeTooLong.Description)
            .WithErrorCode(DatasetErrors.ColumnTypeTooLong.Code);
    }
}
