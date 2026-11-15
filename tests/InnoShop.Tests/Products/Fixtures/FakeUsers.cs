using InnoShop.Products.Api.Application.Abstractions;
using InnoShop.Products.Api.Application.Exceptions;
using InnoShop.Products.Api.Application.Models;

namespace InnoShop.Tests.Products.Fixtures;

public sealed class FakeUsers : IUserStateClient
{
    public Dictionary<Guid, UserAccessState> States { get; } = [];
    public bool Offline { get; set; }

    public Task<IReadOnlyList<UserAccessState>> GetManyAsync(IReadOnlyList<Guid> ids)
    {
        if (Offline)
        {
            throw new AppException(503, "Users service is temporarily unavailable.");
        }

        return Task.FromResult<IReadOnlyList<UserAccessState>>(ids.Where(States.ContainsKey).Select(x => States[x]).ToArray());
    }
}
