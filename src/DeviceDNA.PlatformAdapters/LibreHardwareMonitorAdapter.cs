//
// DeviceDNA
// Author:  nobody174 (nobodylearn174@gmail.com)
// Repo:    https://github.com/nobody174/DeviceDNA
// Patreon: https://www.patreon.com/c/Nobody174
// License: All rights reserved (c) 2026 nobody174
// "It's never too late to give up!"
//

using LibreHardwareMonitor.Hardware;

namespace DeviceDNA.PlatformAdapters;

// Concrete adapter wrapping a single LibreHardwareMonitor Computer instance.
// One Computer.Open() call enables all relevant hardware groups (CPU, GPU, Memory, Storage,
// Motherboard, Network) and feeds sensor readings into multiple DNAs from a single source —
// per CLAUDE.md's "organize by data source, not by DNA type" rule.
// Sensor access typically requires admin/elevated rights on Windows; if a hardware group
// cannot be opened (e.g. running unelevated), this adapter degrades gracefully and returns
// whatever readings ARE available rather than throwing.
public class LibreHardwareMonitorAdapter : ILibreHardwareMonitorAdapter
{
    private class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var subHardware in hardware.SubHardware)
            {
                subHardware.Accept(this);
            }
        }

        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }

    public IReadOnlyList<RawSensorReading> ReadAllSensors()
    {
        var readings = new List<RawSensorReading>();

        var computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsStorageEnabled = true,
            IsMotherboardEnabled = true,
            IsNetworkEnabled = true,
        };

        try
        {
            computer.Open();
            computer.Accept(new UpdateVisitor());

            foreach (var hardware in computer.Hardware)
            {
                CollectFromHardware(hardware, readings);
            }
        }
        catch (Exception)
        {
            // Sensor access commonly fails without elevation (e.g. some kernel drivers cannot load).
            // Return whatever was collected so far — the app must still function with WMI-only data.
        }
        finally
        {
            try
            {
                computer.Close();
            }
            catch (Exception)
            {
                // Closing can throw if Open() partially failed; nothing further to do.
            }
        }

        return readings;
    }

    private static void CollectFromHardware(IHardware hardware, List<RawSensorReading> readings)
    {
        foreach (var sensor in hardware.Sensors)
        {
            readings.Add(new RawSensorReading
            {
                HardwareName = hardware.Name,
                HardwareType = hardware.HardwareType.ToString(),
                HardwareIdentifier = hardware.Identifier.ToString(),
                SensorName = sensor.Name,
                SensorType = sensor.SensorType.ToString(),
                Value = sensor.Value,
            });
        }

        foreach (var subHardware in hardware.SubHardware)
        {
            CollectFromHardware(subHardware, readings);
        }
    }
}

//*Built with assistance from __Claude Code__ by Anthropic.*
