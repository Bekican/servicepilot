namespace ServicePilot.Application.Customers;

public sealed record CustomerData(
    string Type,
    string? FirstName,
    string? LastName,
    string? CompanyName,
    string? ContactPerson,
    string? Email,
    string? Phone);

public sealed record AddressData(
    string? Label,
    string Line1,
    string? Line2,
    string City,
    string? Region,
    string? PostalCode,
    string CountryCode,
    bool IsPrimary);

public sealed record CustomerAddressResponse(
    Guid Id,
    string? Label,
    string Line1,
    string? Line2,
    string City,
    string? Region,
    string? PostalCode,
    string CountryCode,
    bool IsPrimary,
    bool IsActive);

public sealed record CustomerResponse(
    Guid Id,
    string CustomerNumber,
    string Type,
    string? FirstName,
    string? LastName,
    string? CompanyName,
    string? ContactPerson,
    string? Email,
    string? Phone,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<CustomerAddressResponse> Addresses);