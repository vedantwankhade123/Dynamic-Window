using Newton.Core.Models;
using Newton.Core.Services;
using Newton.Windows.Notifications;

namespace Newton.App.Services;

/// <summary>Adapts the chat add-on's events to Newton's existing activity pipeline.</summary>
internal sealed class NewtonChatActivityAdapter : IDisposable
{
    private readonly UniversalReplyService _chat;
    private readonly ActivityManager _activities;

    public NewtonChatActivityAdapter(UniversalReplyService chat, ActivityManager activities)
    {
        _chat = chat;
        _activities = activities;
        _chat.OnChatMessageReceived += OnMessageReceived;
        _chat.OnChatMessageRemoved += OnMessageRemoved;
        _chat.OnReplySent += OnReplySent;
    }

    private void OnMessageReceived(object? sender, ChatNotification message)
    {
        _activities.AddOrUpdateActivity(new ActivityItem
        {
            Id = GetActivityId(message),
            Type = ActivityType.Notification,
            Title = string.IsNullOrWhiteSpace(message.Title) ? message.AppName : $"{message.AppName} · {message.Title}",
            Subtitle = message.Body,
            IconGlyph = "\uE715",
            AccentHex = "#0A84FF",
            Status = ActivityStatus.Active,
            Priority = ActivityPriority.High,
            IsSimulated = false,
            Metadata =
            {
                ["SourceApp"] = message.AppName,
                ["AppId"] = message.AppId,
                ["NotificationId"] = message.NotificationId.ToString()
            }
        });
    }

    private void OnMessageRemoved(object? sender, ChatNotification message) =>
        _activities.RemoveActivity(GetActivityId(message));

    private void OnReplySent(object? sender, ChatReplyResult result)
    {
        var activity = _activities.Activities.FirstOrDefault(item => item.Id == GetActivityId(result.Notification));
        if (activity is null) return;

        activity.Subtitle = result.Succeeded ? "Reply sent" : result.Error ?? "Reply unavailable";
        _activities.AddOrUpdateActivity(activity);
    }

    private static string GetActivityId(ChatNotification message) => $"win-chat-{message.NotificationId}";

    public void Dispose()
    {
        _chat.OnChatMessageReceived -= OnMessageReceived;
        _chat.OnChatMessageRemoved -= OnMessageRemoved;
        _chat.OnReplySent -= OnReplySent;
    }
}
