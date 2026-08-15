using ServicePilot.Application.Common;

namespace ServicePilot.Application.Customers;

public static class CustomerErrors
{
    public static readonly Error NotFound = new(
        "Customer.NotFound",
        "Customer was not found");
    public static readonly Error AddressNotFound = new(
        "Customer.AddressNotFound",
        "Customer address was not found");
    public static readonly Error InvalidData = new(
        "Customer.InvalidData",
        "Customer data is invalid");
    public static readonly Error InvalidType = Error.ForField(
        "Customer.InvalidType", "Customer type is invalid", "type", "Invalid");
    public static readonly Error FirstNameRequired = Error.ForField(
        "Customer.FirstNameRequired", "First name is required", "firstName", "Required");
    public static readonly Error LastNameRequired = Error.ForField(
        "Customer.LastNameRequired", "Last name is required", "lastName", "Required");
    public static readonly Error CompanyNameRequired = Error.ForField(
        "Customer.CompanyNameRequired", "Company name is required", "companyName", "Required");
    public static readonly Error FirstNameTooLong = Error.ForField(
        "Customer.FirstNameTooLong", "First name is too long", "firstName", "TooLong");
    public static readonly Error LastNameTooLong = Error.ForField(
        "Customer.LastNameTooLong", "Last name is too long", "lastName", "TooLong");
    public static readonly Error CompanyNameTooLong = Error.ForField(
        "Customer.CompanyNameTooLong", "Company name is too long", "companyName", "TooLong");
    public static readonly Error ContactPersonTooLong = Error.ForField(
        "Customer.ContactPersonTooLong", "Contact person is too long", "contactPerson", "TooLong");
    public static readonly Error EmailTooLong = Error.ForField(
        "Customer.EmailTooLong", "Customer email is too long", "email", "TooLong");
    public static readonly Error InvalidEmail = Error.ForField(
        "Customer.InvalidEmail", "Customer email is invalid", "email", "InvalidEmail");
    public static readonly Error InvalidPhone = Error.ForField(
        "Customer.InvalidPhone", "Customer phone is invalid", "phone", "InvalidPhone");
    public static readonly Error EmailAlreadyExists = new(
        "Customer.EmailAlreadyExists",
        "A customer with this email already exists");
    public static readonly Error EmailBelongsToInactiveCustomer = new(
        "Customer.EmailBelongsToInactiveCustomer",
        "This email belongs to an inactive customer");
    public static readonly Error PhoneAlreadyExists = new(
        "Customer.PhoneAlreadyExists",
        "A customer with this phone already exists");
    public static readonly Error PhoneBelongsToInactiveCustomer = new(
        "Customer.PhoneBelongsToInactiveCustomer",
        "This phone belongs to an inactive customer");
    public static readonly Error PrimaryAddressConflict = new(
        "Customer.PrimaryAddressConflict",
        "Another address became primary first");

    public static Error AddressField(string field, string code) => Error.ForField(
        "Customer.AddressInvalid", "Customer address is invalid", field, code);
}
