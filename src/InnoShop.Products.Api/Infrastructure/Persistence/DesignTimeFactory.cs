using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore;

namespace InnoShop.Products.Api.Infrastructure.Persistence;

public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<ProductsDbContext>
{
    public ProductsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("products_connection")
            ?? ""; // подключение для команд EF

        var options = new DbContextOptionsBuilder<ProductsDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ProductsDbContext(options);
    }
}
