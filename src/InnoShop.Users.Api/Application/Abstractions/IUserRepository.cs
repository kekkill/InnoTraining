using InnoShop.Users.Api.Domain.Authentication;
using InnoShop.Users.Api.Domain.Users;

namespace InnoShop.Users.Api.Application.Abstractions;

public interface IUserRepository
{
    Task<UserAccount?> GetAsync(Guid id);
    Task<UserAccount?> FindByEmailAsync(string email);
    Task<IReadOnlyList<UserAccount>> GetManyAsync(IReadOnlyList<Guid> ids);
    Task<bool> EmailExistsAsync(string email);
    Task<IReadOnlyList<UserAccount>> ListAsync(
        int page,
        int pageSize);
    Task<OneTimeToken?> FindTokenAsync(
        string hash,
        string purpose);
    Task<IReadOnlyList<OneTimeToken>> ActiveTokensAsync(
        Guid userId,
        string purpose);
    void Add(UserAccount user);
    void Add(OneTimeToken token);
    Task SaveAsync();
}
