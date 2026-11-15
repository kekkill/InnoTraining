using InnoShop.Users.Api.Application.Abstractions;
using InnoShop.Users.Api.Application.Models;
using InnoShop.Users.Api.Application.Validation;
using InnoShop.Users.Api.Domain.Users;
using InnoShop.Users.Api.Infrastructure.Auth;
using InnoShop.Users.Api.Infrastructure.Mail;
using InnoShop.Users.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InnoShop.Users.Api.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddUsersInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddDbContext<UsersDbContext>(options =>
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

        services.Configure<MailOptions>(configuration.GetSection(MailOptions.SectionName));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddScoped<IJwtIssuer, JwtIssuer>();
        services.AddSingleton<IEmailSender, EmailSender>();

        return services;
    }

    public static async Task InitializeUsersDatabaseAsync(
        this IServiceProvider provider,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();

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

        var email = configuration["Bootstrap:AdminEmail"]?.Trim().ToLowerInvariant();
        var password = configuration["Bootstrap:AdminPassword"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (await db.Users.AnyAsync(user => user.Email == email))
        {
            return;
        }

        var request = new RegisterRequest("Administrator", email, password);
        if (!new RegisterValidator().Validate(request).IsValid)
        {
            throw new InvalidOperationException("invalid admin email or password");
        }

        var admin = UserAccount.CreateAdministrator(email);
        admin.SetPasswordHash(scope.ServiceProvider.GetRequiredService<IPasswordService>().Hash(admin, password));
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }
}
