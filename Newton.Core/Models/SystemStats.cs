namespace Newton.Core.Models;

public sealed class SystemStats
{
    public double CpuUsagePercent { get; set; }
    public double RamUsagePercent { get; set; }
    public double RamUsedGb { get; set; }
    public double RamTotalGb { get; set; } = 32.0;
    public double GpuUsagePercent { get; set; }
    public double GpuTempCelsius { get; set; }
    public int BatteryPercent { get; set; } = 84;
    public bool IsCharging { get; set; } = true;
    public double NetworkDownMbps { get; set; }
    public double NetworkUpMbps { get; set; }
    public List<double> CpuHistory { get; init; } = new();
    public List<double> GpuHistory { get; init; } = new();
    public bool IsSimulated { get; init; } = true;
}
