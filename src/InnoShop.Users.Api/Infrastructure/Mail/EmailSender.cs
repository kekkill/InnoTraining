using InnoShop.Users.Api.Application.Abstractions;
using InnoShop.Users.Api.Application.Exceptions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace InnoShop.Users.Api.Infrastructure.Mail;

public sealed class EmailSender(IOptions<MailOptions> options, ILogger<EmailSender> logger) : IEmailSender
{
    public Task SendConfirmationAsync(string recipient, string token)
    {
        var url = $"{options.Value.PublicApiUrl.TrimEnd('/')}/?action=confirm&token={Uri.EscapeDataString(token)}";
        return SendAsync(recipient, "Подтверждение InnoShop", $"Подтвердите аккаунт по ссылке (действует 1 час):\n{url}");
    }

    public Task SendPasswordResetAsync(string recipient, string token)
    {
        var url = $"{options.Value.PublicApiUrl.TrimEnd('/')}/?action=reset&token={Uri.EscapeDataString(token)}";
        var body = $"Для смены пароля откройте ссылку:\n{url}\nСсылка действует 1 час и используется один раз.";
        return SendAsync(recipient, "Сброс пароля InnoShop", body);
    }

    private async Task SendAsync(
        string recipient,
        string subject,
        string body)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            throw new AppException(503, "email is not configured");
        }

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(settings.From));
        message.To.Add(MailboxAddress.Parse(recipient));
        message.Subject = subject;
        message.Body = new TextPart("plain")
        {
            Text = body
        };
        try
        {
            using var smtp = new SmtpClient();
            smtp.Timeout = 15000;
            await smtp.ConnectAsync(settings.SmtpHost, settings.SmtpPort, settings.SmtpSecurity);
            await smtp.AuthenticateAsync(settings.SmtpUser, settings.SmtpPassword);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
        catch (AuthenticationException)
        {
            logger.LogWarning("smtp login failed");
            throw new AppException(503, "smtp login failed, check email and app password");
        }
        catch (Exception exception)
        {
            logger.LogWarning("email send failed ({FailureType})", exception.GetType().Name);
            throw new AppException(503, "could not send email, try again later");
        }
    }
}
