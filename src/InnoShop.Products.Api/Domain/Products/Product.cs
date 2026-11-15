namespace InnoShop.Products.Api.Domain.Products;

public sealed class Product
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid OwnerUserId { get; private set; }
    public string Name { get; private set; } = "";
    public string Description { get; private set; } = "";
    public decimal Price { get; private set; }
    public bool IsAvailable { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedUtc { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; private set; } = DateTime.UtcNow;

    private Product()
    {
    }

    public static Product Create(
        Guid owner,
        string name,
        string description,
        decimal price,
        bool available)
    {
        if (owner == Guid.Empty)
        {
            throw new ArgumentException("owner is required");
        }

        var product = new Product
        {
            OwnerUserId = owner
        };
        product.Update(name, description, price, available);
        return product;
    }

    public void Update(
        string name,
        string description,
        decimal price,
        bool available)
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("product is deleted");
        }

        if (string.IsNullOrWhiteSpace(name) || name.Length > 150 || description is null || description.Length > 3000)
        {
            throw new ArgumentException("invalid product name or description");
        }

        if (price < 0 || price > 9999999999.99m || decimal.Round(price, 2) != price)
        {
            throw new ArgumentException("invalid price (0 to 9999999999.99, up to 2 decimal places)");
        }

        Name = name.Trim();
        Description = description;
        Price = price;
        IsAvailable = available;
        UpdatedUtc = DateTime.UtcNow;
    }

    public void Delete()
    {
        IsDeleted = true;
        UpdatedUtc = DateTime.UtcNow;
    }

    public bool IsVisible(bool ownerActive) => !IsDeleted && ownerActive;
}
