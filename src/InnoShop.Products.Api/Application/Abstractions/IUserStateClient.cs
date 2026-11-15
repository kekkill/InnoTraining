namespace InnoShop.Products.Api.Application.Abstractions;

public interface IUserStateClient
{
    Task<IReadOnlyList<UserAccessState>> GetManyAsync(IReadOnlyList<Guid> ids);
}

public sealed record UserAccessState(
    Guid Id,
    bool CanAccess,
    int AuthVersion);
