using System.Text.Json;

using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ServicePilot.Api.Health;

public sealed class KnowledgeReadinessHealthCheck(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            VerifyStorage();

            using HttpClient client =
                httpClientFactory.CreateClient("knowledge-health");
            using HttpResponseMessage response = await client.GetAsync(
                "api/tags",
                cancellationToken);
            response.EnsureSuccessStatusCode();
            await using Stream body = await response.Content.ReadAsStreamAsync(
                cancellationToken);
            using JsonDocument json = await JsonDocument.ParseAsync(
                body,
                cancellationToken: cancellationToken);
            HashSet<string> models = json.RootElement
                .GetProperty("models")
                .EnumerateArray()
                .Select(model => model.GetProperty("name").GetString())
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            string chatModel = configuration["KnowledgeAi:ChatModel"]
                ?? "qwen3:4b";
            string embeddingModel =
                configuration["KnowledgeAi:EmbeddingModel"]
                ?? "qwen3-embedding:0.6b";
            if (!models.Contains(chatModel)
                || !models.Contains(embeddingModel))
            {
                return HealthCheckResult.Unhealthy(
                    "One or more required local knowledge models are missing.");
            }

            return HealthCheckResult.Healthy(
                "Knowledge storage and local models are ready.");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy(
                "The local knowledge service is unavailable.",
                exception);
        }
    }

    private void VerifyStorage()
    {
        string configuredPath = configuration["KnowledgeStorage:RootPath"]
            ?? "data";
        string rootPath = Path.GetFullPath(configuredPath);
        Directory.CreateDirectory(rootPath);
        string probePath = Path.Combine(
            rootPath,
            $".health-{Guid.NewGuid():N}");
        File.WriteAllText(probePath, string.Empty);
        File.Delete(probePath);
    }
}
