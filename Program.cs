namespace MonitorMux;

static class Program
{
    // Fixed GUID-derived name so a second launch (e.g. double-clicking the exe while the
    // tray instance is already running from startup) detects the existing instance instead
    // of spawning a duplicate tray icon.
    private const string MutexName = "MonitorMux-SingleInstance-8F3E1B2A-9C4D-4E7A-9C1B-2F6E7D8A1B3C";

    // Signals an already-running instance to restore its window. Needed because the main
    // window is often hidden (in the tray, or closed-to-tray), so a second launch attempt
    // (e.g. clicking a pinned taskbar icon) has no window to activate on its own — it can
    // only ask the first instance to show itself, then exit.
    private const string ShowEventName = "MonitorMux-ShowEvent-8F3E1B2A-9C4D-4E7A-9C1B-2F6E7D8A1B3C";

    [STAThread]
    static void Main(string[] args)
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            try
            {
                using var existingShowEvent = EventWaitHandle.OpenExisting(ShowEventName);
                existingShowEvent.Set();
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                // The running instance hasn't created its event yet; nothing we can do here.
            }

            return;
        }

        ApplicationConfiguration.Initialize();

        bool startInTray = args.Any(a => a.Equals("--tray", StringComparison.OrdinalIgnoreCase)
            || a.Equals("/tray", StringComparison.OrdinalIgnoreCase));

        var form = new Form1(startInTray);

        using var showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        var watcher = new Thread(() =>
        {
            while (showEvent.WaitOne())
                form.BeginInvoke(new Action(form.RequestShow));
        })
        {
            IsBackground = true,
        };
        watcher.Start();

        Application.Run(form);

        GC.KeepAlive(mutex);
    }
}
