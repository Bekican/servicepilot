namespace ServicePilot.Domain.Customers;

public sealed class CustomerAddress
{
    public const int MaxLabelLength = 100;
    public const int MaxLineLength = 200;
    public const int MaxCityLength = 100;
    public const int MaxRegionLength = 100;
    public const int MaxPostalCodeLength = 32;
    public const int CountryCodeLength = 2;

    private CustomerAddress()
    {
    }

    public CustomerAddress(
        Guid id,
        Guid organizationId,
        Guid customerId,
        string? label,
        string line1,
        string? line2,
        string city,
        string? region,
        string? postalCode,
        string countryCode,
        bool isPrimary,
        DateTimeOffset createdAtUtc)
    {
        ValidateIdentifier(id, nameof(id));
        ValidateIdentifier(
            organizationId,
            nameof(organizationId));
        ValidateIdentifier(
            customerId,
            nameof(customerId));

        Id = id;
        OrganizationId = organizationId;
        CustomerId = customerId;
        SetDetails(
            label,
            line1,
            line2,
            city,
            region,
            postalCode,
            countryCode);
        IsPrimary = isPrimary;
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string? Label { get; private set; }
    public string Line1 { get; private set; } = string.Empty;
    public string? Line2 { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string? Region { get; private set; }
    public string? PostalCode { get; private set; }
    public string CountryCode { get; private set; } = string.Empty;
    public bool IsPrimary { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public void Update(
        string? label,
        string line1,
        string? line2,
        string city,
        string? region,
        string? postalCode,
        string countryCode,
        DateTimeOffset updatedAtUtc)
    {
        SetDetails(
            label,
            line1,
            line2,
            city,
            region,
            postalCode,
            countryCode);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void SetPrimary(
        bool isPrimary,
        DateTimeOffset updatedAtUtc)
    {
        IsPrimary = isPrimary;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Deactivate(DateTimeOffset updatedAtUtc)
    {
        IsActive = false;
        IsPrimary = false;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Activate(DateTimeOffset updatedAtUtc)
    {
        IsActive = true;
        IsPrimary = false;
        UpdatedAtUtc = updatedAtUtc;
    }

    private void SetDetails(
        string? label,
        string line1,
        string? line2,
        string city,
        string? region,
        string? postalCode,
        string countryCode)
    {
        Label = OptionalText(
            label,
            MaxLabelLength,
            nameof(label));
        Line1 = RequiredText(
            line1,
            MaxLineLength,
            nameof(line1));
        Line2 = OptionalText(
            line2,
            MaxLineLength,
            nameof(line2));
        City = RequiredText(
            city,
            MaxCityLength,
            nameof(city));
        Region = OptionalText(
            region,
            MaxRegionLength,
            nameof(region));
        PostalCode = OptionalText(
            postalCode,
            MaxPostalCodeLength,
            nameof(postalCode));

        string normalizedCountryCode =
            RequiredText(
                countryCode,
                CountryCodeLength,
                nameof(countryCode))
            .ToUpperInvariant();

        if (normalizedCountryCode.Length
            != CountryCodeLength
            || !normalizedCountryCode.All(
                character =>
                    character is >= 'A' and <= 'Z'))
        {
            throw new ArgumentException(
                "Country code must be ISO 3166-1 alpha-2",
                nameof(countryCode));
        }

        CountryCode = normalizedCountryCode;
    }

    private static string RequiredText(
        string? value,
        int maxLength,
        string parameterName)
    {
        return OptionalText(
                value,
                maxLength,
                parameterName)
            ?? throw new ArgumentException(
                "Required address value cannot be empty",
                parameterName);
    }

    private static string? OptionalText(
        string? value,
        int maxLength,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();

        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException(
                "Address value is too long",
                parameterName);
        }

        return trimmed;
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
}