namespace InnoShop.Users.Api.Domain.Users;

public sealed class UserAccount
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Name { get; private set; } = "";
    public string Email { get; private set; } = "";
    public string PasswordHash { get; private set; } = "";
    public string Role { get; private set; } = "User";
    public bool EmailConfirmed { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsDeleted { get; private set; }
    public int AuthVersion { get; private set; } = 1;
    public int FailedLogins { get; private set; }
    public DateTime? LockedUntilUtc { get; private set; }
    public DateTime CreatedUtc { get; private set; } = DateTime.UtcNow;

    public bool ProductsVisible => EmailConfirmed && IsActive && !IsDeleted;

    private UserAccount()
    {
    }

    public static UserAccount Create(string name, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return new UserAccount
        {
            Name = name.Trim(),
            Email = email.Trim().ToLowerInvariant()
        };
    }

    public static UserAccount CreateAdministrator(string email)
    {
        var user = Create("Administrator", email);
        user.ConfirmEmail();
        user.SetRole("Admin");
        return user;
    }

    public void SetPasswordHash(string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        PasswordHash = hash;
    }

    public void ConfirmEmail() => EmailConfirmed = true;

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
    }

    public void SetActive(bool active)
    {
        if (IsActive == active)
        {
            return;
        }

        IsActive = active;
        AuthVersion++;
    }

    public void SetRole(string role)
    {
        if (role is not ("User" or "Admin"))
        {
            throw new ArgumentException("unknown role", nameof(role));
        }

        if (Role == role)
        {
            return;
        }

        Role = role;
        AuthVersion++;
    }

    public void Delete()
    {
        IsDeleted = true;
        IsActive = false;
        AuthVersion++;
    }

    public void RecordFailedLogin()
    {
        FailedLogins++;
        if (FailedLogins >= 5)
        {
            FailedLogins = 0;
            LockedUntilUtc = DateTime.UtcNow.AddMinutes(5);
        }
    }

    public void RecordSuccessfulLogin()
    {
        FailedLogins = 0;
        LockedUntilUtc = null;
    }

    public void ResetPassword(string hash)
    {
        SetPasswordHash(hash);
        AuthVersion++;
        RecordSuccessfulLogin();
    }
}
