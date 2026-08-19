using Microsoft.Extensions.Configuration;

namespace ServicePilot.Infrastructure.Knowledge;

internal sealed record OllamaEmbeddingOptions(
    Uri BaseAddress,
    string Model,
    int Dimensions,
    int BatchSize)
{
    public static OllamaEmbeddingOptions FromConfiguration(
        IConfiguration configuration)
    {
        string baseUrl = configuration["KnowledgeAi:OllamaBaseUrl"]
            ?? "http://localhost:11434";
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? baseAddress)
            || baseAddress.Scheme is not "http" and not "https")
        {
            throw new InvalidOperationException(
                "KnowledgeAi:OllamaBaseUrl must be an absolute HTTP(S) URL.");
        }

        bool localEndpoint = baseAddress.IsLoopback
            || baseAddress.Host.Equals(
                "host.docker.internal",
                StringComparison.OrdinalIgnoreCase)
            || baseAddress.Host.Equals(
                "ollama",
                StringComparison.OrdinalIgnoreCase);
        if (!localEndpoint)
        {
            throw new InvalidOperationException(
                "The Ollama endpoint must be local to the ServicePilot deployment.");
        }

        string model = configuration["KnowledgeAi:EmbeddingModel"]
            ?? "qwen3-embedding:0.6b";
        if (model.EndsWith(":cloud", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Cloud models are disabled for knowledge embeddings.");
        }

        int dimensions = configuration.GetValue(
            "KnowledgeAi:EmbeddingDimensions",
            1024);
        if (dimensions != 1024)
        {
            throw new InvalidOperationException(
                "The current knowledge index requires 1024 dimensions.");
        }

        int batchSize = Math.Clamp(
            configuration.GetValue(
                "KnowledgeAi:EmbeddingBatchSize",
                16),
            1,
            64);
        return new OllamaEmbeddingOptions(
            baseAddress,
            model,
            dimensions,
            batchSize);
    }
}