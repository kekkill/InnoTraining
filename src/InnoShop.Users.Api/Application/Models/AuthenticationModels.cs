namespace InnoShop.Users.Api.Application.Models;

public sealed record RegisterRequest(
    string Name,
    string Email,
    string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record LoginResponse(string AccessToken, int ExpiresInSeconds);

public sealed record EmailRequest(string Email);

public sealed record ResetPasswordRequest(string Token, string NewPassword);
