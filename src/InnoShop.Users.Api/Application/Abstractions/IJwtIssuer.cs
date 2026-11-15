using InnoShop.Users.Api.Domain.Users;

namespace InnoShop.Users.Api.Application.Abstractions;

public interface IJwtIssuer
{
    string Issue(UserAccount user);
}
