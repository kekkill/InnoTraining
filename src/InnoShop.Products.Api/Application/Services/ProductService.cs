using FluentValidation;
using InnoShop.Products.Api.Application.Abstractions;
using InnoShop.Products.Api.Application.Exceptions;
using InnoShop.Products.Api.Application.Models;
using InnoShop.Products.Api.Domain.Products;

namespace InnoShop.Products.Api.Application.Services;

public sealed class ProductService(
    IProductRepository products,
    IUserStateClient users,
    IValidator<ProductWriteRequest> writeValidator,
    IValidator<ProductSearch> searchValidator)
{
    public async Task<ProductPage> SearchAsync(ProductSearch query)
    {
        await searchValidator.ValidateAndThrowAsync(query);
        var candidates = await products.CandidateOwnersAsync(query);
        var ownerStates = await users.GetManyAsync(candidates);
        var owners = ownerStates.Where(owner => owner.CanAccess)
            .Select(owner => owner.Id)
            .ToArray();
        return await products.SearchAsync(query, owners);
    }

    public async Task<ProductResponse> GetAsync(Guid id)
    {
        var product = await products.FindAsync(id);
        if (product is null || product.IsDeleted)
        {
            throw new AppException(404, "product not found");
        }

        var owners = await users.GetManyAsync([product.OwnerUserId]);
        var owner = owners.SingleOrDefault();
        if (!product.IsVisible(owner?.CanAccess == true))
        {
            throw new AppException(404, "product not found");
        }

        return ProductResponse.From(product);
    }

    public async Task<IReadOnlyList<ProductResponse>> MineAsync(Guid? userId)
    {
        var id = RequireUser(userId);
        var items = await products.MineAsync(id);
        return items.Select(ProductResponse.From).ToArray();
    }

    public async Task<ProductResponse> CreateAsync(
        ProductWriteRequest request,
        Guid? userId)
    {
        var id = RequireUser(userId);
        await writeValidator.ValidateAndThrowAsync(request);
        var product = Product.Create(id, request.Name, request.Description, request.Price, request.IsAvailable);
        products.Add(product);
        await products.SaveAsync();
        return ProductResponse.From(product);
    }

    public async Task<ProductResponse> UpdateAsync(
        Guid id,
        ProductWriteRequest request,
        Guid? userId)
    {
        var user = RequireUser(userId);
        await writeValidator.ValidateAndThrowAsync(request);
        var product = await OwnedAsync(id, user);
        product.Update(request.Name, request.Description, request.Price, request.IsAvailable);
        await products.SaveAsync();
        return ProductResponse.From(product);
    }

    public async Task DeleteAsync(
        Guid id,
        Guid? userId)
    {
        var user = RequireUser(userId);
        var product = await OwnedAsync(id, user);
        product.Delete();
        await products.SaveAsync();
    }

    private static Guid RequireUser(Guid? user) => user ?? throw new AppException(401, "sign in required");

    private async Task<Product> OwnedAsync(
        Guid id,
        Guid owner)
    {
        var product = await products.FindAsync(id);
        if (product is null || product.IsDeleted)
        {
            throw new AppException(404, "product not found");
        }

        if (product.OwnerUserId != owner)
        {
            throw new AppException(403, "not your product");
        }

        return product;
    }
}
