using InnoShop.Users.Api.Domain.Users;

namespace InnoShop.Users.Api.Application.Models;

public sealed record Actor(Guid? UserId, bool IsAdmin);

public sealed record ChangeNameRequest(string Name);

public sealed record RoleRequest(string Role);

public sealed record UserResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    bool EmailConfirmed,
    bool IsActive,
    DateTime CreatedUtc)
{
    public static UserResponse From(UserAccount user) => new(
        user.Id,
        user.Name,
        user.Email,
        user.Role,
        user.EmailConfirmed,
        user.IsActive,
        user.CreatedUtc);
}
