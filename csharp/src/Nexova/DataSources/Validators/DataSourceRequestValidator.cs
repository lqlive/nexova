using FluentValidation;
using Nexova.DataSources.Errors;
using Nexova.DataSources.Models;

namespace Nexova.DataSources.Validators;

public sealed class DataSourceRequestValidator : AbstractValidator<DataSourceRequest>
{
    public DataSourceRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .WithMessage(DataSourceErrors.NameRequired.Description)
            .WithErrorCode(DataSourceErrors.NameRequired.Code)
            .MaximumLength(256)
            .WithMessage(DataSourceErrors.NameTooLong.Description)
            .WithErrorCode(DataSourceErrors.NameTooLong.Code);

        RuleFor(request => request.Type)
            .NotEmpty()
            .WithMessage(DataSourceErrors.TypeRequired.Description)
            .WithErrorCode(DataSourceErrors.TypeRequired.Code)
            .MaximumLength(64)
            .WithMessage(DataSourceErrors.TypeTooLong.Description)
            .WithErrorCode(DataSourceErrors.TypeTooLong.Code);
    }
}
