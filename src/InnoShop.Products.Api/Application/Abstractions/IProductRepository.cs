using InnoShop.Products.Api.Application.Models;
using InnoShop.Products.Api.Domain.Products;

namespace InnoShop.Products.Api.Application.Abstractions;

public interface IProductRepository
{
    Task<Product?> FindAsync(Guid id);
    Task<IReadOnlyList<Guid>> CandidateOwnersAsync(ProductSearch query);
    Task<ProductPage> SearchAsync(
        ProductSearch query,
        IReadOnlyList<Guid> visibleOwners);
    Task<IReadOnlyList<Product>> MineAsync(Guid ownerId);
    void Add(Product product);
    Task SaveAsync();
}
