using FluentValidation;
using InnoShop.Products.Api.Application.Services;
using InnoShop.Products.Api.Application.Validation;

namespace InnoShop.Products.Api.Composition;

public static class ApplicationRegistrations
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ProductWriteValidator>();
        services.AddScoped<ProductService>();

        return services;
    }
}
