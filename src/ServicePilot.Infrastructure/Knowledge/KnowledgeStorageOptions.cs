using Microsoft.Extensions.Configuration;

namespace ServicePilot.Infrastructure.Knowledge;

internal sealed record KnowledgeStorageOptions(string RootPath)
{
    public static KnowledgeStorageOptions FromConfiguration(
        IConfiguration configuration)
    {
        string configuredPath = configuration[
            "KnowledgeStorage:RootPath"]
            ?? Path.Combine(
                Environment.CurrentDirectory,
                "data");

        return new KnowledgeStorageOptions(
            Path.GetFullPath(configuredPath));
    }
}
