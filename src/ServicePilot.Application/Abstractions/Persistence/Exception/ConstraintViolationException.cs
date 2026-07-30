namespace ServicePilot.Application.Abstractions.Persistence.Exceptions;

public sealed class ConstraintViolationException : Exception
{
    public ConstraintViolationException(
        string? constraintName,
        Exception innerException)
        : base(
            "A database constraint was violated",
            innerException)
    {
        ConstraintName = constraintName;
    }

    public string? ConstraintName { get; }
}