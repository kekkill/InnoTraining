using InnoShop.Products.Api.Application.Abstractions;
using InnoShop.Products.Api.Application.Models;
using InnoShop.Products.Api.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace InnoShop.Products.Api.Infrastructure.Persistence;

public sealed class ProductRepository(ProductsDbContext dbContext) : IProductRepository
{
    public Task<Product?> FindAsync(Guid id) => dbContext.Products.FirstOrDefaultAsync(product => product.Id == id);

    private IQueryable<Product> Filter(ProductSearch query)
    {
        var items = dbContext.Products.AsNoTracking().Where(product => !product.IsDeleted);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            items = items.Where(product => product.Name.ToLower().Contains(term) || product.Description.ToLower().Contains(term));
        }

        if (query.MinPrice.HasValue)
        {
            items = items.Where(product => product.Price >= query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            items = items.Where(product => product.Price <= query.MaxPrice.Value);
        }

        if (query.Available.HasValue)
        {
            items = items.Where(product => product.IsAvailable == query.Available.Value);
        }

        if (query.OwnerId.HasValue)
        {
            items = items.Where(product => product.OwnerUserId == query.OwnerId.Value);
        }

        return items;
    }

    public async Task<IReadOnlyList<Guid>> CandidateOwnersAsync(ProductSearch query)
    {
        return await Filter(query)
            .Select(product => product.OwnerUserId)
            .Distinct()
            .ToListAsync();
    }

    public async Task<ProductPage> SearchAsync(
        ProductSearch query,
        IReadOnlyList<Guid> owners)
    {
        var items = Filter(query).Where(product => owners.Contains(product.OwnerUserId));
        var count = await items.CountAsync();
        var sorted = query.Sort switch
        {
            "priceAsc" => items.OrderBy(product => product.Price).ThenBy(product => product.Id),
            "priceDesc" => items.OrderByDescending(product => product.Price).ThenBy(product => product.Id),
            _ => items.OrderByDescending(product => product.CreatedUtc).ThenBy(product => product.Id)
        };
        var page = await sorted
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();
        return new ProductPage(count, query.Page, query.PageSize, page.Select(ProductResponse.From).ToArray());
    }

    public async Task<IReadOnlyList<Product>> MineAsync(Guid ownerId)
    {
        return await dbContext.Products.AsNoTracking()
            .Where(product => product.OwnerUserId == ownerId && !product.IsDeleted)
            .OrderByDescending(product => product.CreatedUtc)
            .ToListAsync();
    }

    public void Add(Product product) => dbContext.Products.Add(product);
    public Task SaveAsync() => dbContext.SaveChangesAsync();
}
