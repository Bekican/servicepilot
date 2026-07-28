namespace ServicePilot.UnitTests.Organizations.CreateOrganization;

internal sealed class FakeTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _utcNow;

    public FakeTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow()
    {
        return _utcNow;
    }
}