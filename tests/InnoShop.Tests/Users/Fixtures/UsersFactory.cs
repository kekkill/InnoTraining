using System.Net.Http.Headers;
using System.Net.Http.Json;
using InnoShop.Users.Api.Application.Abstractions;
using InnoShop.Users.Api.Application.Models;
using InnoShop.Users.Api.Domain.Users;
using InnoShop.Users.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace InnoShop.Tests.Users.Fixtures;

public sealed class UsersFactory : WebApplicationFactory<InnoShop.Users.Api.Program>
{
    private readonly string path = Path.Combine(Path.GetTempPath(), $"inno-users-{Guid.NewGuid():N}.db");
    public FakeEmailSender Emails { get; } = new();
    public bool UseRealEmailSender { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Sqlite"] = $"Data Source={path};Pooling=False", ["Jwt:Key"] = "TestOnly_NotAProductionSecret_012345678901234567890123456789", ["Integration:ApiKey"] = "TestOnly_ServiceKey_012345678901234567890123456789" }));
        builder.ConfigureTestServices(services =>
        {
            if (!UseRealEmailSender)
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(Emails);
            }
        });
    }

    public async Task<Guid> SeedAsync(string email = "alice@example.com", string role = "User")
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        var u = UserAccount.Create("Alice", email);
        u.ConfirmEmail();
        u.SetRole(role);
        u.SetPasswordHash(scope.ServiceProvider.GetRequiredService<IPasswordService>().Hash(u, "ValidPassword123"));
        db.Users.Add(u);
        await db.SaveChangesAsync();
        return u.Id;
    }

    public async Task<string> SignInAsync(
        HttpClient client,
        string email = "alice@example.com",
        string password = "ValidPassword123")
    {
        using var r = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        r.EnsureSuccessStatusCode();
        var body = await r.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.AccessToken);
        return body.AccessToken;
    }

    public Task<string> LastTokenAsync(string subject)
    {
        var mail = Emails.Messages.Last(message => message.Subject.Contains(subject));
        return Task.FromResult(mail.Token);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
