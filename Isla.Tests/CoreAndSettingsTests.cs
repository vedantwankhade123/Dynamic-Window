using Isla.Core.Models;
using Isla.Core.Services;
using Isla.Infrastructure.Database;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Isla.Tests;

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
    public void MockDemoService_SimulatesMusicAndDownload()
    {
        var manager = new ActivityManager();
        using var demo = new MockDemoService(manager);

        Assert.Equal("Midnight City (M83 Rework)", demo.CurrentTrack.Title);

        demo.StartDownloadDemo();
        Assert.Equal("demo-download", manager.PrimaryActivity?.Id);

        demo.NextTrack();
        Assert.Equal("Resonance", demo.CurrentTrack.Title);
    }

    [Fact]
    public async Task SqliteSettingsService_PersistsAndLoadsSettings()
    {
        var tempDb = Path.Combine(Path.GetTempPath(), $"isla_test_{Guid.NewGuid():N}.db");
        try
        {
            var svc = new SqliteSettingsService(NullLogger<SqliteSettingsService>.Instance, tempDb);
            await svc.LoadAsync();

            var updated = new IslaSettings
            {
                CompactWidth = 240,
                PositionMode = IslaPositionMode.TopRight,
                IslandOpacity = 0.88
            };
            await svc.SaveAsync(updated);

            var svc2 = new SqliteSettingsService(NullLogger<SqliteSettingsService>.Instance, tempDb);
            await svc2.LoadAsync();

            Assert.Equal(240, svc2.Current.CompactWidth);
            Assert.Equal(IslaPositionMode.TopRight, svc2.Current.PositionMode);
            Assert.Equal(0.88, svc2.Current.IslandOpacity);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(tempDb)) File.Delete(tempDb);
        }
    }
}
