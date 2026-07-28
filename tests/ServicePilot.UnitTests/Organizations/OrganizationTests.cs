using ServicePilot.Domain.Organizations;

namespace ServicePilot.UnitTests.Organizations;

public sealed class OrganizationTests
{
    [Fact]
    public void Constructor_ShouldCreateOrganization_WhenInputIsValid()
    {
        Guid id = Guid.NewGuid();

        Organization organization = new(
            id,
            "Acme Technical Service",
            "ACME-TECHNICAL-SERVICE");

        Assert.Equal(id, organization.Id);
        Assert.Equal("Acme Technical Service", organization.Name);
        Assert.Equal("acme-technical-service", organization.Slug);
        Assert.NotEqual(default, organization.CreatedAtUtc);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("    ")]
    public void Constructor_ShouldThrow_WhenNameIsEmpty(string name)
    {
        Action action = () => new Organization(
            Guid.NewGuid(),
            name,
            "acme");

        ArgumentException exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("name", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_ShouldThrow_WhenSlugIsEmpty(string slug)
    {
        Action action = () => new Organization(
            Guid.NewGuid(),
            "Acme",
            slug);

        ArgumentException exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("slug", exception.ParamName);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenIdIsEmpty()
    {
        Action action = () => new Organization(
            Guid.Empty,
            "Acme",
            "acme");

        ArgumentException exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("id", exception.ParamName);
    }
}