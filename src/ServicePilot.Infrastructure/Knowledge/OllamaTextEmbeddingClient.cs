using System.Net.Http.Json;
using System.Text.Json.Serialization;

using ServicePilot.Application.Knowledge;

namespace ServicePilot.Infrastructure.Knowledge;

internal sealed class OllamaTextEmbeddingClient(
    OllamaEmbeddingOptions options)
    : ITextEmbeddingClient, IDisposable
{
    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = options.BaseAddress,
        Timeout = TimeSpan.FromMinutes(3)
    };

    public async Task<EmbeddingBatch> EmbedAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default)
    {
        if (inputs.Count == 0)
        {
            throw new ArgumentException(
                "At least one embedding input is required",
                nameof(inputs));
        }

        try
        {
            List<IReadOnlyList<float>> embeddings = [];
            for (int offset = 0;
                offset < inputs.Count;
                offset += options.BatchSize)
            {
                string[] batch = inputs
                    .Skip(offset)
                    .Take(options.BatchSize)
                    .ToArray();
                using HttpResponseMessage response =
                    await _httpClient.PostAsJsonAsync(
                        "/api/embed",
                        new OllamaEmbedRequest(
                            options.Model,
                            batch,
                            Truncate: false,
                            options.Dimensions),
                        cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new KnowledgeAiUnavailableException(
                        "The local embedding model is unavailable.");
                }

                OllamaEmbedResponse? result =
                    await response.Content.ReadFromJsonAsync<
                        OllamaEmbedResponse>(
                        cancellationToken: cancellationToken);
                if (result is null
                    || result.Embeddings.Count != batch.Length
                    || result.Embeddings.Any(embedding =>
                        embedding.Count != options.Dimensions))
                {
                    throw new KnowledgeAiUnavailableException(
                        "The local embedding model returned an invalid response.");
                }

                embeddings.AddRange(result.Embeddings);
            }

            return new EmbeddingBatch(
                options.Model,
                options.Dimensions,
                embeddings);
        }
        catch (KnowledgeAiUnavailableException)
        {
            throw;
        }
        catch (OperationCanceledException)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new KnowledgeAiUnavailableException(
                "The local embedding model timed out.");
        }
        catch (HttpRequestException exception)
        {
            throw new KnowledgeAiUnavailableException(
                "The local embedding model is unavailable.",
                exception);
        }
        catch (System.Text.Json.JsonException exception)
        {
            throw new KnowledgeAiUnavailableException(
                "The local embedding model returned invalid JSON.",
                exception);
        }
    }

    public void Dispose() => _httpClient.Dispose();

    private sealed record OllamaEmbedRequest(
        string Model,
        IReadOnlyList<string> Input,
        bool Truncate,
        int Dimensions);

    private sealed record OllamaEmbedResponse(
        string Model,
        IReadOnlyList<IReadOnlyList<float>> Embeddings,
        [property: JsonPropertyName("prompt_eval_count")]
        int PromptEvalCount);
}
