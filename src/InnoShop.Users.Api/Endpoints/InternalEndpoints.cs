using System.Security.Cryptography;
using System.Text;
using InnoShop.Users.Api.Application.Abstractions;

namespace InnoShop.Users.Api.Endpoints;

public static class InternalEndpoints
{
    public static RouteGroupBuilder MapInternalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/internal/users").WithTags("Internal");
        group.MapPost("/status", GetUserStates);
        return group;
    }

    private static async Task<IResult> GetUserStates(
        StatusRequest request,
        HttpContext context,
        IConfiguration configuration,
        IUserRepository users)
    {
        if (!HasServiceKey(context, configuration))
        {
            return Results.Unauthorized();
        }

        if (request.Ids is null)
        {
            return Results.Problem(statusCode: 400, title: "user ids are required");
        }

        var states = await users.GetManyAsync(request.Ids);
        return Results.Ok(states.Select(user => new
        {
            user.Id,
            CanAccess = user.ProductsVisible,
            user.AuthVersion
        }));
    }

    private static bool HasServiceKey(HttpContext context, IConfiguration configuration)
    {
        var expected = Encoding.UTF8.GetBytes(configuration["Integration:ApiKey"] ?? "");
        var received = Encoding.UTF8.GetBytes(context.Request.Headers["X-Service-Key"].ToString());

        return expected.Length >= 24
            && received.Length == expected.Length
            && CryptographicOperations.FixedTimeEquals(expected, received);
    }
}

public sealed record StatusRequest(Guid[] Ids);
