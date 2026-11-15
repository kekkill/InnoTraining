namespace InnoShop.Products.Api.Composition.Configuration;

public sealed class AuthenticationOptions
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
}

public sealed class IntegrationOptions
{
    public const string SectionName = "Integration";
    public string ApiKey { get; set; } = "";
}
