using InnoShop.Users.Api.Domain.Authentication;
using InnoShop.Users.Api.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace InnoShop.Users.Api.Infrastructure.Persistence;

public sealed class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<OneTimeToken> Tokens => Set<OneTimeToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => item.Email).IsUnique();
            entity.Property(item => item.Name).HasMaxLength(100).IsRequired();
            entity.Property(item => item.Email).HasMaxLength(256).IsRequired();
            entity.Property(item => item.Role).HasMaxLength(30).IsRequired();
            entity.Property(item => item.PasswordHash).IsRequired();
            entity.Property(item => item.AuthVersion).IsConcurrencyToken();
            entity.Ignore(item => item.ProductsVisible);
        });
        modelBuilder.Entity<OneTimeToken>(entity =>
        {
            entity.HasKey(item => item.Id);
            entity.HasIndex(item => new { item.Hash, item.Purpose }).IsUnique();
            entity.Property(item => item.Hash).HasMaxLength(64).IsRequired();
            entity.Property(item => item.Purpose).HasMaxLength(30).IsRequired();
            entity.Property(item => item.ConsumedUtc).IsConcurrencyToken();
            entity.HasOne<UserAccount>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
