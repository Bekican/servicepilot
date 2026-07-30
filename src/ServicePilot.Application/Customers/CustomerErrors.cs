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
    public static readonly Error EmailAlreadyExists = new(
        "Customer.EmailAlreadyExists",
        "A customer with this email already exists");
    public static readonly Error PhoneAlreadyExists = new(
        "Customer.PhoneAlreadyExists",
        "A customer with this phone already exists");
    public static readonly Error PrimaryAddressConflict = new(
        "Customer.PrimaryAddressConflict",
        "Another address became primary first");
}