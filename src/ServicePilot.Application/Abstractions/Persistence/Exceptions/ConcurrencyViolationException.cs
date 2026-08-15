namespace ServicePilot.Application.Abstractions.Persistence.Exceptions;

public sealed class ConcurrencyViolationException(Exception innerException)
    : Exception("The record was changed by another operation.", innerException);
