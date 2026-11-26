using DynamicWindow.Core.Models;

namespace DynamicWindow.Core.Events;

public sealed class ActivityChangedEventArgs : EventArgs
{
    public ActivityItem? Activity { get; }
    public string ChangeType { get; }

    public ActivityChangedEventArgs(ActivityItem? activity, string changeType)
    {
        Activity = activity;
        ChangeType = changeType;
    }
}
