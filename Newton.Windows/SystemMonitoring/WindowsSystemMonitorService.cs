using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using Newton.Core.Interfaces;
using Newton.Core.Models;
using Newton.Core.Services;

namespace Newton.Windows.SystemMonitoring;

public sealed class WindowsSystemMonitorService : ISystemMonitorService, IDisposable
{
    private readonly ActivityManager _activityManager;
    private readonly Timer _timer;

    private ulong _prevIdle;
    private ulong _prevKernel;
    private ulong _prevUser;
    private long _prevBytesReceived;
    private long _prevBytesSent;
    private DateTime _prevSampleTime = DateTime.UtcNow;

    public SystemStats CurrentStats { get; } = new() { IsSimulated = false };
    public event EventHandler<SystemStats>? StatsUpdated;

    public WindowsSystemMonitorService(ActivityManager activityManager)
    {
        _activityManager = activityManager;
        SampleCpuDelta();
        SampleNetworkDelta(out _, out _);
        SampleAll();
        _timer = new Timer(_ => SampleAll(), null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
    }

    public void TriggerSpike()
    {
        // Real system monitor does not fabricate spikes.
    }

    private void SampleAll()
    {
        try
        {
            var cpu = SampleCpuDelta();
            var (ramPct, ramUsedGb, ramTotalGb) = ReadMemory();
            var (batteryPct, isCharging, hasBattery) = ReadPower();
            SampleNetworkDelta(out var downMbps, out var upMbps);

            CurrentStats.CpuUsagePercent = Math.Round(cpu, 0);
            CurrentStats.RamUsagePercent = Math.Round(ramPct, 1);
            CurrentStats.RamUsedGb = Math.Round(ramUsedGb, 1);
            CurrentStats.RamTotalGb = Math.Round(ramTotalGb, 1);
            CurrentStats.BatteryPercent = batteryPct;
            CurrentStats.IsCharging = isCharging;
            CurrentStats.NetworkDownMbps = Math.Round(downMbps, 2);
            CurrentStats.NetworkUpMbps = Math.Round(upMbps, 2);

            CurrentStats.CpuHistory.Add(CurrentStats.CpuUsagePercent);
            if (CurrentStats.CpuHistory.Count > 24) CurrentStats.CpuHistory.RemoveAt(0);

            if (hasBattery && !isCharging && batteryPct <= 20)
            {
                _activityManager.AddOrUpdateActivity(new ActivityItem
                {
                    Id = "win-low-battery",
                    Type = ActivityType.LowBattery,
                    Title = "Low Battery",
                    Subtitle = $"{batteryPct}%",
                    IconGlyph = "\uEBAA",
                    AccentHex = "#FB7185",
                    Progress = batteryPct,
                    Status = ActivityStatus.Active,
                    Priority = ActivityPriority.Urgent,
                    IsSimulated = false
                });
            }
            else
            {
                _activityManager.RemoveActivity("win-low-battery");
            }

            StatsUpdated?.Invoke(this, CurrentStats);
        }
        catch
        {
            // Keep running cleanly on transient API errors
        }
    }

    private double SampleCpuDelta()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user)) return 0;

        ulong idleVal = ((ulong)idle.dwHighDateTime << 32) | idle.dwLowDateTime;
        ulong kernelVal = ((ulong)kernel.dwHighDateTime << 32) | kernel.dwLowDateTime;
        ulong userVal = ((ulong)user.dwHighDateTime << 32) | user.dwLowDateTime;

        ulong idleDiff = idleVal - _prevIdle;
        ulong totalDiff = (kernelVal - _prevKernel) + (userVal - _prevUser);

        _prevIdle = idleVal;
        _prevKernel = kernelVal;
        _prevUser = userVal;

        if (totalDiff == 0) return 0;
        return Math.Clamp((1.0 - (double)idleDiff / totalDiff) * 100.0, 0, 100);
    }

    private static (double Pct, double UsedGb, double TotalGb) ReadMemory()
    {
        var mem = new MEMORYSTATUSEX { dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>() };
        if (!GlobalMemoryStatusEx(ref mem)) return (0, 0, 0);
        double totalGb = mem.ullTotalPhys / (1024.0 * 1024.0 * 1024.0);
        double availGb = mem.ullAvailPhys / (1024.0 * 1024.0 * 1024.0);
        return (mem.dwMemoryLoad, Math.Max(0, totalGb - availGb), totalGb);
    }

    private static (int Pct, bool Charging, bool HasBattery) ReadPower()
    {
        if (!GetSystemPowerStatus(out var power)) return (100, true, false);
        bool hasBattery = (power.BatteryFlag & 128) == 0 && power.BatteryLifePercent <= 100;
        int pct = power.BatteryLifePercent <= 100 ? power.BatteryLifePercent : 100;
        bool charging = power.ACLineStatus == 1;
        return (pct, charging, hasBattery);
    }

    private void SampleNetworkDelta(out double downMbps, out double upMbps)
    {
        long rx = 0, tx = 0;
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
            var stats = nic.GetIPv4Statistics();
            rx += stats.BytesReceived;
            tx += stats.BytesSent;
        }

        var now = DateTime.UtcNow;
        var secs = Math.Max(0.5, (now - _prevSampleTime).TotalSeconds);
        downMbps = _prevBytesReceived > 0 ? Math.Max(0, (rx - _prevBytesReceived) * 8.0 / (secs * 1_000_000.0)) : 0;
        upMbps = _prevBytesSent > 0 ? Math.Max(0, (tx - _prevBytesSent) * 8.0 / (secs * 1_000_000.0)) : 0;

        _prevBytesReceived = rx;
        _prevBytesSent = tx;
        _prevSampleTime = now;
    }

    public void Dispose() => _timer.Dispose();

    [StructLayout(LayoutKind.Sequential)]
    private struct FILETIME { public uint dwLowDateTime; public uint dwHighDateTime; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }

    [DllImport("kernel32.dll")]
    private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

    [DllImport("kernel32.dll")]
    private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

    [DllImport("kernel32.dll")]
    private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);
}
