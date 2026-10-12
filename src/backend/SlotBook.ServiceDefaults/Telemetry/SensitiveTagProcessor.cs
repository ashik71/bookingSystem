using System.Diagnostics;
using OpenTelemetry;

namespace SlotBook.ServiceDefaults.Telemetry;

/// <summary>
/// Removes tags that can carry personal data from every span before export (AD-17): query strings,
/// database command text and request or response bodies.
/// </summary>
public sealed class SensitiveTagProcessor : BaseProcessor<Activity>
{
    private static readonly string[] _forbiddenKeys = ["url.query", "http.url", "http.target", "db.query.text", "db.statement"];

    public override void OnEnd(Activity data)
    {
        ArgumentNullException.ThrowIfNull(data);

        foreach (var tag in data.TagObjects.ToList())
        {
            if (IsForbidden(tag.Key))
            {
                data.SetTag(tag.Key, null);
            }
        }
    }

    private static bool IsForbidden(string key) =>
        _forbiddenKeys.Contains(key, StringComparer.Ordinal)
        || key.EndsWith(".body", StringComparison.Ordinal)
        || key.Contains(".body.", StringComparison.Ordinal);
}
