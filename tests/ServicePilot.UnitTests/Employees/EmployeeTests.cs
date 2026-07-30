using ServicePilot.Domain.Employees;

namespace ServicePilot.UnitTests.Employees;

public sealed class EmployeeTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 7, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Constructor_ShouldCreateActiveEmployee_WhenInputIsValid()
    {
        Guid id = Guid.NewGuid();
        Guid organizationId = Guid.NewGuid();

        Employee employee = new(
            id,
            organizationId,
            "  Ada  ",
            "  Lovelace  ",
            "  ADA@EXAMPLE.COM  ",
            CreatedAtUtc);

        Assert.Equal(id, employee.Id);
        Assert.Equal(organizationId, employee.OrganizationId);
        Assert.Equal("Ada", employee.FirstName);
        Assert.Equal("Lovelace", employee.LastName);
        Assert.Equal("ada@example.com", employee.Email);
        Assert.True(employee.IsActive);
        Assert.Equal(CreatedAtUtc, employee.CreatedAtUtc);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenIdIsEmpty()
    {
        Action action = () => CreateEmployee(id: Guid.Empty);

        ArgumentException exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenOrganizationIdIsEmpty()
    {
        Action action = () =>
            CreateEmployee(organizationId: Guid.Empty);

        ArgumentException exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("organizationId", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_ShouldThrow_WhenFirstNameIsEmpty(
        string firstName)
    {
        Action action = () => CreateEmployee(firstName: firstName);

        ArgumentException exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("firstName", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_ShouldThrow_WhenLastNameIsEmpty(
        string lastName)
    {
        Action action = () => CreateEmployee(lastName: lastName);

        ArgumentException exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("lastName", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_ShouldThrow_WhenEmailIsEmpty(string email)
    {
        Action action = () => CreateEmployee(email: email);

        ArgumentException exception =
            Assert.Throws<ArgumentException>(action);

        Assert.Equal("email", exception.ParamName);
    }

    [Fact]
    public void Deactivate_ShouldMakeEmployeeInactive()
    {
        Employee employee = CreateEmployee();

        employee.Deactivate();

        Assert.False(employee.IsActive);
    }

    private static Employee CreateEmployee(
        Guid? id = null,
        Guid? organizationId = null,
        string firstName = "Ada",
        string lastName = "Lovelace",
        string email = "ada@example.com")
    {
        return new Employee(
            id ?? Guid.NewGuid(),
            organizationId ?? Guid.NewGuid(),
            firstName,
            lastName,
            email,
            CreatedAtUtc);
    }
}