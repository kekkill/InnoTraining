using InnoShop.Users.Api.Application.Abstractions;
using InnoShop.Users.Api.Application.Exceptions;

namespace InnoShop.Tests.Users.Fixtures;

public sealed class FakeEmailSender : IEmailSender
{
    public List<(string Recipient, string Subject, string Token)> Messages { get; } = [];
    public bool Fails { get; set; }

    public Task SendConfirmationAsync(string recipient, string token) =>
        RecordAsync(recipient, "Подтверждение InnoShop", token);

    public Task SendPasswordResetAsync(string recipient, string token) =>
        RecordAsync(recipient, "Сброс пароля InnoShop", token);

    private Task RecordAsync(string recipient, string subject, string token)
    {
        if (Fails)
        {
            throw new AppException(503, "could not send email, try again later");
        }

        Messages.Add((recipient, subject, token));
        return Task.CompletedTask;
    }
}
