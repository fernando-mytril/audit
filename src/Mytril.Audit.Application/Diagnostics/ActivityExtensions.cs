using System.Diagnostics;

namespace Mytril.Audit.Application.Diagnostics;

public static class ActivityExtensions
{
    public static void RecordException(this Activity activity, Exception exception)
    {
        activity.AddEvent(new ActivityEvent("exception",
            tags: new ActivityTagsCollection
            {
                { "exception.type", exception.GetType().FullName! },
                { "exception.message", exception.Message },
                { "exception.stacktrace", exception.ToString() }
            }));
    }
}
