using ServicePilot.Application.Email;
using ServicePilot.Application.Knowledge;
using ServicePilot.Application.Reminders;
using ServicePilot.Infrastructure;
using ServicePilot.Observability;
using ServicePilot.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServicePilotObservability("ServicePilot.Worker");
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ReminderProcessor>();
builder.Services.AddScoped<EmailOutboxProcessor>();
builder.Services.AddSingleton<KnowledgeTextChunker>();
builder.Services.AddScoped<KnowledgeDocumentIngestionProcessor>();
builder.Services.AddWorkerInfrastructure(
    builder.Configuration);
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<EmailOutboxWorker>();
builder.Services.AddHostedService<RetentionWorker>();
if (builder.Configuration.GetValue(
    "KnowledgeAi:ProcessingEnabled",
    true))
{
    builder.Services.AddHostedService<KnowledgeIngestionWorker>();
}

var host = builder.Build();
host.Run();