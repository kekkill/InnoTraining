using FluentValidation;
using InnoShop.Users.Api.Application.Models;

namespace InnoShop.Users.Api.Application.Validation;

public sealed class ChangeNameValidator : AbstractValidator<ChangeNameRequest>
{
    public ChangeNameValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
}
