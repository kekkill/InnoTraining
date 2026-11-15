namespace InnoShop.Users.Api.Domain.Authentication;

public sealed class OneTimeToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Hash { get; set; } = "";
    public string Purpose { get; set; } = "";
    public DateTime ExpiresUtc { get; set; }
    public DateTime? ConsumedUtc { get; set; }
}
