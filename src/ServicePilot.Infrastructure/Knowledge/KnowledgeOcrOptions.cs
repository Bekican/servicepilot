using Microsoft.Extensions.Configuration;

namespace ServicePilot.Infrastructure.Knowledge;

internal sealed record KnowledgeOcrOptions(
    bool Enabled,
    string Languages,
    int Dpi,
    TimeSpan PageTimeout)
{
    public static KnowledgeOcrOptions FromConfiguration(
        IConfiguration configuration) => new(
        configuration.GetValue("KnowledgeOcr:Enabled", false),
        configuration["KnowledgeOcr:Languages"] ?? "tur+eng",
        Math.Clamp(
            configuration.GetValue("KnowledgeOcr:Dpi", 200),
            150,
            300),
        TimeSpan.FromSeconds(Math.Clamp(
            configuration.GetValue(
                "KnowledgeOcr:PageTimeoutSeconds",
                30),
            5,
            120)));
}
