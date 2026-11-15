using InnoShop.Users.Api.Domain.Users;

namespace InnoShop.Users.Api.Application.Abstractions;

public interface IPasswordService
{
    string Hash(UserAccount user, string password);
    bool Verify(UserAccount user, string password);
}
