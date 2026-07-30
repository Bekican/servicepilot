namespace ServicePilot.Domain.Users;

public sealed class User
{
    public const int MaxFirstNameLength = 100;
    public const int MaxLastNameLength = 100;
    public const int MaxEmailLength = 320;
    public const int MaxPasswordHashLength = 512;
    public const int MaxRoleLength = 32;

    private User()
    {
    }

    public User(
        Guid id,
        Guid organizationId,
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        string role,
        DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "User identifier cannot be empty",
                nameof(id));
        }

        if (organizationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Organization identifier cannot be empty",
                nameof(organizationId));
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new ArgumentException(
                "User first name cannot be empty",
                nameof(firstName));
        }

        if (firstName.Trim().Length > MaxFirstNameLength)
        {
            throw new ArgumentException(
                "User first name is too long",
                nameof(firstName));
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new ArgumentException(
                "User last name cannot be empty",
                nameof(lastName));
        }

        if (lastName.Trim().Length > MaxLastNameLength)
        {
            throw new ArgumentException(
                "User last name is too long",
                nameof(lastName));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException(
                "User email cannot be empty",
                nameof(email));
        }

        if (email.Trim().Length > MaxEmailLength)
        {
            throw new ArgumentException(
                "User email is too long",
                nameof(email));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException(
                "User password hash cannot be empty",
                nameof(passwordHash));
        }

        if (passwordHash.Length > MaxPasswordHashLength)
        {
            throw new ArgumentException(
                "User password hash is too long",
                nameof(passwordHash));
        }

        if (!UserRoles.IsSupported(role))
        {
            throw new ArgumentException(
                "User role is not supported",
                nameof(role));
        }

        Id = id;
        OrganizationId = organizationId;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Role = role;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void ChangeRole(string role)
    {
        if (!UserRoles.IsSupported(role))
        {
            throw new ArgumentException(
                "User role is not supported",
                nameof(role));
        }

        Role = role;
    }
}