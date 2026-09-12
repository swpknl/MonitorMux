using System.Text.Json;

namespace MonitorMux;

public class MonitorProfile
{
    // Spec-default MCCS input-source codes; overridden per-monitor once learned via "Read Current".
    public int Hdmi1Code { get; set; } = 0x11;
    public int Hdmi2Code { get; set; } = 0x12;

    // USB-C (DP Alt Mode) monitors typically report this input under the standard
    // DisplayPort-1 VCP code, but this varies by manufacturer just like HDMI 1/2.
    public int UsbcCode { get; set; } = 0x0F;
}

public class AppConfig
{
    public Dictionary<string, MonitorProfile> Profiles { get; set; } = new();

    private static string ConfigPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonitorMux", "config.json");

    // Carried over from this app's previous name; lets anyone upgrading keep the input
    // codes they already learned for their monitors instead of re-learning them.
    private static string LegacyConfigPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BenqInputSwitcher", "config.json");

    public static AppConfig Load()
    {
        try
        {
            var path = File.Exists(ConfigPath) ? ConfigPath : LegacyConfigPath;
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var cfg = JsonSerializer.Deserialize<AppConfig>(json);
                if (cfg != null)
                    return cfg;
            }
        }
        catch (Exception)
        {
            // Config file missing/corrupt: fall back to defaults rather than fail startup.
        }

        return new AppConfig();
    }

    public void Save()
    {
        var dir = Path.GetDirectoryName(ConfigPath)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
    }

    public MonitorProfile GetProfile(string key)
    {
        if (!Profiles.TryGetValue(key, out var profile))
        {
            profile = new MonitorProfile();
            Profiles[key] = profile;
        }

        return profile;
    }
}
