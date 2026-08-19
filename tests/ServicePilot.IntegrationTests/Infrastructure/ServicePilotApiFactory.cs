using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

using ServicePilot.Application.Abstractions.Email;
using ServicePilot.Application.Knowledge;
using ServicePilot.Infrastructure.Persistence;

using Testcontainers.PostgreSql;

namespace ServicePilot.IntegrationTests.Infrastructure;

public sealed class ServicePilotApiFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly TestLogSink _logSink = new();
    private readonly string _knowledgeStorageRoot = Path.Combine(
        Path.GetTempPath(),
        "servicepilot-integration-tests",
        Guid.NewGuid().ToString("N"));

    private readonly PostgreSqlContainer _postgresContainer =
        new PostgreSqlBuilder("pgvector/pgvector:0.8.6-pg17")
            .WithDatabase("servicepilot_tests")
            .WithUsername("servicepilot")
            .WithPassword("servicepilot-test-password")
            .Build();

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging =>
            logging.AddProvider(
                _logSink.CreateProvider()));
        builder.UseEnvironment("Testing");
        builder.UseSetting(
            "ConnectionStrings:Database",
            _postgresContainer.GetConnectionString());
        builder.UseSetting(
            "Jwt:SigningKey",
            "servicepilot-integration-test-signing-key-never-use-in-production");
        builder.UseSetting(
            "Invitations:PublicBaseUrl",
            "https://servicepilot.test/invitations/accept");
        builder.UseSetting(
            "PasswordReset:PublicBaseUrl",
            "https://servicepilot.test/app/");
        builder.UseSetting(
            "RateLimiting:AuthenticationPermitLimit",
            "1000");
        builder.UseSetting(
            "RateLimiting:InvitationPermitLimit",
            "1000");
        builder.UseSetting(
            "RateLimiting:PasswordResetPermitLimit",
            "20");
        builder.UseSetting(
            "KnowledgeStorage:RootPath",
            _knowledgeStorageRoot);

        builder.ConfigureServices(services =>
        {
            services.AddControllers().AddApplicationPart(
                typeof(TestingFailureController).Assembly);

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<FakeEmailSender>();
            services.AddSingleton<IEmailSender>(
                serviceProvider =>
                    serviceProvider.GetRequiredService<
                        FakeEmailSender>());
            services.AddSingleton<ITextEmbeddingClient,
                FakeTextEmbeddingClient>();
            services.AddSingleton<IGroundedAnswerGenerator,
                FakeGroundedAnswerGenerator>();
            services.AddSingleton<KnowledgeTextChunker>();
            services.AddScoped<KnowledgeDocumentIngestionProcessor>();
        });
    }

    public FakeEmailSender EmailSender =>
        Services.GetRequiredService<FakeEmailSender>();

    public TestLogSink LogSink => _logSink;

    public string ConnectionString =>
        _postgresContainer.GetConnectionString();

    public async Task<TResult> ExecuteDbContextAsync<TResult>(
        Func<ServicePilotDbContext, Task<TResult>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await using AsyncServiceScope scope =
            Services.CreateAsyncScope();

        ServicePilotDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<
                ServicePilotDbContext>();

        return await operation(dbContext);
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        using HttpClient client = CreateClient();

        await using AsyncServiceScope scope =
            Services.CreateAsyncScope();

        ServicePilotDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<
                ServicePilotDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgresContainer.DisposeAsync();
        if (Directory.Exists(_knowledgeStorageRoot))
        {
            Directory.Delete(
                _knowledgeStorageRoot,
                recursive: true);
        }
    }
}
