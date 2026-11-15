using InnoShop.Users.Api.Application.Abstractions;
using InnoShop.Users.Api.Domain.Authentication;
using InnoShop.Users.Api.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace InnoShop.Users.Api.Infrastructure.Persistence;

public sealed class UserRepository(UsersDbContext db) : IUserRepository
{
    public Task<UserAccount?> GetAsync(Guid id)
    {
        return db.Users.FirstOrDefaultAsync(user => user.Id == id);
    }

    public Task<UserAccount?> FindByEmailAsync(string email)
    {
        return db.Users.FirstOrDefaultAsync(user => user.Email == email);
    }

    public async Task<IReadOnlyList<UserAccount>> GetManyAsync(IReadOnlyList<Guid> ids)
    {
        return await db.Users.AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .ToListAsync();
    }

    public Task<bool> EmailExistsAsync(string email)
    {
        return db.Users.AnyAsync(user => user.Email == email);
    }

    public async Task<IReadOnlyList<UserAccount>> ListAsync(int page, int pageSize)
    {
        return await db.Users.AsNoTracking()
            .Where(user => !user.IsDeleted)
            .OrderBy(user => user.CreatedUtc)
            .ThenBy(user => user.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public Task<OneTimeToken?> FindTokenAsync(string hash, string purpose)
    {
        return db.Tokens.FirstOrDefaultAsync(token =>
            token.Hash == hash && token.Purpose == purpose &&
            token.ConsumedUtc == null && token.ExpiresUtc > DateTime.UtcNow);
    }

    public async Task<IReadOnlyList<OneTimeToken>> ActiveTokensAsync(Guid id, string purpose)
    {
        return await db.Tokens
            .Where(token => token.UserId == id && token.Purpose == purpose && token.ConsumedUtc == null)
            .ToListAsync();
    }

    public void Add(UserAccount user) => db.Users.Add(user);
    public void Add(OneTimeToken token) => db.Tokens.Add(token);
    public Task SaveAsync() => db.SaveChangesAsync();
}
