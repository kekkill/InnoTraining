using System.Text.Json.Serialization;

namespace InnoShop.Users.Api.Composition;

public static class ApiRegistrations
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
            options.SerializerOptions.RespectRequiredConstructorParameters = true;
        });
        services.Configure<Microsoft.AspNetCore.Routing.RouteHandlerOptions>(options =>
            options.ThrowOnBadRequest = true);

        return services;
    }
}
