using FluentValidation;
using InnoShop.Products.Api.Application.Models;

namespace InnoShop.Products.Api.Application.Validation;

public sealed class ProductWriteValidator : AbstractValidator<ProductWriteRequest>
{
    public ProductWriteValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(150);
        RuleFor(request => request.Description).NotNull().MaximumLength(3000);
        RuleFor(request => request.Price).GreaterThanOrEqualTo(0).PrecisionScale(12, 2, ignoreTrailingZeros: true);
    }
}

public sealed class ProductSearchValidator : AbstractValidator<ProductSearch>
{
    public ProductSearchValidator()
    {
        RuleFor(query => query.Page).InclusiveBetween(1, 100000);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Sort).Must(sort => sort is "newest" or "priceAsc" or "priceDesc");
        RuleFor(query => query.Search).MaximumLength(200);
        RuleFor(query => query.MinPrice).GreaterThanOrEqualTo(0).PrecisionScale(12, 2, ignoreTrailingZeros: true);
        RuleFor(query => query.MaxPrice)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(12, 2, ignoreTrailingZeros: true)
            .GreaterThanOrEqualTo(query => query.MinPrice)
            .WithMessage("max price must not be less than min price");
    }
}
