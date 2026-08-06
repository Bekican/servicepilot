using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Npgsql;

using ServicePilot.Infrastructure;
using ServicePilot.Infrastructure.Persistence;

namespace ServicePilot.ArchitectureTests;

public sealed class DatabaseRegistrationTests
{
    [Fact]
    public void Registration_ShouldUseSingletonDataSource_AndScopedDbContext()
    {
        ServiceCollection services = [];
        IConfiguration configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Database"] =
                            "Host=localhost;Database=test;"
                            + "Username=test;Password=secret"
                    })
                .Build();

        services.AddMigrationInfrastructure(
            configuration);

        ServiceDescriptor dataSourceRegistration =
            Assert.Single(
                services,
                descriptor => descriptor.ServiceType
                    == typeof(NpgsqlDataSource));
        ServiceDescriptor dbContextRegistration =
            Assert.Single(
                services,
                descriptor => descriptor.ServiceType
                    == typeof(ServicePilotDbContext));

        Assert.Equal(
            ServiceLifetime.Singleton,
            dataSourceRegistration.Lifetime);
        Assert.Equal(
            ServiceLifetime.Scoped,
            dbContextRegistration.Lifetime);

        using ServiceProvider provider =
            services.BuildServiceProvider(
                new ServiceProviderOptions
                {
                    ValidateScopes = true
                });
        NpgsqlDataSource firstDataSource =
            provider.GetRequiredService<
                NpgsqlDataSource>();
        NpgsqlDataSource secondDataSource =
            provider.GetRequiredService<
                NpgsqlDataSource>();
        Assert.Same(
            firstDataSource,
            secondDataSource);

        using IServiceScope firstScope =
            provider.CreateScope();
        using IServiceScope secondScope =
            provider.CreateScope();
        ServicePilotDbContext firstContext =
            firstScope.ServiceProvider
                .GetRequiredService<
                    ServicePilotDbContext>();
        ServicePilotDbContext secondContext =
            secondScope.ServiceProvider
                .GetRequiredService<
                    ServicePilotDbContext>();
        Assert.NotSame(firstContext, secondContext);
    }
}
