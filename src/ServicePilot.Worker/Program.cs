using ServicePilot.Application.Reminders;
using ServicePilot.Application.Email;
using ServicePilot.Infrastructure;
using ServicePilot.Observability;
using ServicePilot.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServicePilotObservability("ServicePilot.Worker");
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ReminderProcessor>();
builder.Services.AddScoped<EmailOutboxProcessor>();
builder.Services.AddWorkerInfrastructure(
    builder.Configuration);
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<EmailOutboxWorker>();
builder.Services.AddHostedService<RetentionWorker>();

var host = builder.Build();
host.Run();
