using LibreHardwareMonitor.Hardware;
using SystemInformation = System.Windows.Forms.SystemInformation;
using Timer = System.Windows.Forms.Timer;

namespace TdpDisplay;

/// <summary>
/// Responsible for updating the tray icon and context menu.
/// </summary>
/// <remarks>
/// Uses the LHM library to read the power consumption of the CPU and GPU.
/// Also, it uses the Windows API to read the battery life.
/// </remarks>
public sealed class TrayController : IDisposable
{
    private const int UpdateIntervalMs = 3000;

    private readonly NotifyIcon _icon;
    private readonly ContextMenuStrip _contextStrip;
    private readonly Computer _computer;
    private readonly Timer _timer;
    private readonly TdpDatabase _db;

    private double? _cpuWMax;
    private double? _gpuWMax;
    private double? _gpuWValidated;

    public TrayController()
    {
        _db = new TdpDatabase();
        _db.Initialize();
        _icon = new NotifyIcon
        {
            Visible = true,
            Icon = new Icon(typeof(TrayController), "Resources.TDP-Logo-weiss.ico")
        };
        _icon.DoubleClick += (_, _) => Application.Exit(); // alternate way of closing

        _contextStrip = new ContextMenuStrip();
        _contextStrip.Items.Add("Exit", null, (_, _) => Application.Exit());
        _icon.ContextMenuStrip = _contextStrip;
        // TODO perhaps separate into a settings window? Perhaps a new class

        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsBatteryEnabled = true
        };
        _computer.Open();

        _timer = new Timer { Interval = UpdateIntervalMs };
        _timer.Tick += (_, _) => UpdateDisplay();
        _timer.Start();
    }

    private void UpdateDisplay()
    {
        var cpuW = GetPower(HardwareType.Cpu, "Package");
        var gpuW = GetPower(HardwareType.GpuNvidia, "Package");
        _db.SaveReading(Math.Round(cpuW ?? -1, 2), Math.Round(gpuW ?? -1, 2));

        if (gpuW is > 0 and < 200) _gpuWValidated = gpuW;

        if (_cpuWMax < cpuW || _cpuWMax == null) _cpuWMax = cpuW;
        if (_gpuWMax < _gpuWValidated || _gpuWMax == null) _gpuWMax = _gpuWValidated;

        _icon.Text = $"CPU {cpuW?.ToString("0.0") ?? "?"} W | GPU {_gpuWValidated?.ToString("0.0") ?? "?"} W"
            + $"\nMax {_cpuWMax?.ToString("0.0") ?? "?"} W | {_gpuWMax?.ToString("0.0") ?? "?"} W"
            + $"\nBatt {GetBatteryText()}";
    }

    private double? GetPower(HardwareType type, string nameContains)
    {
        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType != type) continue;
            hardware.Update();
            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType == SensorType.Power &&
                    sensor.Value.HasValue &&
                    sensor.Name.Contains(nameContains, StringComparison.OrdinalIgnoreCase))
                {
                    return sensor.Value; // in watts
                }
            }
        }
        return null;
    }

    private string GetBatteryText()
    {
        var power = SystemInformation.PowerStatus;

        // Percent: Windows native works even if LHM cannot read the battery
        var percent = power.BatteryLifePercent >= 0
            ? $"{power.BatteryLifePercent * 100:0}%"
            : "?";

        // Remaining time: LHM sensor first, Windows native as fallback
        var remaining = GetRemainingTime();
        if (remaining is null && power.BatteryLifeRemaining >= 0)
            remaining = TimeSpan.FromSeconds(power.BatteryLifeRemaining);

        return remaining.HasValue
            ? $"{percent} {remaining.Value:hh\\:mm}"
            : percent;
    }

    private TimeSpan? GetRemainingTime()
    {
        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType != HardwareType.Battery) continue;
            hardware.Update();
            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType == SensorType.TimeSpan && sensor.Value.HasValue)
                    return TimeSpan.FromMinutes(sensor.Value.Value);
            }
        }
        return null;
    }

    public void Dispose()
    {
        _timer.Dispose();
        _icon.Dispose();
        _contextStrip.Dispose();
        _computer.Close();
    }
}
