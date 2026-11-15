using InnoShop.Products.Api.Application.Abstractions;
using InnoShop.Products.Api.Infrastructure.Http;
using InnoShop.Products.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InnoShop.Products.Api.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddProductsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddDbContext<ProductsDbContext>(options =>
        {
            var provider = environment.IsEnvironment("Testing")
                ? "Sqlite"
                : configuration["Database:Provider"] ?? "PostgreSql";

            if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                options.UseSqlite(configuration.GetConnectionString("Sqlite"));
            }
            else if (provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(configuration.GetConnectionString("PostgreSql")
                    ?? throw new InvalidOperationException("postgresql connection string is missing"));
            }
            else
            {
                throw new InvalidOperationException($"unknown database: {provider}");
            }
        });

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddHttpClient<IUserStateClient, UserStateClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Integration:UsersBaseUrl"]!);
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.Add("X-Service-Key", configuration["Integration:ApiKey"]);
        });

        return services;
    }

    public static async Task InitializeProductsDatabaseAsync(
        this IServiceProvider provider,
        IHostEnvironment environment)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductsDbContext>();

        if (db.Database.IsSqlite())
        {
            if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
            {
                throw new InvalidOperationException("sqlite is only available in development and tests");
            }

            await db.Database.EnsureCreatedAsync();
        }
        else
        {
            await db.Database.MigrateAsync();
        }
    }
}
