using System.Diagnostics;

using OpenTelemetry;

namespace ServicePilot.Observability;

internal sealed class AllowedTraceTagProcessor
    : BaseProcessor<Activity>
{
    private static readonly HashSet<string>
        AllowedTagNames = new(
            StringComparer.Ordinal)
        {
            "http.request.method",
            "http.route",
            "http.response.status_code",
            "db.system.name",
            "db.operation.name",
            "db.response.status_code",
            "exception.type",
            "problem.code",
            "servicepilot.correlation_id",
            "servicepilot.reminder.processed_count",
            "servicepilot.retention.deleted_invitation_count",
            "servicepilot.retention.anonymized_audit_count",
            "servicepilot.migration.count"
        };

    public override void OnEnd(Activity activity)
    {
        activity.SetStatus(activity.Status);

        foreach (KeyValuePair<string, object?> tag
                 in activity.TagObjects.ToArray())
        {
            if (!AllowedTagNames.Contains(tag.Key))
            {
                activity.SetTag(tag.Key, null);
            }
        }
    }
}
