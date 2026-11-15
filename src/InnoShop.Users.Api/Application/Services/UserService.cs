using FluentValidation;
using InnoShop.Users.Api.Application.Abstractions;
using InnoShop.Users.Api.Application.Exceptions;
using InnoShop.Users.Api.Application.Models;
using InnoShop.Users.Api.Domain.Users;

namespace InnoShop.Users.Api.Application.Services;

public sealed class UserService(
    IUserRepository users,
    IValidator<ChangeNameRequest> nameValidator)
{
    public async Task<UserResponse> GetAsync(
        Guid id,
        Actor actor)
    {
        RequireOwnerOrAdmin(id, actor);
        return UserResponse.From(await ExistingAsync(id));
    }

    public async Task<IReadOnlyList<UserResponse>> ListAsync(
        Actor actor,
        int page,
        int pageSize)
    {
        if (!actor.IsAdmin)
        {
            throw new AppException(403, "admin access required");
        }

        if (page < 1 || page > 100000 || pageSize < 1 || pageSize > 100)
        {
            throw new AppException(400, "invalid page or page size");
        }

        return (await users.ListAsync(page, pageSize)).Select(UserResponse.From).ToArray();
    }

    public async Task<UserResponse> RenameAsync(
        Guid id,
        ChangeNameRequest request,
        Actor actor)
    {
        RequireOwnerOrAdmin(id, actor);
        await nameValidator.ValidateAndThrowAsync(request);
        var user = await ExistingAsync(id);
        user.Rename(request.Name);
        await users.SaveAsync();
        return UserResponse.From(user);
    }

    public async Task SetActiveAsync(
        Guid id,
        bool active,
        Actor actor)
    {
        RequireOwnerOrAdmin(id, actor);
        if (active && !actor.IsAdmin)
        {
            throw new AppException(403, "admin access required");
        }

        if (!active && actor.IsAdmin && actor.UserId == id)
        {
            throw new AppException(409, "you cannot deactivate your own admin account");
        }

        var user = await ExistingAsync(id);
        if (user.IsActive != active)
        {
            user.SetActive(active);
            await users.SaveAsync();
        }
    }

    public async Task SetRoleAsync(
        Guid id,
        RoleRequest request,
        Actor actor)
    {
        if (!actor.IsAdmin)
        {
            throw new AppException(403, "admin access required");
        }

        if (request.Role is not ("Admin" or "User"))
        {
            throw new AppException(400, "unknown role");
        }

        if (actor.UserId == id && request.Role != "Admin")
        {
            throw new AppException(409, "you cannot remove your own admin role");
        }

        var user = await ExistingAsync(id);
        if (user.Role != request.Role)
        {
            user.SetRole(request.Role);
            await users.SaveAsync();
        }
    }

    public async Task DeleteAsync(
        Guid id,
        Actor actor)
    {
        RequireOwnerOrAdmin(id, actor);
        if (actor.IsAdmin && actor.UserId == id)
        {
            throw new AppException(409, "you cannot delete your own admin account");
        }

        var user = await ExistingAsync(id);
        user.Delete();
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

    private static void RequireOwnerOrAdmin(Guid id, Actor actor)
    {
        if (actor.UserId is null)
        {
            throw new AppException(401, "sign in required");
        }

        if (!actor.IsAdmin && actor.UserId != id)
        {
            throw new AppException(403, "access denied");
        }
    }

}
