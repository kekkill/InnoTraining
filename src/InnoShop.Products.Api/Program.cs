using InnoShop.Products.Api.Composition.Configuration;
using InnoShop.Products.Api.Composition;
using InnoShop.Products.Api.Endpoints;
using InnoShop.Products.Api.Infrastructure;
using InnoShop.Products.Api.Middleware;
using Scalar.AspNetCore;

namespace InnoShop.Products.Api;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddLocalDevelopment("Products", args);
        builder.Services.AddShopJwt(builder.Configuration);
        builder.Services.AddApplicationServices();
        builder.Services.AddProductsInfrastructure(builder.Configuration, builder.Environment);
        builder.Services.AddApiServices();
        builder.Services.AddOpenApi();
        builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
            policy.WithOrigins(builder.Configuration["Frontend:Origin"] ?? "http://localhost:5081")
                .AllowAnyHeader()
                .AllowAnyMethod()));

        var app = builder.Build();

        app.UseMiddleware<ApiExceptionHandler>();
        app.UseStatusCodePages();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<ActiveAccountMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.MapHealthEndpoints();
        app.MapProductEndpoints();

        await app.Services.InitializeProductsDatabaseAsync(app.Environment);
        await app.RunAsync();
    }
}
