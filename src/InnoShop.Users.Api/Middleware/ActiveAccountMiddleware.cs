using InnoShop.Users.Api.Application.Abstractions;
using InnoShop.Users.Api.Composition;
using Microsoft.AspNetCore.Authorization;

namespace InnoShop.Users.Api.Middleware;

public sealed class ActiveAccountMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.GetEndpoint()?.Metadata.GetOrderedMetadata<IAuthorizeData>().Count is not > 0 || context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        var id = context.User.CurrentUserId();
        if (id is null || !int.TryParse(context.User.FindFirst("ver")?.Value, out var version))
        {
            await Results.Problem(statusCode: 401, title: "invalid token").ExecuteAsync(context);
            return;
        }

        var account = await context.RequestServices.GetRequiredService<IUserRepository>().GetAsync(id.Value);
        var valid = account is { ProductsVisible: true } && account.AuthVersion == version;
        if (!valid)
        {
            await Results.Problem(statusCode: 401, title: "account or token is no longer valid, sign in again").ExecuteAsync(context);
            return;
        }

        await next(context);
    }
}
