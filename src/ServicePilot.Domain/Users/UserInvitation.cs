namespace ServicePilot.Domain.Users;

public sealed class UserInvitation
{
    public const int MaxEmailLength = User.MaxEmailLength;
    public const int TokenHashLength = 64;

    private UserInvitation()
    {
    }

    public UserInvitation(
        Guid id,
        Guid organizationId,
        string email,
        string role,
        string tokenHash,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        ValidateIdentifier(id, nameof(id));
        ValidateIdentifier(
            organizationId,
            nameof(organizationId));
        ValidateEmail(email);
        ValidateRole(role);
        ValidateTokenHash(tokenHash);
        ValidateExpiration(issuedAtUtc, expiresAtUtc);

        Id = id;
        OrganizationId = organizationId;
        Email = email.Trim().ToLowerInvariant();
        Role = role;
        TokenHash = tokenHash;
        Status = UserInvitationStatus.Pending;
        IssuedAtUtc = issuedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string Role { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public UserInvitationStatus Status { get; private set; }
    public DateTimeOffset IssuedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public bool CanBeAcceptedAt(DateTimeOffset nowUtc)
    {
        return Status == UserInvitationStatus.Pending
            && nowUtc < ExpiresAtUtc;
    }

    public void Renew(
        string role,
        string tokenHash,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (Status != UserInvitationStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only a pending invitation can be renewed");
        }

        ValidateRole(role);
        ValidateTokenHash(tokenHash);
        ValidateExpiration(issuedAtUtc, expiresAtUtc);

        Role = role;
        TokenHash = tokenHash;
        IssuedAtUtc = issuedAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public void MarkAccepted(DateTimeOffset acceptedAtUtc)
    {
        if (!CanBeAcceptedAt(acceptedAtUtc))
        {
            throw new InvalidOperationException(
                "Invitation is not valid for acceptance");
        }

        Status = UserInvitationStatus.Accepted;
        AcceptedAtUtc = acceptedAtUtc;
    }

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        if (Status != UserInvitationStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only a pending invitation can be revoked");
        }

        Status = UserInvitationStatus.Revoked;
        RevokedAtUtc = revokedAtUtc;
    }

    private static void ValidateIdentifier(
        Guid identifier,
        string parameterName)
    {
        if (identifier == Guid.Empty)
        {
            throw new ArgumentException(
                "Identifier cannot be empty",
                parameterName);
        }
    }

    private static void ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException(
                "Invitation email cannot be empty",
                nameof(email));
        }

        if (email.Trim().Length > MaxEmailLength)
        {
            throw new ArgumentException(
                "Invitation email is too long",
                nameof(email));
        }
    }

    private static void ValidateRole(string role)
    {
        if (!UserRoles.IsSupported(role))
        {
            throw new ArgumentException(
                "Invitation role is not supported",
                nameof(role));
        }
    }

    private static void ValidateTokenHash(string tokenHash)
    {
        if (tokenHash.Length != TokenHashLength)
        {
            throw new ArgumentException(
                "Invitation token hash must be a SHA-256 hexadecimal value",
                nameof(tokenHash));
        }
    }

    private static void ValidateExpiration(
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (expiresAtUtc <= issuedAtUtc)
        {
            throw new ArgumentException(
                "Invitation expiration must be after its issue time",
                nameof(expiresAtUtc));
        }
    }
}