using System.Diagnostics;
using LibreHardwareMonitor.Hardware;

namespace TdpDisplay;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var icon = new NotifyIcon();
        icon.Visible = true;
        icon.Icon = new Icon(typeof(Program), "Resources.TDP-Logo-weiss.ico");

        icon.DoubleClick += (sender, e) => Application.Exit(); // alternate way of closing

        var computer = new Computer();
        computer.IsCpuEnabled = true;
        computer.IsGpuEnabled = true;
        computer.Open();

        var contextStrip = new ContextMenuStrip();
        contextStrip.Items.Add("Exit", null, (sender, e)
            => Application.Exit()); // close the application

        icon.ContextMenuStrip = contextStrip;

        var cpuWMax = new double?();
        var gpuWMax = new double?();

        var timer = new System.Windows.Forms.Timer { Interval = 3000 };
        timer.Tick += (sender, e) =>
        {
            var cpuW = GetPower(computer, HardwareType.Cpu, "Package");
            var gpuW = GetPower(computer, HardwareType.GpuNvidia, "Package");
            
            if (cpuWMax < cpuW || cpuWMax == null) cpuWMax = cpuW;
            if (gpuWMax < gpuW || gpuWMax == null) gpuWMax = gpuW;
            
            icon.Text = $"CPU {cpuW?.ToString("0.0") ?? "?"} W | GPU {gpuW?.ToString("0.0") ?? "?"} W"
                + "\n" + "Max wattage: " + $"{cpuWMax?.ToString("0.0") ?? "?"} W | {gpuWMax?.ToString("0.0") ?? "?"} W";
            
            //TODO fix the gpuW Value as it can reach 365 watts due to nvidia optimus issues
        };
        timer.Start();

        Application.Run();
    }

    static double? GetPower(Computer computer, HardwareType type, string nameContains)
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
}