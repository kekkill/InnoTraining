using MailKit.Security;
using MimeKit;

namespace InnoShop.Users.Api.Infrastructure.Mail;

public sealed class MailOptions
{
    public const string SectionName = "Mail";

    public string PublicApiUrl { get; set; } = "http://localhost:5081";
    public string From { get; set; } = "";
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public SecureSocketOptions SmtpSecurity { get; set; } = SecureSocketOptions.StartTls;
    public string SmtpUser { get; set; } = "";
    public string SmtpPassword { get; set; } = "";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(SmtpHost)
        && SmtpPort is > 0 and <= 65535
        && SmtpSecurity is SecureSocketOptions.StartTls or SecureSocketOptions.SslOnConnect or SecureSocketOptions.None
        && !string.IsNullOrWhiteSpace(SmtpUser)
        && !string.IsNullOrWhiteSpace(SmtpPassword)
        && MailboxAddress.TryParse(From, out _)
        && Uri.TryCreate(PublicApiUrl, UriKind.Absolute, out var url)
        && url.Scheme is "http" or "https";
}
