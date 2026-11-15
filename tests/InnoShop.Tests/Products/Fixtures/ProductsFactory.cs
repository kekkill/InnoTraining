using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using InnoShop.Products.Api.Application.Abstractions;
using InnoShop.Products.Api.Infrastructure.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace InnoShop.Tests.Products.Fixtures;

public sealed class ProductsFactory : WebApplicationFactory<InnoShop.Products.Api.Program>
{
    public FakeUsers Users { get; } = new();
    public HttpClient? UsersApiClient { get; init; }

    private readonly string path = Path.Combine(Path.GetTempPath(), $"inno-products-{Guid.NewGuid():N}.db");
    protected override void ConfigureWebHost(IWebHostBuilder b)
    {
        b.UseEnvironment("Testing");
        b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Sqlite"] = $"Data Source={path};Pooling=False", ["Jwt:Key"] = "TestOnly_NotAProductionSecret_012345678901234567890123456789", ["Integration:ApiKey"] = "TestOnly_ServiceKey_012345678901234567890123456789" }));
        b.ConfigureTestServices(s =>
        {
            s.RemoveAll<IUserStateClient>();
            if (UsersApiClient is null)
            {
                s.AddSingleton<IUserStateClient>(Users);
            }
            else
            {
                UsersApiClient.DefaultRequestHeaders.Add("X-Service-Key",
                    "TestOnly_ServiceKey_012345678901234567890123456789");
                s.AddScoped<IUserStateClient>(_ => new UserStateClient(UsersApiClient));
            }
        });
    }

    public Guid SignIn(
        HttpClient c,
        Guid? id = null,
        string role = "User",
        int version = 1)
    {
        var owner = id ?? Guid.NewGuid();
        if (!Users.States.ContainsKey(owner))
        {
            Users.States[owner] = new(owner, true, version);
        }

        var config = Services.GetRequiredService<IConfiguration>();
        var token = new JwtSecurityToken(config["Jwt:Issuer"], config["Jwt:Audience"], [new Claim("sub", owner.ToString()), new Claim("role", role), new Claim("ver", version.ToString())], expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)), SecurityAlgorithms.HmacSha256));
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return owner;
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
