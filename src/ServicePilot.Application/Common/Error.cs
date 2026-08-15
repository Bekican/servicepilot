namespace ServicePilot.Application.Common;

public sealed record Error(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? FieldErrors = null)

{

    public static readonly Error None = new(
        string.Empty,
        string.Empty);

    public static Error ForField(
        string code,
        string message,
        string field,
        string fieldErrorCode) =>
        new(
            code,
            message,
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                [field] = [fieldErrorCode]
            });
}