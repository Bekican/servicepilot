namespace ServicePilot.Domain.Customers;

public sealed class Customer
{
    public const int MaxFirstNameLength = 100;
    public const int MaxLastNameLength = 100;
    public const int MaxCompanyNameLength = 200;
    public const int MaxContactPersonLength = 200;
    public const int MaxEmailLength = 320;
    public const int MaxPhoneLength = 16;

    private Customer()
    {
    }

    private Customer(
        Guid id,
        Guid organizationId,
        long number,
        CustomerType type,
        string? firstName,
        string? lastName,
        string? companyName,
        string? contactPerson,
        string? email,
        string? phone,
        DateTimeOffset createdAtUtc)
    {
        ValidateIdentifier(id, nameof(id));
        ValidateIdentifier(
            organizationId,
            nameof(organizationId));

        if (number <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(number),
                "Customer number must be positive");
        }

        Id = id;
        OrganizationId = organizationId;
        Number = number;
        Type = type;
        SetNames(
            type,
            firstName,
            lastName,
            companyName,
            contactPerson);
        SetContact(email, phone);
        IsActive = true;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public long Number { get; private set; }
    public CustomerType Type { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? CompanyName { get; private set; }
    public string? ContactPerson { get; private set; }
    public string? Email { get; private set; }
    public string? NormalizedEmail { get; private set; }
    public string? Phone { get; private set; }
    public string? NormalizedPhone { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public string CustomerNumber =>
        $"CUS-{Number:D6}";

    public static Customer CreateIndividual(
        Guid id,
        Guid organizationId,
        long number,
        string firstName,
        string lastName,
        string? email,
        string? phone,
        DateTimeOffset createdAtUtc)
    {
        return new Customer(
            id,
            organizationId,
            number,
            CustomerType.Individual,
            firstName,
            lastName,
            null,
            null,
            email,
            phone,
            createdAtUtc);
    }

    public static Customer CreateCompany(
        Guid id,
        Guid organizationId,
        long number,
        string companyName,
        string? contactPerson,
        string? email,
        string? phone,
        DateTimeOffset createdAtUtc)
    {
        return new Customer(
            id,
            organizationId,
            number,
            CustomerType.Company,
            null,
            null,
            companyName,
            contactPerson,
            email,
            phone,
            createdAtUtc);
    }

    public void UpdateIndividual(
        string firstName,
        string lastName,
        string? email,
        string? phone,
        DateTimeOffset updatedAtUtc)
    {
        SetNames(
            CustomerType.Individual,
            firstName,
            lastName,
            null,
            null);
        SetContact(email, phone);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void UpdateCompany(
        string companyName,
        string? contactPerson,
        string? email,
        string? phone,
        DateTimeOffset updatedAtUtc)
    {
        SetNames(
            CustomerType.Company,
            null,
            null,
            companyName,
            contactPerson);
        SetContact(email, phone);
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Deactivate(DateTimeOffset updatedAtUtc)
    {
        IsActive = false;
        UpdatedAtUtc = updatedAtUtc;
    }

    private void SetNames(
        CustomerType type,
        string? firstName,
        string? lastName,
        string? companyName,
        string? contactPerson)
    {
        if (type == CustomerType.Individual)
        {
            FirstName = RequiredText(
                firstName,
                MaxFirstNameLength,
                nameof(firstName));
            LastName = RequiredText(
                lastName,
                MaxLastNameLength,
                nameof(lastName));
            CompanyName = null;
            ContactPerson = null;
        }
        else if (type == CustomerType.Company)
        {
            CompanyName = RequiredText(
                companyName,
                MaxCompanyNameLength,
                nameof(companyName));
            ContactPerson = OptionalText(
                contactPerson,
                MaxContactPersonLength,
                nameof(contactPerson));
            FirstName = null;
            LastName = null;
        }
        else
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                "Customer type is unsupported");
        }

        Type = type;
    }

    private void SetContact(
        string? email,
        string? phone)
    {
        string? normalizedEmail =
            CustomerContactNormalizer.NormalizeEmail(email);
        string? normalizedPhone =
            CustomerContactNormalizer.NormalizePhone(phone);

        if (normalizedEmail?.Length > MaxEmailLength)
        {
            throw new ArgumentException(
                "Customer email is too long",
                nameof(email));
        }

        Email = normalizedEmail;
        NormalizedEmail = normalizedEmail;
        Phone = normalizedPhone;
        NormalizedPhone = normalizedPhone;
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
                "Required customer value cannot be empty",
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
                "Customer value is too long",
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