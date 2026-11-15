namespace InnoShop.Users.Api.Application.Abstractions;

public interface IEmailSender
{
    Task SendConfirmationAsync(string recipient, string token);
    Task SendPasswordResetAsync(string recipient, string token);
}
