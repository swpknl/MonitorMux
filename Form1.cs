using System.Globalization;

namespace MonitorMux;

public partial class Form1 : Form
{
    private List<MonitorEntry> _monitors = new();
    private readonly AppConfig _config = AppConfig.Load();

    private readonly bool _startInTray;
    private bool _initialVisibilitySuppressed;
    private bool _allowExit;

    private NotifyIcon _trayIcon = null!;
    private ToolStripMenuItem _startupMenuItem = null!;

    public Form1() : this(startInTray: false)
    {
    }

    public Form1(bool startInTray)
    {
        _startInTray = startInTray;

        InitializeComponent();
        SetupTrayIcon();

        Load += (_, _) => RefreshMonitors();

        Resize += (_, _) =>
        {
            if (WindowState == FormWindowState.Minimized)
                Hide();
        };

        FormClosing += (_, e) =>
        {
            if (!_allowExit && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                return;
            }

            _config.Save();
            foreach (var m in _monitors)
                MonitorControl.Release(m);
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
        };

        btnRefresh.Click += (_, _) => RefreshMonitors();
        btnReadCurrent.Click += (_, _) => ReadCurrent();
        btnHdmi1.Click += (_, _) => SwitchTo(txtHdmi1Code);
        btnHdmi2.Click += (_, _) => SwitchTo(txtHdmi2Code);
        btnUsbc.Click += (_, _) => SwitchTo(txtUsbcCode);
        cmbMonitors.SelectedIndexChanged += (_, _) => LoadProfileForSelection();
    }

    // Standard trick to start with no visible window/taskbar flash when launched at logon:
    // Application.Run() shows the form via this override, which we redirect to stay hidden.
    // CreateControl() refuses to create the handle while Visible is still false, so the handle
    // would otherwise never exist — and a later BeginInvoke (e.g. from Program.cs restoring the
    // window on a second launch) throws on a control with no handle, taking down the whole
    // process. CreateHandle() forces the native window to exist without making it visible.
    protected override void SetVisibleCore(bool value)
    {
        if (_startInTray && !_initialVisibilitySuppressed)
        {
            _initialVisibilitySuppressed = true;
            if (!IsHandleCreated)
                CreateHandle();
            base.SetVisibleCore(false);
            return;
        }

        base.SetVisibleCore(value);
    }

    private void SetupTrayIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => ShowFromTray());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Switch to HDMI 1", null, (_, _) => SwitchTo(txtHdmi1Code));
        menu.Items.Add("Switch to HDMI 2", null, (_, _) => SwitchTo(txtHdmi2Code));
        menu.Items.Add("Switch to USB-C / DP", null, (_, _) => SwitchTo(txtUsbcCode));
        menu.Items.Add("Read Current", null, (_, _) => ReadCurrent());
        menu.Items.Add(new ToolStripSeparator());

        _startupMenuItem = new ToolStripMenuItem("Start with Windows") { CheckOnClick = true, Checked = StartupManager.IsEnabled() };
        _startupMenuItem.Click += (_, _) => StartupManager.SetEnabled(_startupMenuItem.Checked);
        menu.Items.Add(_startupMenuItem);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApp());

        _trayIcon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application,
            Text = "MonitorMux",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _trayIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                ShowFromTray();
        };
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    // Entry point for a second launch attempt (e.g. a pinned taskbar icon) to ask this
    // already-running instance to restore its window. See Program.Main.
    public void RequestShow() => ShowFromTray();

    private void ExitApp()
    {
        _allowExit = true;
        Close();
    }

    private MonitorEntry? SelectedMonitor =>
        cmbMonitors.SelectedIndex >= 0 && cmbMonitors.SelectedIndex < _monitors.Count
            ? _monitors[cmbMonitors.SelectedIndex]
            : null;

    private void RefreshMonitors()
    {
        foreach (var m in _monitors)
            MonitorControl.Release(m);

        _monitors = MonitorControl.Enumerate();

        cmbMonitors.Items.Clear();
        foreach (var m in _monitors)
            cmbMonitors.Items.Add($"{m.FriendlyName} ({m.AdapterDeviceName})");

        if (cmbMonitors.Items.Count > 0)
        {
            cmbMonitors.SelectedIndex = 0;
        }
        else
        {
            lblCurrentValue.Text = "(none)";
            SetStatus("No DDC/CI-capable monitors found. Make sure DDC/CI is enabled in the monitor's OSD menu.", isError: true);
        }
    }

    private void LoadProfileForSelection()
    {
        var monitor = SelectedMonitor;
        if (monitor == null)
            return;

        var profile = _config.GetProfile(monitor.FriendlyName);
        txtHdmi1Code.Text = "0x" + profile.Hdmi1Code.ToString("X2");
        txtHdmi2Code.Text = "0x" + profile.Hdmi2Code.ToString("X2");
        txtUsbcCode.Text = "0x" + profile.UsbcCode.ToString("X2");
        lblCurrentValue.Text = "(unread)";
    }

    private void ReadCurrent()
    {
        var monitor = SelectedMonitor;
        if (monitor == null)
            return;

        if (MonitorControl.TryGetInputSource(monitor, out uint current, out uint max))
        {
            lblCurrentValue.Text = $"0x{current:X2} (max 0x{max:X2})";
            SetStatus($"Current input code is 0x{current:X2}.", isError: false);
        }
        else
        {
            lblCurrentValue.Text = "(read failed)";
            SetStatus($"Could not read the current input (Win32 error {MonitorControl.LastWin32Error}). " +
                "Make sure DDC/CI is enabled in the monitor's OSD menu (often System > DDC/CI), and that " +
                "the cable runs directly from the PC to the monitor rather than through a hub, dock, or KVM switch, " +
                "which often block DDC/CI.", isError: true);
        }
    }

    private void SwitchTo(TextBox codeBox)
    {
        var monitor = SelectedMonitor;
        if (monitor == null)
            return;

        if (!TryParseCode(codeBox.Text, out uint code))
        {
            SetStatus("Enter the code as hex, e.g. 0x11.", isError: true);
            return;
        }

        SaveCurrentProfile();

        if (MonitorControl.TrySetInputSource(monitor, code))
            SetStatus($"Sent switch command (0x{code:X2}). If nothing changed, try the other code value below.", isError: false);
        else
            SetStatus($"Switch command failed (Win32 error {MonitorControl.LastWin32Error}). " +
                "Make sure DDC/CI is enabled in the monitor's OSD menu, and that the cable runs directly " +
                "from the PC to the monitor rather than through a hub, dock, or KVM switch.", isError: true);
    }

    private void SaveCurrentProfile()
    {
        var monitor = SelectedMonitor;
        if (monitor == null)
            return;

        if (TryParseCode(txtHdmi1Code.Text, out uint c1) && TryParseCode(txtHdmi2Code.Text, out uint c2) && TryParseCode(txtUsbcCode.Text, out uint c3))
        {
            var profile = _config.GetProfile(monitor.FriendlyName);
            profile.Hdmi1Code = (int)c1;
            profile.Hdmi2Code = (int)c2;
            profile.UsbcCode = (int)c3;
            _config.Save();
        }
    }

    private static bool TryParseCode(string text, out uint value)
    {
        text = text.Trim();
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            text = text[2..];
        return uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
    }

    private void SetStatus(string message, bool isError)
    {
        lblStatus.Text = message;
        lblStatus.ForeColor = isError ? Color.Firebrick : Color.DarkGreen;
    }
}
