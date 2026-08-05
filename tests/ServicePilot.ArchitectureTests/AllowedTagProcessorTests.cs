using System.Diagnostics;

using ServicePilot.Observability;

namespace ServicePilot.ArchitectureTests;

public sealed class AllowedTraceTagProcessorTests
{
    [Fact]
    public void OnEnd_ShouldKeepAllowedTags_AndRemoveAllOtherTags()
    {
        AllowedTraceTagProcessor processor = new();
        using Activity activity =
            new("test.operation");

        activity.SetTag(
            "http.route",
            "api/customers/{customerId}");
        activity.SetTag(
            "servicepilot.correlation_id",
            "support-1234");

        activity.SetTag(
            "url.full",
            "https://example.test/customers/secret-id");
        activity.SetTag(
            "exception.message",
            "password=should-not-leak");

        processor.OnEnd(activity);

        Assert.Equal(
            "api/customers/{customerId}",
            activity.GetTagItem("http.route"));
        Assert.Equal(
            "support-1234",
            activity.GetTagItem(
                "servicepilot.correlation_id"));

        Assert.Null(
            activity.GetTagItem("url.full"));
        Assert.Null(
            activity.GetTagItem(
                "exception.message"));
    }
}
