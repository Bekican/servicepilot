namespace ServicePilot.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection
    : ICollectionFixture<ServicePilotApiFactory>
{
    public const string Name = "ServicePilot integration tests";
}