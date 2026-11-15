using System.Net.Http.Json;
using System.Net;
using InnoShop.Tests.Users.Fixtures;
using InnoShop.Users.Api.Application.Models;
using InnoShop.Users.Api.Application.Validation;
using InnoShop.Users.Api.Domain.Users;
using InnoShop.Users.Api.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InnoShop.Tests.Users;

public sealed class UsersTests
{
    [Fact]
    public void Unit_RegistrationRejectsWeakPassword() => Assert.False(new RegisterValidator().Validate(new RegisterRequest("A", "a@example.com", "123")).IsValid);

    [Fact]
    public void Unit_PasswordHashIsSaltedAndVerifiable()
    {
        var p = new PasswordService();
        var u = UserAccount.Create("Alice", "alice@example.com");
        u.SetPasswordHash(p.Hash(u, "ValidPassword123"));
        Assert.NotEqual("ValidPassword123", u.PasswordHash);
        Assert.True(p.Verify(u, "ValidPassword123"));
        Assert.False(p.Verify(u, "WrongPassword123"));
        Assert.NotEqual(u.PasswordHash, p.Hash(u, "ValidPassword123"));
    }

    [Fact]
    public async Task Integration_RegisterConfirmAndRejectDuplicate()
    {
        using var f = new UsersFactory();
        using var c = f.CreateClient();
        var r = new RegisterRequest("Alice", "Alice@Example.com", "ValidPassword123");
        Assert.Equal(HttpStatusCode.Created, (await c.PostAsJsonAsync("/api/v1/auth/register", r)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await c.PostAsJsonAsync("/api/v1/auth/register", r)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(r.Email, r.Password))).StatusCode);
        var token = await f.LastTokenAsync("Подтверждение");
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/v1/auth/confirm-email?token=" + token)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.GetAsync("/api/v1/auth/confirm-email?token=" + token)).StatusCode);
        await f.SignInAsync(c, "alice@example.com");
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync("/api/v1/users/me")).StatusCode);
    }

    [Fact]
    public async Task Integration_EmailFailureCanBeRecoveredByResendingConfirmation()
    {
        using var factory = new UsersFactory();
        using var client = factory.CreateClient();
        factory.Emails.Fails = true;
        var request = new RegisterRequest("Alice", "alice@example.com", "ValidPassword123");

        var failed = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        Assert.Empty(factory.Emails.Messages);
        Assert.Equal(HttpStatusCode.Conflict,
            (await client.PostAsJsonAsync("/api/v1/auth/register", request)).StatusCode);

        factory.Emails.Fails = false;
        var resend = await client.PostAsJsonAsync("/api/v1/auth/resend-confirmation", new EmailRequest(request.Email));
        Assert.Equal(HttpStatusCode.Accepted, resend.StatusCode);
        Assert.Single(factory.Emails.Messages);
        var token = await factory.LastTokenAsync("Подтверждение");
        Assert.Equal(HttpStatusCode.OK,
            (await client.GetAsync("/api/v1/auth/confirm-email?token=" + token)).StatusCode);
        await factory.SignInAsync(client);
    }

    [Fact]
    public async Task Integration_UserCanRenameSelfButCannotReadOrChangeOthers()
    {
        using var f = new UsersFactory();
        using var c = f.CreateClient();
        var id = await f.SeedAsync();
        var other = await f.SeedAsync("other@example.com");
        await f.SignInAsync(c);
        Assert.Equal(HttpStatusCode.OK, (await c.PutAsJsonAsync($"/api/v1/users/{id}", new ChangeNameRequest("New name"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync($"/api/v1/users/{other}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.GetAsync("/api/v1/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await c.DeleteAsync($"/api/v1/users/{other}")).StatusCode);
    }

    [Fact]
    public async Task Integration_AdminRoleChangesInvalidateOldToken()
    {
        using var f = new UsersFactory();
        using var admin = f.CreateClient();
        using var user = f.CreateClient();
        var id = await f.SeedAsync();
        await f.SeedAsync("admin@example.com", "Admin");
        await f.SignInAsync(user);
        await f.SignInAsync(admin, "admin@example.com");
        Assert.Equal(HttpStatusCode.NoContent, (await admin.PatchAsJsonAsync($"/api/v1/users/{id}/role", new RoleRequest("Admin"))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await user.GetAsync("/api/v1/users/me")).StatusCode);
        await f.SignInAsync(user);
        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/api/v1/users")).StatusCode);
    }

    [Fact]
    public async Task Integration_ResetIsSingleUseAndRevokesAccessToken()
    {
        using var f = new UsersFactory();
        using var c = f.CreateClient();
        await f.SeedAsync();
        await f.SignInAsync(c);
        await c.PostAsJsonAsync("/api/v1/auth/forgot-password", new EmailRequest("alice@example.com"));
        var reset = new ResetPasswordRequest(await f.LastTokenAsync("Сброс"), "NewValidPassword123");
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync("/api/v1/auth/reset-password", reset)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/v1/auth/reset-password", reset)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.GetAsync("/api/v1/users/me")).StatusCode);
        await f.SignInAsync(c, password: "NewValidPassword123");
    }

    [Fact]
    public async Task Integration_InternalStatusRequiresServiceKeyAndHidesSecrets()
    {
        using var f = new UsersFactory();
        using var c = f.CreateClient();
        var id = await f.SeedAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await c.PostAsJsonAsync("/api/v1/internal/users/status", new
        {
            ids = new[] { id }
        })).StatusCode);
        c.DefaultRequestHeaders.Add("X-Service-Key", f.Services.GetRequiredService<IConfiguration>()["Integration:ApiKey"]);
        var r = await c.PostAsJsonAsync("/api/v1/internal/users/status", new
        {
            ids = new[] { id }
        });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadAsStringAsync();
        Assert.Contains("canAccess", body);
        Assert.DoesNotContain("passwordHash", body);
        Assert.DoesNotContain("email", body);
    }
}
