using InnoShop.Products.Api.Domain.Products;

namespace InnoShop.Products.Api.Application.Models;

public sealed record ProductWriteRequest(
    string Name,
    string Description,
    decimal Price,
    bool IsAvailable);

public sealed record ProductSearch(
    string? Search,
    decimal? MinPrice,
    decimal? MaxPrice,
    bool? Available,
    Guid? OwnerId,
    int Page = 1,
    int PageSize = 20,
    string Sort = "newest");

public sealed record ProductPage(
    int Total,
    int Page,
    int PageSize,
    IReadOnlyList<ProductResponse> Items);

public sealed record ProductResponse(
    Guid Id,
    Guid OwnerUserId,
    string Name,
    string Description,
    decimal Price,
    bool IsAvailable,
    DateTime CreatedUtc,
    DateTime UpdatedUtc)
{
    public static ProductResponse From(Product product) => new(
        product.Id,
        product.OwnerUserId,
        product.Name,
        product.Description,
        product.Price,
        product.IsAvailable,
        product.CreatedUtc,
        product.UpdatedUtc);
}
