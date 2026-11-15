using InnoShop.Users.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InnoShop.Users.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
        app.MapGet("/health/ready", Ready);
        return app;
    }

    private static async Task<IResult> Ready(UsersDbContext db)
    {
        return await db.Database.CanConnectAsync()
            ? Results.Ok()
            : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
}
