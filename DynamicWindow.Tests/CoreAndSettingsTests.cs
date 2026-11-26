using DynamicWindow.Core.Models;
using DynamicWindow.Core.Services;
using DynamicWindow.Infrastructure.Database;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DynamicWindow.Tests;

public sealed class CoreAndSettingsTests
{
    [Fact]
    public void ActivityManager_SelectsHighestPriorityActiveActivity()
    {
        var manager = new ActivityManager();

        manager.AddOrUpdateActivity(new ActivityItem
        {
            Id = "low-1",
            Type = ActivityType.Music,
            Title = "Song",
            Priority = ActivityPriority.Normal,
            Status = ActivityStatus.Active
        });

        manager.AddOrUpdateActivity(new ActivityItem
        {
            Id = "high-1",
            Type = ActivityType.Download,
            Title = "SDK Download",
            Priority = ActivityPriority.High,
            Status = ActivityStatus.Active
        });

        Assert.Equal("high-1", manager.PrimaryActivity?.Id);
    }

    [Fact]
    public void RealTimerService_StartsPausesAndStopsTimer()
    {
        var manager = new ActivityManager();
        using var timerService = new RealTimerAndNotificationService(manager);

        timerService.StartTimer(120, "Deep Focus");
        Assert.True(timerService.IsRunning);
        Assert.Equal(120, timerService.RemainingSeconds);
        Assert.Equal("dynamicwindow-timer", manager.PrimaryActivity?.Id);

        timerService.TogglePauseResume();
        Assert.False(timerService.IsRunning);

        timerService.StopTimer();
        Assert.Null(manager.PrimaryActivity);
    }

    [Fact]
    public async Task SqliteSettingsService_PersistsAndLoadsSettings()
    {
        var tempDb = Path.Combine(Path.GetTempPath(), $"dynamicwindow_test_{Guid.NewGuid():N}.db");
        try
        {
            var svc = new SqliteSettingsService(NullLogger<SqliteSettingsService>.Instance, tempDb);
            await svc.LoadAsync();

            var updated = new DynamicWindowSettings
            {
                CompactWidth = 240,
                PositionMode = DynamicWindowPositionMode.TopRight,
                DynamicWindowOpacity = 0.88
            };
            await svc.SaveAsync(updated);

            var svc2 = new SqliteSettingsService(NullLogger<SqliteSettingsService>.Instance, tempDb);
            await svc2.LoadAsync();

            Assert.Equal(240, svc2.Current.CompactWidth);
            Assert.Equal(DynamicWindowPositionMode.TopRight, svc2.Current.PositionMode);
            Assert.Equal(0.88, svc2.Current.DynamicWindowOpacity);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(tempDb)) File.Delete(tempDb);
        }
    }
}
