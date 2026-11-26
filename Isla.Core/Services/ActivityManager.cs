using Isla.Core.Events;
using Isla.Core.Interfaces;
using Isla.Core.Models;

namespace Isla.Core.Services;

public sealed class ActivityManager : IActivityService
{
    private readonly List<ActivityItem> _activities = new();
    private readonly object _lock = new();
    private string? _pinnedActivityId;
    private ActivityItem? _primaryActivity;

    public IReadOnlyList<ActivityItem> Activities
    {
        get
        {
            lock (_lock)
            {
                return _activities.ToList().AsReadOnly();
            }
        }
    }

    public ActivityItem? PrimaryActivity
    {
        get
        {
            lock (_lock)
            {
                return _primaryActivity;
            }
        }
    }

    public event EventHandler<ActivityChangedEventArgs>? ActivitiesChanged;
    public event EventHandler<ActivityItem?>? PrimaryActivityChanged;
    public event EventHandler<(string ActivityId, string ActionId)>? ActionRequested;

    public void AddOrUpdateActivity(ActivityItem activity)
    {
        string changeType;
        lock (_lock)
        {
            var index = _activities.FindIndex(a => a.Id == activity.Id);
            if (index >= 0)
            {
                _activities[index] = activity;
                changeType = "Updated";
            }
            else
            {
                _activities.Add(activity);
                changeType = "Added";
            }
        }

        ActivitiesChanged?.Invoke(this, new ActivityChangedEventArgs(activity, changeType));
        RecalculatePrimary();
    }

    public void RemoveActivity(string activityId)
    {
        ActivityItem? removed = null;
        lock (_lock)
        {
            var index = _activities.FindIndex(a => a.Id == activityId);
            if (index >= 0)
            {
                removed = _activities[index];
                _activities.RemoveAt(index);
                if (_pinnedActivityId == activityId)
                {
                    _pinnedActivityId = null;
                }
            }
        }

        if (removed is not null)
        {
            ActivitiesChanged?.Invoke(this, new ActivityChangedEventArgs(removed, "Removed"));
            RecalculatePrimary();
        }
    }

    public void SetPrimaryActivity(string activityId)
    {
        lock (_lock)
        {
            if (_activities.Any(a => a.Id == activityId))
            {
                _pinnedActivityId = activityId;
            }
        }

        RecalculatePrimary();
    }

    public void ExecuteAction(string activityId, string actionId)
    {
        ActionRequested?.Invoke(this, (activityId, actionId));
    }

    private void RecalculatePrimary()
    {
        ActivityItem? nextPrimary;
        bool changed;

        lock (_lock)
        {
            if (_pinnedActivityId is not null)
            {
                nextPrimary = _activities.FirstOrDefault(a => a.Id == _pinnedActivityId);
                if (nextPrimary is null)
                {
                    _pinnedActivityId = null;
                }
            }
            else
            {
                nextPrimary = null;
            }

            nextPrimary ??= _activities
                .OrderByDescending(a => a.Status == ActivityStatus.Active ? 2 : a.Status == ActivityStatus.Paused ? 1 : 0)
                .ThenByDescending(a => (int)a.Priority)
                .ThenByDescending(a => a.StartTime)
                .FirstOrDefault();

            changed = !ReferenceEquals(_primaryActivity, nextPrimary) || _primaryActivity?.Id != nextPrimary?.Id;
            _primaryActivity = nextPrimary;
        }

        if (changed)
        {
            PrimaryActivityChanged?.Invoke(this, nextPrimary);
        }
    }
}
