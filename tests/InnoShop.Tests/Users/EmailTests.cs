using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using InnoShop.Tests.Users.Fixtures;
using InnoShop.Users.Api.Application.Models;
using InnoShop.Users.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InnoShop.Tests.Users;

public sealed class EmailTests
{
    [Fact]
    public async Task MissingSmtpSettingsReturnErrorInsteadOfPretendingToSend()
    {
        using var factory = new UsersFactory { UseRealEmailSender = true };
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest("Alice", "alice@example.com", "ValidPassword123"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("not configured", problem.GetProperty("detail").GetString());
        Assert.Empty(factory.Emails.Messages);
    }

    [Fact]
    public async Task ExpiredConfirmationLinkDoesNotConfirmAccount()
    {
        using var factory = new UsersFactory();
        using var client = factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest("Alice", "alice@example.com", "ValidPassword123"));
        var raw = await factory.LastTokenAsync("Подтверждение");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
            var token = await db.Tokens.SingleAsync();
            Assert.NotEqual(raw, token.Hash);
            token.ExpiresUtc = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync("/api/v1/auth/confirm-email?token=" + raw)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.PostAsJsonAsync("/api/v1/auth/login",
                new LoginRequest("alice@example.com", "ValidPassword123"))).StatusCode);
    }
}
