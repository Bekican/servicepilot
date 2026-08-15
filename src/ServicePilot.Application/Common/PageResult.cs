namespace ServicePilot.Application.Common;

public sealed record PageResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public const int MaxPage = 1_000_000;

    public int TotalPages => TotalCount == 0
        ? 0
        : (int)Math.Ceiling((double)TotalCount / PageSize);

    public static (int Page, int PageSize) Normalize(int page, int pageSize) =>
        (Math.Clamp(page, 1, MaxPage), Math.Clamp(pageSize, 1, 100));

    public static int CalculateSkip(int page, int pageSize)
    {
        long skip = ((long)page - 1) * pageSize;
        return (int)Math.Min(skip, int.MaxValue);
    }
}
