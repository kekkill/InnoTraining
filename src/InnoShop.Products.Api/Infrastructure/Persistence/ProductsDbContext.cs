using InnoShop.Products.Api.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace InnoShop.Products.Api.Infrastructure.Persistence;

public sealed class ProductsDbContext(DbContextOptions<ProductsDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.OwnerUserId, item.IsDeleted });
            entity.HasIndex(item => item.Price);
            entity.Property(item => item.Name).HasMaxLength(150).IsRequired();
            entity.Property(item => item.Description).HasMaxLength(3000).IsRequired();
            // Integer cents are exact and support the SQLite integration-test provider too.
            entity.Property(item => item.Price).HasConversion(v => checked((long)(v * 100m)), v => v / 100m);
        });
    }
}
