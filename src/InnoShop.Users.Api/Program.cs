using InnoShop.Users.Api.Composition.Configuration;
using InnoShop.Users.Api.Composition;
using InnoShop.Users.Api.Endpoints;
using InnoShop.Users.Api.Infrastructure;
using InnoShop.Users.Api.Middleware;
using Scalar.AspNetCore;

namespace InnoShop.Users.Api;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddLocalDevelopment("Users", args);
        builder.Services.AddShopJwt(builder.Configuration);
        builder.Services.AddApplicationServices();
        builder.Services.AddUsersInfrastructure(builder.Configuration, builder.Environment);
        builder.Services.AddApiServices();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        app.UseMiddleware<ApiExceptionHandler>();
        app.UseStatusCodePages();
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<ActiveAccountMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.MapHealthEndpoints();
        app.MapGet("/site-config", () => Results.Ok(new
        {
            productsApiUrl = app.Configuration["Frontend:ProductsApiUrl"] ?? "http://localhost:5082/api/v1"
        }));
        app.MapAuthEndpoints();
        app.MapUserEndpoints();
        app.MapInternalEndpoints();

        await app.Services.InitializeUsersDatabaseAsync(app.Environment, app.Configuration);
        await app.RunAsync();
    }
}
