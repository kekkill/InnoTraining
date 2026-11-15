using System.Security.Claims;
using InnoShop.Users.Api.Application.Models;
using InnoShop.Users.Api.Application.Services;
using InnoShop.Users.Api.Composition;

namespace InnoShop.Users.Api.Endpoints;

public static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users")
            .WithTags("Users")
            .RequireAuthorization();

        group.MapGet("/me", GetCurrentUser);
        group.MapGet("", ListUsers).RequireAuthorization(policy => policy.RequireRole("Admin"));
        group.MapGet("/{id:guid}", GetUser);
        group.MapPut("/{id:guid}", RenameUser);
        group.MapPost("/{id:guid}/deactivate", DeactivateUser);
        group.MapPost("/{id:guid}/activate", ActivateUser);
        group.MapPatch("/{id:guid}/role", ChangeRole);
        group.MapDelete("/{id:guid}", DeleteUser);

        return group;
    }

    private static Actor GetActor(ClaimsPrincipal user) => new(user.CurrentUserId(), user.IsInRole("Admin"));

    private static async Task<IResult> GetCurrentUser(
        ClaimsPrincipal user,
        UserService users)
    {
        var actor = GetActor(user);
        var response = await users.GetAsync(actor.UserId!.Value, actor);
        return Results.Ok(response);
    }

    private static async Task<IResult> ListUsers(
        ClaimsPrincipal user,
        UserService users,
        int page = 1,
        int pageSize = 20)
    {
        var response = await users.ListAsync(GetActor(user), page, pageSize);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetUser(
        Guid id,
        ClaimsPrincipal user,
        UserService users)
    {
        var response = await users.GetAsync(id, GetActor(user));
        return Results.Ok(response);
    }

    private static async Task<IResult> RenameUser(
        Guid id,
        ChangeNameRequest request,
        ClaimsPrincipal user,
        UserService users)
    {
        var response = await users.RenameAsync(id, request, GetActor(user));
        return Results.Ok(response);
    }

    private static async Task<IResult> DeactivateUser(
        Guid id,
        ClaimsPrincipal user,
        UserService users)
    {
        await users.SetActiveAsync(id, false, GetActor(user));
        return Results.NoContent();
    }

    private static async Task<IResult> ActivateUser(
        Guid id,
        ClaimsPrincipal user,
        UserService users)
    {
        await users.SetActiveAsync(id, true, GetActor(user));
        return Results.NoContent();
    }

    private static async Task<IResult> ChangeRole(
        Guid id,
        RoleRequest request,
        ClaimsPrincipal user,
        UserService users)
    {
        await users.SetRoleAsync(id, request, GetActor(user));
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteUser(
        Guid id,
        ClaimsPrincipal user,
        UserService users)
    {
        await users.DeleteAsync(id, GetActor(user));
        return Results.NoContent();
    }
}
