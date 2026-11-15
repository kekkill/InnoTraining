using System.Security.Claims;
using InnoShop.Products.Api.Application.Models;
using InnoShop.Products.Api.Application.Services;
using InnoShop.Products.Api.Composition;

namespace InnoShop.Products.Api.Endpoints;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/products").WithTags("Products");

        group.MapGet("", SearchProducts);
        group.MapGet("/mine", ListOwnProducts).RequireAuthorization();
        group.MapGet("/{id:guid}", GetProduct);
        group.MapPost("", CreateProduct).RequireAuthorization();
        group.MapPut("/{id:guid}", UpdateProduct).RequireAuthorization();
        group.MapDelete("/{id:guid}", DeleteProduct).RequireAuthorization();

        return group;
    }

    private static async Task<IResult> SearchProducts(
        [AsParameters] ProductSearch search,
        ProductService products)
    {
        var response = await products.SearchAsync(search);
        return Results.Ok(response);
    }

    private static async Task<IResult> ListOwnProducts(
        ClaimsPrincipal user,
        ProductService products)
    {
        var response = await products.MineAsync(user.CurrentUserId());
        return Results.Ok(response);
    }

    private static async Task<IResult> GetProduct(
        Guid id,
        ProductService products)
    {
        var response = await products.GetAsync(id);
        return Results.Ok(response);
    }

    private static async Task<IResult> CreateProduct(
        ProductWriteRequest request,
        ClaimsPrincipal user,
        ProductService products)
    {
        var response = await products.CreateAsync(request, user.CurrentUserId());
        return Results.Created($"/api/v1/products/{response.Id}", response);
    }

    private static async Task<IResult> UpdateProduct(
        Guid id,
        ProductWriteRequest request,
        ClaimsPrincipal user,
        ProductService products)
    {
        var response = await products.UpdateAsync(id, request, user.CurrentUserId());
        return Results.Ok(response);
    }

    private static async Task<IResult> DeleteProduct(
        Guid id,
        ClaimsPrincipal user,
        ProductService products)
    {
        await products.DeleteAsync(id, user.CurrentUserId());
        return Results.NoContent();
    }
}
