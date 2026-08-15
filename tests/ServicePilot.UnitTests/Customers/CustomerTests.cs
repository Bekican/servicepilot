using ServicePilot.Domain.Customers;

namespace ServicePilot.UnitTests.Customers;

public sealed class CustomerTests
{
    private static readonly DateTimeOffset UtcNow =
        new(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateIndividual_ShouldNormalizeContact()
    {
        Customer customer = Customer.CreateIndividual(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            " Bekir ",
            " Çakmak ",
            " BEKIR@EXAMPLE.COM ",
            "+90 (555) 111-2233",
            UtcNow);

        Assert.Equal(
            CustomerType.Individual,
            customer.Type);
        Assert.Equal("Bekir", customer.FirstName);
        Assert.Equal("Çakmak", customer.LastName);
        Assert.Null(customer.CompanyName);
        Assert.Equal(
            "bekir@example.com",
            customer.NormalizedEmail);
        Assert.Equal(
            "+905551112233",
            customer.NormalizedPhone);
        Assert.Equal("CUS-000001", customer.CustomerNumber);
    }

    [Fact]
    public void CreateCompany_ShouldRequireCompanyName()
    {
        Assert.Throws<ArgumentException>(() =>
            Customer.CreateCompany(
                Guid.NewGuid(),
                Guid.NewGuid(),
                1,
                " ",
                null,
                null,
                null,
                UtcNow));
    }

    [Fact]
    public void CreateIndividual_ShouldRequireBothNames()
    {
        Assert.Throws<ArgumentException>(() =>
            Customer.CreateIndividual(
                Guid.NewGuid(),
                Guid.NewGuid(),
                1,
                "Bekir",
                string.Empty,
                null,
                null,
                UtcNow));
    }

    [Fact]
    public void Create_ShouldRejectInvalidPhone()
    {
        Assert.Throws<ArgumentException>(() =>
            Customer.CreateIndividual(
                Guid.NewGuid(),
                Guid.NewGuid(),
                1,
                "Bekir",
                "Çakmak",
                null,
                "555 111 22 33",
                UtcNow));
    }

    [Fact]
    public void Create_ShouldNormalizeLocalTurkeyPhoneToE164()
    {
        Customer customer = Customer.CreateIndividual(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            "Bekir",
            "Çakmak",
            null,
            "0555 111 22 33",
            UtcNow);

        Assert.Equal("+905551112233", customer.NormalizedPhone);
    }

    [Fact]
    public void AddressDeactivation_ShouldClearPrimaryFlag()
    {
        CustomerAddress address = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Home",
            "Main Street 1",
            null,
            "Istanbul",
            null,
            "34000",
            "tr",
            true,
            UtcNow);

        address.Deactivate(UtcNow.AddMinutes(1));

        Assert.False(address.IsActive);
        Assert.False(address.IsPrimary);
        Assert.Equal("TR", address.CountryCode);

        address.Activate(UtcNow.AddMinutes(2));

        Assert.True(address.IsActive);
        Assert.False(address.IsPrimary);
    }
}