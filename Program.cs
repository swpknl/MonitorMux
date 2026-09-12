namespace MonitorMux;

static class Program
{
    // Fixed GUID-derived name so a second launch (e.g. double-clicking the exe while the
    // tray instance is already running from startup) detects the existing instance instead
    // of spawning a duplicate tray icon.
    private const string MutexName = "MonitorMux-SingleInstance-8F3E1B2A-9C4D-4E7A-9C1B-2F6E7D8A1B3C";

    [STAThread]
    static void Main(string[] args)
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        if (!createdNew)
            return;

        ApplicationConfiguration.Initialize();

        bool startInTray = args.Any(a => a.Equals("--tray", StringComparison.OrdinalIgnoreCase)
            || a.Equals("/tray", StringComparison.OrdinalIgnoreCase));

        Application.Run(new Form1(startInTray));

        GC.KeepAlive(mutex);
    }
}
