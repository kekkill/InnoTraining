using FluentValidation;
using InnoShop.Users.Api.Application.Services;
using InnoShop.Users.Api.Application.Validation;

namespace InnoShop.Users.Api.Composition;

public static class ApplicationRegistrations
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterValidator>();
        services.AddScoped<UserService>();
        services.AddScoped<AuthenticationService>();
        return services;
    }
}
