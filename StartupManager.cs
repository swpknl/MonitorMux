using Microsoft.Win32;

namespace MonitorMux;

/// <summary>
/// Registers/unregisters the app in the per-user Run key so it launches at logon.
/// HKCU (not HKLM) is used deliberately: no admin rights needed, and it only affects this user.
/// </summary>
public static class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "MonitorMux";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return key?.GetValue(ValueName) as string == ExpectedCommand();
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

        if (enabled)
            key.SetValue(ValueName, ExpectedCommand());
        else
            key.DeleteValue(ValueName, throwOnMissingValue: false);
    }

    private static string ExpectedCommand() => $"\"{Application.ExecutablePath}\" --tray";
}
