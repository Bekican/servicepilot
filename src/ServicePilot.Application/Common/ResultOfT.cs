namespace ServicePilot.Application.Common;

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    private Result(
        TValue? value,
        bool isSuccess,
        Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException(
                "The value of a failed result cannot be accessed"
            );

    public static Result<TValue> Success(TValue value)
    {
        return new Result<TValue>(
            value,
            true,
            Error.None);
    }

    public static new Result<TValue> Failure(Error error)
    {
        return new Result<TValue>(
            default,
            false,
            error);
    }
}