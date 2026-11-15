using System.Net.Http.Json;
using System.Text.Json;
using InnoShop.Products.Api.Application.Abstractions;
using InnoShop.Products.Api.Application.Exceptions;
using InnoShop.Products.Api.Application.Models;

namespace InnoShop.Products.Api.Infrastructure.Http;

public sealed class UserStateClient(HttpClient http) : IUserStateClient
{
    public async Task<IReadOnlyList<UserAccessState>> GetManyAsync(IReadOnlyList<Guid> ids)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        try
        {
            using var response = await http.PostAsJsonAsync("api/v1/internal/users/status", new { ids });
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<UserAccessState[]>()
                ?? throw new HttpRequestException("invalid response from users service");
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            throw new AppException(503, "users service is unavailable");
        }
    }
}
