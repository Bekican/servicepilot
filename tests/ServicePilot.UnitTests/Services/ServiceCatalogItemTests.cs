using ServicePilot.Domain.Services;

namespace ServicePilot.UnitTests.Services;

public sealed class ServiceCatalogItemTests
{
    [Fact]
    public void Constructor_ShouldNormalizeName()
    {
        ServiceCatalogItem service = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            " Boiler Repair ",
            90,
            DateTimeOffset.UtcNow);

        Assert.Equal("Boiler Repair", service.Name);
        Assert.Equal(
            "boiler repair",
            service.NormalizedName);
        Assert.True(service.IsActive);
    }

    [Fact]
    public void Constructor_ShouldRejectInvalidDuration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ServiceCatalogItem(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Repair",
                0,
                DateTimeOffset.UtcNow));
    }
}