using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using InnoShop.Products.Api.Application.Models;
using InnoShop.Tests.Products.Fixtures;
using InnoShop.Tests.Users.Fixtures;
using Xunit;

namespace InnoShop.Tests.Products;

public sealed class UsersIntegrationTests
{
    [Fact]
    public async Task RealUsersApiHidesAndRestoresProductsAndRejectsRevokedTokens()
    {
        using var usersFactory = new UsersFactory();
        using var ownerClient = usersFactory.CreateClient();
        using var adminClient = usersFactory.CreateClient();
        var ownerId = await usersFactory.SeedAsync();
        await usersFactory.SeedAsync("admin@example.com", "Admin");
        var ownerToken = await usersFactory.SignInAsync(ownerClient);
        await usersFactory.SignInAsync(adminClient, "admin@example.com");

        using var productsFactory = new ProductsFactory { UsersApiClient = ownerClient };
        using var productsClient = productsFactory.CreateClient();
        productsClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

        var created = await productsClient.PostAsJsonAsync("/api/v1/products",
            new ProductWriteRequest("Keyboard", "Mechanical", 100m, true));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var product = (await created.Content.ReadFromJsonAsync<ProductResponse>())!;

        Assert.Equal(HttpStatusCode.NoContent,
            (await adminClient.PostAsync($"/api/v1/users/{ownerId}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await productsClient.GetAsync($"/api/v1/products/{product.Id}")).StatusCode);
        Assert.Equal(0, (await productsClient.GetFromJsonAsync<ProductPage>("/api/v1/products"))!.Total);

        Assert.Equal(HttpStatusCode.NoContent,
            (await adminClient.PostAsync($"/api/v1/users/{ownerId}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await productsClient.GetAsync($"/api/v1/products/{product.Id}")).StatusCode);
        Assert.Equal(1, (await productsClient.GetFromJsonAsync<ProductPage>("/api/v1/products"))!.Total);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await productsClient.DeleteAsync($"/api/v1/products/{product.Id}")).StatusCode);

        var freshToken = await usersFactory.SignInAsync(ownerClient);
        productsClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", freshToken);
        Assert.Equal(HttpStatusCode.NoContent,
            (await productsClient.DeleteAsync($"/api/v1/products/{product.Id}")).StatusCode);
    }
}
