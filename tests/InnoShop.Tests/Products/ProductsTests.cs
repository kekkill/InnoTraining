using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using InnoShop.Products.Api.Application.Models;
using InnoShop.Products.Api.Application.Validation;
using InnoShop.Products.Api.Domain.Products;
using InnoShop.Tests.Products.Fixtures;
using Xunit;

namespace InnoShop.Tests.Products;

public sealed class ProductsTests
{
    private static ProductWriteRequest Body(
        string name = "Toy",
        decimal price = 12.50m,
        bool available = true) => new(name, "Description", price, available);
    private static async Task<ProductResponse> Create(HttpClient c, ProductWriteRequest? body = null)
    {
        var r = await c.PostAsJsonAsync("/api/v1/products", body ?? Body());
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<ProductResponse>())!;
    }

    [Fact]
    public void Unit_RejectNegativePrice() => Assert.False(new ProductWriteValidator().Validate(Body(price: -1)).IsValid);

    [Fact]
    public void Unit_RejectMoreThanTwoDecimalPlaces() => Assert.False(new ProductWriteValidator().Validate(Body(price: 1.001m)).IsValid);

    [Fact]
    public void Unit_DeletedProductStaysHiddenAfterOwnerReactivation()
    {
        var p = Product.Create(Guid.NewGuid(), "Toy", "", 1, true);
        Assert.True(p.IsVisible(true));
        Assert.False(p.IsVisible(false));
        p.Delete();
        Assert.False(p.IsVisible(true));
    }

    [Fact]
    public async Task Integration_CrudOnlyOwnerIncludingAdmin()
    {
        using var f = new ProductsFactory();
        using var c = f.CreateClient();
        using var a = f.CreateClient();
        var owner = f.SignIn(c);
        f.SignIn(a, role: "Admin");
        var p = await Create(c);
        Assert.Equal(owner, p.OwnerUserId);
        Assert.Equal(HttpStatusCode.Forbidden, (await a.PutAsJsonAsync($"/api/v1/products/{p.Id}", Body())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await a.DeleteAsync($"/api/v1/products/{p.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync($"/api/v1/products/{p.Id}", Body("Updated"))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/v1/products/{p.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/v1/products/{p.Id}")).StatusCode);
    }

    [Fact]
    public async Task Integration_RejectOwnerInjectionAndMissingPrice()
    {
        using var f = new ProductsFactory();
        using var c = f.CreateClient();
        f.SignIn(c);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Toy",
            description = "",
            price = 1,
            isAvailable = true,
            ownerUserId = Guid.NewGuid()
        })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/v1/products", new
        {
            name = "Toy",
            description = "",
            isAvailable = true
        })).StatusCode);
    }

    [Fact]
    public async Task Integration_RequiresJwtAndRejectsInvalidSignature()
    {
        using var f = new ProductsFactory();
        using var c = f.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/v1/products", Body())).StatusCode);
        f.SignIn(c);
        var token = c.DefaultRequestHeaders.Authorization!.Parameter!;
        var parts = token.Split('.');
        parts[2] = (parts[2][0] == 'A' ? "B" : "A") + parts[2][1..];
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", string.Join('.', parts));
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/v1/products", Body())).StatusCode);
    }

    [Fact]
    public async Task Integration_SearchFiltersPagingAndTotals()
    {
        using var f = new ProductsFactory();
        using var c = f.CreateClient();
        f.SignIn(c);
        await Create(c, Body("Phone", 100));
        await Create(c, Body("Phone case", 10));
        await Create(c, Body("Phone broken", 5, false));
        await Create(c, Body("Book", 20));
        var page = await c.GetFromJsonAsync<ProductPage>("/api/v1/products?search=PHONE&minPrice=6&available=true&page=1&pageSize=1&sort=priceAsc");
        Assert.Equal(2, page!.Total);
        Assert.Single(page.Items);
        Assert.Equal("Phone case", page.Items[0].Name);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/v1/products?minPrice=20&maxPrice=10")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/v1/products?minPrice=1.001")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/v1/products", Body(price: decimal.MaxValue))).StatusCode);
    }

    [Fact]
    public async Task Integration_OwnDeletionIsNotUndoneByActivation()
    {
        using var f = new ProductsFactory();
        using var c = f.CreateClient();
        var owner = f.SignIn(c);
        var p = await Create(c);
        Assert.Equal(HttpStatusCode.NoContent, (await c.DeleteAsync($"/api/v1/products/{p.Id}")).StatusCode);
        f.Users.States[owner] = new(owner, false, 2);
        f.Users.States[owner] = new(owner, true, 3);
        Assert.Equal(0, (await c.GetFromJsonAsync<ProductPage>("/api/v1/products"))!.Total);
    }

    [Fact]
    public async Task Integration_UsersUnavailableReturns503()
    {
        using var f = new ProductsFactory();
        using var c = f.CreateClient();
        f.SignIn(c);
        await Create(c);
        f.Users.Offline = true;
        var r = await c.GetAsync("/api/v1/products");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, r.StatusCode);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType!.MediaType);
    }
}
