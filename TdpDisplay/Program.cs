using LibreHardwareMonitor.Hardware;
using SystemInformation = System.Windows.Forms.SystemInformation;
using Timer = System.Windows.Forms.Timer;

namespace TdpDisplay;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var icon = new NotifyIcon();
        icon.Visible = true;
        icon.Icon = new Icon(typeof(Program), "Resources.TDP-Logo-weiss.ico");

        icon.DoubleClick += (sender, e) => Application.Exit(); // alternate way of closing

        var computer = new Computer();
        computer.IsCpuEnabled = true;
        computer.IsGpuEnabled = true;
        computer.IsBatteryEnabled = true;
        computer.Open();

        var contextStrip = new ContextMenuStrip();
        contextStrip.Items.Add("Exit", null, (sender, e)
            => Application.Exit()); 

        icon.ContextMenuStrip = contextStrip;

        var cpuWMax = new double?();
        var gpuWMax = new double?();
        var gpuWValidated = new double?();

        var timer = new Timer { Interval = 3000 };
        timer.Tick += (sender, e) =>
        {
            var cpuW = GetPower(computer, HardwareType.Cpu, "Package");
            var gpuW = GetPower(computer, HardwareType.GpuNvidia, "Package");
            
            if (gpuW is > 0 and < 200) gpuWValidated = gpuW;
                
            if (cpuWMax < cpuW || cpuWMax == null) cpuWMax = cpuW;
            if (gpuWMax < gpuWValidated || gpuWMax == null) gpuWMax = gpuWValidated;
            
            icon.Text = $"CPU {cpuW?.ToString("0.0") ?? "?"} W | GPU {gpuWValidated?.ToString("0.0") ?? "?"} W"
                + $"\nMax {cpuWMax?.ToString("0.0") ?? "?"} W | {gpuWMax?.ToString("0.0") ?? "?"} W"
                + $"\nBatt {GetBatteryText(computer)}";
            
            
        };
        timer.Start();

        Application.Run();
    }
    
    private static double? GetPower(Computer computer, HardwareType type, string nameContains)
    {
        foreach (var hardware in computer.Hardware)
        {
            if (hardware.HardwareType != type) continue;
            hardware.Update();
            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType == SensorType.Power &&
                    sensor.Value.HasValue &&
                    sensor.Name.Contains(nameContains, StringComparison.OrdinalIgnoreCase))
                {
                    return
                        sensor.Value; // in watts                                                                                                                                                                                                                                                                                                   
                }
            }
        }
        return null;
    }

    private static string GetBatteryText(Computer computer)
    {
        var power = SystemInformation.PowerStatus;

        // Percent: Windows native, works even if LHM cannot read the battery
        var percent = power.BatteryLifePercent >= 0
            ? $"{power.BatteryLifePercent * 100:0}%"
            : "?";

        // Remaining time: LHM sensor first, Windows native as fallback
        var remaining = GetRemainingTime(computer);
        if (remaining is null && power.BatteryLifeRemaining >= 0)
            remaining = TimeSpan.FromSeconds(power.BatteryLifeRemaining);

        return remaining.HasValue
            ? $"{percent} {remaining.Value:hh\\:mm}"
            : percent;
    }

     private static TimeSpan? GetRemainingTime(Computer computer)
     {                                                                                                                                                                                                   
         foreach (var hardware in computer.Hardware)                                                                                                                                                     
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
}