using System.Diagnostics;

using OpenTelemetry;

namespace ServicePilot.Observability;

internal sealed class SensitiveHttpTagProcessor
    : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        activity.SetTag("url.full", null);
        activity.SetTag("url.path", null);
        activity.SetTag("url.query", null);
        activity.SetTag("http.url", null);
        activity.SetTag("http.target", null);
    }
}
