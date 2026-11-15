
namespace InnoShop.Products.Api.Application.Exceptions;

public sealed class AppException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}
