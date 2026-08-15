namespace ServicePilot.Domain.Users;

public sealed class PasswordResetToken
{
    public const int TokenHashLength = 64;

    private PasswordResetToken() { }

    public PasswordResetToken(
        Guid id,
        Guid organizationId,
        Guid userId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        if (id == Guid.Empty || organizationId == Guid.Empty || userId == Guid.Empty)
            throw new ArgumentException("Identifiers cannot be empty");
        if (tokenHash.Length != TokenHashLength)
            throw new ArgumentException("Token hash is invalid", nameof(tokenHash));
        if (expiresAtUtc <= createdAtUtc)
            throw new ArgumentException("Expiration must follow creation");

        Id = id;
        OrganizationId = organizationId;
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? UsedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public bool CanBeUsedAt(DateTimeOffset nowUtc) =>
        UsedAtUtc is null && RevokedAtUtc is null && nowUtc < ExpiresAtUtc;

    public void Use(DateTimeOffset usedAtUtc)
    {
        if (!CanBeUsedAt(usedAtUtc)) throw new InvalidOperationException("Token is unavailable");
        UsedAtUtc = usedAtUtc;
    }

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        if (UsedAtUtc is null && RevokedAtUtc is null) RevokedAtUtc = revokedAtUtc;
    }
}