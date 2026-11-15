using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using InnoShop.Users.Api.Application.Abstractions;
using InnoShop.Users.Api.Application.Exceptions;
using InnoShop.Users.Api.Application.Models;
using InnoShop.Users.Api.Domain.Authentication;
using InnoShop.Users.Api.Domain.Users;

namespace InnoShop.Users.Api.Application.Services;

public sealed class AuthenticationService(
    IUserRepository users,
    IPasswordService passwords,
    IJwtIssuer jwt,
    IEmailSender emails,
    IValidator<RegisterRequest> registerValidator,
    IValidator<ResetPasswordRequest> resetValidator)
{
    public async Task<Guid> RegisterAsync(RegisterRequest request)
    {
        await registerValidator.ValidateAndThrowAsync(request);
        var email = Normalize(request.Email);
        if (await users.EmailExistsAsync(email))
        {
            throw new AppException(409, "email is already registered");
        }

        var user = UserAccount.Create(request.Name, email);
        user.SetPasswordHash(passwords.Hash(user, request.Password));
        users.Add(user);
        var raw = IssueToken(user.Id, "confirm");
        await users.SaveAsync();
        await emails.SendConfirmationAsync(user.Email, raw);
        return user.Id;
    }

    public async Task ConfirmEmailAsync(string raw)
    {
        var token = await FindTokenAsync(raw, "confirm") ?? throw new AppException(400, "invalid or expired token");
        var user = await ExistingAsync(token.UserId);
        token.ConsumedUtc = DateTime.UtcNow;
        user.ConfirmEmail();
        await users.SaveAsync();
    }

    public async Task ResendConfirmationAsync(EmailRequest request)
    {
        var user = await users.FindByEmailAsync(Normalize(request.Email));
        if (user is null || user.IsDeleted || user.EmailConfirmed)
        {
            return;
        }

        var raw = IssueToken(user.Id, "confirm");
        await users.SaveAsync();
        await emails.SendConfirmationAsync(user.Email, raw);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Password) || request.Password.Length > 128)
        {
            throw new AppException(401, "invalid email or password");
        }

        var user = await users.FindByEmailAsync(Normalize(request.Email));
        if (user is null || !user.ProductsVisible || user.LockedUntilUtc > DateTime.UtcNow)
        {
            throw new AppException(401, "invalid login or inactive account");
        }

        if (!passwords.Verify(user, request.Password))
        {
            user.RecordFailedLogin();
            await users.SaveAsync();
            throw new AppException(401, "invalid login or inactive account");
        }

        user.RecordSuccessfulLogin();
        await users.SaveAsync();
        return new LoginResponse(jwt.Issue(user), 900);
    }

    public async Task ForgotPasswordAsync(EmailRequest request)
    {
        var user = await users.FindByEmailAsync(Normalize(request.Email));
        if (user is null || !user.ProductsVisible)
        {
            return;
        }

        var raw = IssueToken(user.Id, "reset");
        await users.SaveAsync();
        await emails.SendPasswordResetAsync(user.Email, raw);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        await resetValidator.ValidateAndThrowAsync(request);
        var token = await FindTokenAsync(request.Token, "reset") ?? throw new AppException(400, "invalid or expired token");
        var user = await ExistingAsync(token.UserId);
        if (!user.ProductsVisible)
        {
            throw new AppException(400, "account is unavailable");
        }

        user.ResetPassword(passwords.Hash(user, request.NewPassword));
        foreach (var item in await users.ActiveTokensAsync(user.Id, "reset"))
        {
            item.ConsumedUtc = DateTime.UtcNow;
        }

        await users.SaveAsync();
    }

    private async Task<UserAccount> ExistingAsync(Guid id)
    {
        var user = await users.GetAsync(id);
        if (user is null || user.IsDeleted)
        {
            throw new AppException(404, "user not found");
        }

        return user;
    }

    private static string Normalize(string? email) => (email ?? "").Trim().ToLowerInvariant();
    private static string Hash(string raw) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
    private string IssueToken(Guid id, string purpose)
    {
        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        users.Add(new OneTimeToken
        {
            UserId = id,
            Hash = Hash(raw),
            Purpose = purpose,
            ExpiresUtc = DateTime.UtcNow.AddHours(1)
        });
        return raw;
    }

    private Task<OneTimeToken?> FindTokenAsync(string raw, string purpose) =>
        raw is { Length: 64 } && raw.All(Uri.IsHexDigit)
            ? users.FindTokenAsync(Hash(raw), purpose)
            : Task.FromResult<OneTimeToken?>(null);
}
