using System.Net.Http.Json;
using System.Text.Json.Serialization;

const string ollamaBaseUrl = "http://localhost:11434";
const string model = "qwen3:4b";

using HttpClient httpClient = new()
{
    BaseAddress = new Uri(ollamaBaseUrl),
    Timeout = TimeSpan.FromSeconds(60)
};

OllamaChatRequest request = new(
    Model: model,
    Messages:
    [
        new OllamaMessage(
            Role: "system",
            Content: """
You are a senior backend engineer.
Explain concepts simply but technically.
Never answer with more than four sentences.
"""),

        new OllamaMessage(
            Role: "user",
            Content: "What is a database index and when should I not use one?")
    ],
    Stream: false);

HttpResponseMessage response =
    await httpClient.PostAsJsonAsync(
        "/api/chat",
        request);

response.EnsureSuccessStatusCode();

OllamaChatResponse? chatResponse =
    await response.Content.ReadFromJsonAsync<OllamaChatResponse>();

if (chatResponse is null)
{
    Console.WriteLine("The model returned an empty response.");
    return;
}

Console.WriteLine();
Console.WriteLine("Assistant:");
Console.WriteLine(chatResponse.Message.Content);

Console.WriteLine();
Console.WriteLine($"Model: {chatResponse.Model}");
Console.WriteLine($"Prompt tokens: {chatResponse.PromptEvalCount}");
Console.WriteLine($"Generated tokens: {chatResponse.EvalCount}");
Console.WriteLine(
    $"Total duration: {TimeSpan.FromMilliseconds(chatResponse.TotalDuration / 1_000_000.0)}");
    

public sealed record OllamaChatRequest(
    string Model,
    IReadOnlyList<OllamaMessage> Messages,
    bool Stream);

public sealed record OllamaMessage(
    string Role,
    string Content);

public sealed record OllamaChatResponse(
    string Model,
    OllamaMessage Message,
    bool Done,
    [property : JsonPropertyName("total_duration")]
    long TotalDuration,
    [property : JsonPropertyName("prompt_eval_count")]
    int PromptEvalCount,
    [property : JsonPropertyName("eval_count")]
    int EvalCount);

