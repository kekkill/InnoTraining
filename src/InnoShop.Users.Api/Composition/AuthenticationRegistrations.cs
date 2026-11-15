using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InnoShop.Users.Api.Composition.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace InnoShop.Users.Api.Composition;

public static class AuthenticationRegistrations
{
    public static IServiceCollection AddShopJwt(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<IntegrationOptions>()
            .Bind(configuration.GetSection(IntegrationOptions.SectionName))
            .Validate(options => Encoding.UTF8.GetByteCount(options.ApiKey) >= 24,
                "apikey must contain at least 24 bytes")
            .ValidateOnStart();

        services.AddOptions<AuthenticationOptions>()
            .Bind(configuration.GetSection("Jwt"))
            .Validate(settings => Encoding.UTF8.GetByteCount(settings.Key) >= 32,
                "jwt key must contain at least 32 bytes")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Issuer), "jwt issuer is required")
            .Validate(settings => !string.IsNullOrWhiteSpace(settings.Audience), "jwt audience is required")
            .ValidateOnStart();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                var jwt = configuration.GetSection("Jwt").Get<AuthenticationOptions>()!;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(15),
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                    RoleClaimType = "role"
                };
            });

        services.AddAuthorization();
        return services;
    }

    public static Guid? CurrentUserId(this ClaimsPrincipal user)
    {
        return Guid.TryParse(user.FindFirstValue(JwtRegisteredClaimNames.Sub), out var id)
            ? id
            : null;
    }
}
