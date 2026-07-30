using ServicePilot.Application.Reminders;
using ServicePilot.Infrastructure;
using ServicePilot.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ReminderProcessor>();
builder.Services.AddWorkerInfrastructure(
    builder.Configuration);
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<RetentionWorker>();

var host = builder.Build();
host.Run();