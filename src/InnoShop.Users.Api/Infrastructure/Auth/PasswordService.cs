using InnoShop.Users.Api.Application.Abstractions;
using InnoShop.Users.Api.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace InnoShop.Users.Api.Infrastructure.Auth;

public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<UserAccount> hasher = new();
    public string Hash(UserAccount user, string password) => hasher.HashPassword(user, password);
    public bool Verify(UserAccount user, string password) => hasher.VerifyHashedPassword(user, user.PasswordHash, password) != PasswordVerificationResult.Failed;
}
