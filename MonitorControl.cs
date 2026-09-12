using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;

namespace MonitorMux;

public sealed class MonitorEntry
{
    public required IntPtr Handle { get; init; }
    public required string Description { get; init; }
    public required string FriendlyName { get; init; }
    public required string AdapterDeviceName { get; init; }
}

/// <summary>
/// Talks to monitors over DDC/CI (VESA MCCS) via the Windows Monitor Configuration API.
/// VCP code 0x60 is "Input Source Select"; valid values are manufacturer-defined per MCCS,
/// so different monitor models don't all agree on which number means HDMI 1 vs HDMI 2.
/// </summary>
public static class MonitorControl
{
    public const byte VcpInputSource = 0x60;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct PHYSICAL_MONITOR
    {
        public IntPtr hPhysicalMonitor;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szPhysicalMonitorDescription;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DISPLAY_DEVICE
    {
        public int cb;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;

        public int StateFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    private const int DISPLAY_DEVICE_ATTACHED_TO_DESKTOP = 0x1;
    private const uint EDD_GET_DEVICE_INTERFACE_NAME = 0x00000001;

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out uint pdwNumberOfPhysicalMonitors);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetVCPFeatureAndVCPFeatureReply(IntPtr hMonitor, byte bVCPCode, IntPtr pvct, out uint pdwCurrentValue, out uint pdwMaximumValue);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool SetVCPFeature(IntPtr hMonitor, byte bVCPCode, uint dwNewValue);

    public static List<MonitorEntry> Enumerate()
    {
        var results = new List<MonitorEntry>();
        var edidNamesByInstance = QueryEdidFriendlyNames();

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr _, ref RECT _, IntPtr _) =>
        {
            var mi = new MONITORINFOEX { cbSize = Marshal.SizeOf<MONITORINFOEX>() };
            if (!GetMonitorInfo(hMonitor, ref mi))
                return true;

            if (!GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out uint count) || count == 0)
                return true;

            var physicalMonitors = new PHYSICAL_MONITOR[count];
            if (!GetPhysicalMonitorsFromHMONITOR(hMonitor, count, physicalMonitors))
                return true;

            for (int i = 0; i < count; i++)
            {
                string friendlyName = ResolveEdidFriendlyName(mi.szDevice, i, edidNamesByInstance)
                    ?? GetFriendlyName(mi.szDevice)
                    ?? mi.szDevice;

                results.Add(new MonitorEntry
                {
                    Handle = physicalMonitors[i].hPhysicalMonitor,
                    Description = physicalMonitors[i].szPhysicalMonitorDescription,
                    FriendlyName = friendlyName,
                    AdapterDeviceName = mi.szDevice,
                });
            }

            return true;
        }, IntPtr.Zero);

        return results;
    }

    /// <summary>
    /// EnumDisplayDevices' plain DeviceString is usually just the generic driver label
    /// ("Generic PnP Monitor"), not the monitor's real EDID model name. WmiMonitorID (root\WMI)
    /// decodes the EDID and exposes the actual name, keyed by PnP device instance path.
    /// </summary>
    private static Dictionary<string, string> QueryEdidFriendlyNames()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\WMI",
                "SELECT InstanceName, UserFriendlyName, UserFriendlyNameLength FROM WmiMonitorID");

            foreach (ManagementObject mo in searcher.Get())
            {
                if (mo["InstanceName"] is not string instanceName)
                    continue;

                if (mo["UserFriendlyName"] is not ushort[] nameChars || mo["UserFriendlyNameLength"] is not ushort length || length == 0)
                    continue;

                var name = new string(Array.ConvertAll(nameChars[..length], c => (char)c)).TrimEnd('\0').Trim();
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                // WMI suffixes the instance path with "_0", "_1", etc.; strip it so it matches
                // the plain PnP device instance path we derive from EnumDisplayDevices below.
                var normalizedKey = Regex.Replace(instanceName, @"_\d+$", "");
                result[normalizedKey] = name;
            }
        }
        catch
        {
            // WMI unavailable/blocked: callers fall back to the generic driver-reported name.
        }

        return result;
    }

    private static string? ResolveEdidFriendlyName(string adapterDeviceName, int monitorIndex, Dictionary<string, string> edidNamesByInstance)
    {
        var dd = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };
        if (!EnumDisplayDevices(adapterDeviceName, (uint)monitorIndex, ref dd, EDD_GET_DEVICE_INTERFACE_NAME))
            return null;

        // dd.DeviceID looks like \\?\DISPLAY#BENQ2477#4&1a2b3c4d&0&UID4352#{guid};
        // rebuild it as the plain "DISPLAY\BENQ2477\4&1a2b3c4d&0&UID4352" instance path.
        var parts = dd.DeviceID.Split('#');
        if (parts.Length < 3)
            return null;

        var deviceClass = parts[0].Length > 4 ? parts[0][4..] : parts[0];
        var instanceKey = $"{deviceClass}\\{parts[1]}\\{parts[2]}";

        return edidNamesByInstance.TryGetValue(instanceKey, out var name) ? name : null;
    }

    public static void Release(MonitorEntry entry)
    {
        var arr = new[]
        {
            new PHYSICAL_MONITOR { hPhysicalMonitor = entry.Handle, szPhysicalMonitorDescription = entry.Description }
        };
        DestroyPhysicalMonitors(1, arr);
    }

    private static string? GetFriendlyName(string adapterDeviceName)
    {
        var dd = new DISPLAY_DEVICE { cb = Marshal.SizeOf<DISPLAY_DEVICE>() };
        uint devNum = 0;
        while (EnumDisplayDevices(adapterDeviceName, devNum, ref dd, 0))
        {
            if ((dd.StateFlags & DISPLAY_DEVICE_ATTACHED_TO_DESKTOP) != 0 && !string.IsNullOrWhiteSpace(dd.DeviceString))
                return dd.DeviceString;
            devNum++;
        }
        return null;
    }

    // Windows' DDC/CI implementation is known to fail transiently (the underlying I2C bus is
    // slow and shared with other traffic), even when the monitor and cabling are fine, so a
    // single failed call isn't conclusive. Retry a few times before giving up.
    private const int RetryCount = 4;
    private const int RetryDelayMs = 50;

    public static int LastWin32Error { get; private set; }

    public static bool TryGetInputSource(MonitorEntry entry, out uint currentValue, out uint maxValue)
    {
        uint value = 0, max = 0;
        bool ok = false;

        for (int attempt = 0; attempt < RetryCount && !ok; attempt++)
        {
            if (attempt > 0)
                Thread.Sleep(RetryDelayMs);

            ok = GetVCPFeatureAndVCPFeatureReply(entry.Handle, VcpInputSource, IntPtr.Zero, out value, out max);
            if (!ok)
                LastWin32Error = Marshal.GetLastWin32Error();
        }

        currentValue = value;
        maxValue = max;
        return ok;
    }

    public static bool TrySetInputSource(MonitorEntry entry, uint value)
    {
        for (int attempt = 0; attempt < RetryCount; attempt++)
        {
            if (attempt > 0)
                Thread.Sleep(RetryDelayMs);

            if (SetVCPFeature(entry.Handle, VcpInputSource, value))
                return true;

            LastWin32Error = Marshal.GetLastWin32Error();
        }

        return false;
    }
}
