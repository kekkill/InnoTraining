using InnoShop.Users.Api.Application.Models;
using InnoShop.Users.Api.Application.Services;

namespace InnoShop.Users.Api.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Authentication");

        group.MapPost("/register", Register);
        group.MapPost("/login", Login);
        group.MapGet("/confirm-email", ConfirmEmail);
        group.MapPost("/resend-confirmation", ResendConfirmation);
        group.MapPost("/forgot-password", ForgotPassword);
        group.MapPost("/reset-password", ResetPassword);

        return group;
    }

    private static async Task<IResult> Register(
        RegisterRequest request,
        AuthenticationService authentication)
    {
        var id = await authentication.RegisterAsync(request);
        return Results.Created($"/api/v1/users/{id}", new
        {
            id,
            message = "check your email to confirm registration"
        });
    }

    private static async Task<IResult> Login(
        LoginRequest request,
        AuthenticationService authentication)
    {
        var response = await authentication.LoginAsync(request);
        return Results.Ok(response);
    }

    private static async Task<IResult> ConfirmEmail(
        string token,
        AuthenticationService authentication)
    {
        await authentication.ConfirmEmailAsync(token);
        return Results.Ok(new
        {
            message = "email confirmed"
        });
    }

    private static async Task<IResult> ResendConfirmation(
        EmailRequest request,
        AuthenticationService authentication)
    {
        await authentication.ResendConfirmationAsync(request);
        return Results.Accepted(value: new
        {
            message = "check your email"
        });
    }

    private static async Task<IResult> ForgotPassword(
        EmailRequest request,
        AuthenticationService authentication)
    {
        await authentication.ForgotPasswordAsync(request);
        return Results.Accepted(value: new
        {
            message = "check your email"
        });
    }

    private static async Task<IResult> ResetPassword(
        ResetPasswordRequest request,
        AuthenticationService authentication)
    {
        await authentication.ResetPasswordAsync(request);
        return Results.Ok(new
        {
            message = "password changed"
        });
    }
}
