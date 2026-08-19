using Microsoft.Extensions.Configuration;

namespace ServicePilot.Infrastructure.Knowledge;

internal sealed record OllamaGroundedAnswerOptions(
    Uri BaseAddress,
    string Model)
{
    public static OllamaGroundedAnswerOptions FromConfiguration(
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

        string model = configuration["KnowledgeAi:ChatModel"]
            ?? "qwen3:4b";
        if (model.EndsWith(":cloud", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Cloud models are disabled for knowledge answers.");
        }

        return new OllamaGroundedAnswerOptions(baseAddress, model);
    }
}
