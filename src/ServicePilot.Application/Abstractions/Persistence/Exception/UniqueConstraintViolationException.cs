namespace ServicePilot.Application.Abstractions.Persistence.Exceptions;

public sealed class UniqueConstraintViolationException : Exception
{
    public UniqueConstraintViolationException(
        string? constraintName,
        Exception innerException)
        : base(
            "A unique database constraint was violated",
            innerException)
    {
        ConstraintName = constraintName;
    }

    public string? ConstraintName { get; }
}