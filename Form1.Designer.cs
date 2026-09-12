namespace MonitorMux;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private Label lblMonitor;
    private ComboBox cmbMonitors;
    private Button btnRefresh;
    private Label lblCurrentCaption;
    private Label lblCurrentValue;
    private Button btnReadCurrent;
    private GroupBox grpSwitch;
    private Label lblHdmi1;
    private TextBox txtHdmi1Code;
    private Button btnHdmi1;
    private Label lblHdmi2;
    private TextBox txtHdmi2Code;
    private Button btnHdmi2;
    private Label lblUsbc;
    private TextBox txtUsbcCode;
    private Button btnUsbc;
    private Label lblHint;
    private Label lblStatus;

    private void InitializeComponent()
    {
        lblMonitor = new Label();
        cmbMonitors = new ComboBox();
        btnRefresh = new Button();
        lblCurrentCaption = new Label();
        lblCurrentValue = new Label();
        btnReadCurrent = new Button();
        grpSwitch = new GroupBox();
        lblHdmi1 = new Label();
        txtHdmi1Code = new TextBox();
        btnHdmi1 = new Button();
        lblHdmi2 = new Label();
        txtHdmi2Code = new TextBox();
        btnHdmi2 = new Button();
        lblUsbc = new Label();
        txtUsbcCode = new TextBox();
        btnUsbc = new Button();
        lblHint = new Label();
        lblStatus = new Label();
        grpSwitch.SuspendLayout();
        SuspendLayout();

        Font = new Font("Segoe UI", 10F);

        // lblMonitor
        lblMonitor.AutoSize = true;
        lblMonitor.Location = new Point(16, 22);
        lblMonitor.Text = "Monitor:";

        // cmbMonitors
        cmbMonitors.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbMonitors.Location = new Point(100, 18);
        cmbMonitors.Size = new Size(400, 28);

        // btnRefresh
        btnRefresh.AutoSize = true;
        btnRefresh.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        btnRefresh.MinimumSize = new Size(110, 32);
        btnRefresh.Location = new Point(508, 16);
        btnRefresh.Text = "Refresh";
        btnRefresh.UseVisualStyleBackColor = true;

        // lblCurrentCaption
        lblCurrentCaption.AutoSize = true;
        lblCurrentCaption.Location = new Point(16, 66);
        lblCurrentCaption.Text = "Current input (VCP 0x60):";

        // lblCurrentValue
        lblCurrentValue.AutoSize = true;
        lblCurrentValue.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        lblCurrentValue.Location = new Point(260, 64);
        lblCurrentValue.Text = "(unread)";

        // btnReadCurrent
        btnReadCurrent.AutoSize = true;
        btnReadCurrent.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        btnReadCurrent.MinimumSize = new Size(140, 32);
        btnReadCurrent.Location = new Point(478, 58);
        btnReadCurrent.Text = "Read Current";
        btnReadCurrent.UseVisualStyleBackColor = true;

        // grpSwitch
        grpSwitch.Controls.Add(lblHdmi1);
        grpSwitch.Controls.Add(txtHdmi1Code);
        grpSwitch.Controls.Add(btnHdmi1);
        grpSwitch.Controls.Add(lblHdmi2);
        grpSwitch.Controls.Add(txtHdmi2Code);
        grpSwitch.Controls.Add(btnHdmi2);
        grpSwitch.Controls.Add(lblUsbc);
        grpSwitch.Controls.Add(txtUsbcCode);
        grpSwitch.Controls.Add(btnUsbc);
        grpSwitch.Controls.Add(lblHint);
        grpSwitch.Location = new Point(16, 108);
        grpSwitch.Size = new Size(632, 344);
        grpSwitch.Text = "Switch Input";

        // lblHdmi1
        lblHdmi1.AutoSize = true;
        lblHdmi1.Location = new Point(20, 44);
        lblHdmi1.Text = "HDMI 1 code:";

        // txtHdmi1Code
        txtHdmi1Code.Location = new Point(180, 40);
        txtHdmi1Code.Size = new Size(90, 27);
        txtHdmi1Code.Text = "0x11";

        // btnHdmi1
        btnHdmi1.AutoSize = true;
        btnHdmi1.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        btnHdmi1.MinimumSize = new Size(330, 40);
        btnHdmi1.Location = new Point(290, 36);
        btnHdmi1.Text = "Switch to HDMI 1";
        btnHdmi1.UseVisualStyleBackColor = true;

        // lblHdmi2
        lblHdmi2.AutoSize = true;
        lblHdmi2.Location = new Point(20, 96);
        lblHdmi2.Text = "HDMI 2 code:";

        // txtHdmi2Code
        txtHdmi2Code.Location = new Point(180, 92);
        txtHdmi2Code.Size = new Size(90, 27);
        txtHdmi2Code.Text = "0x12";

        // btnHdmi2
        btnHdmi2.AutoSize = true;
        btnHdmi2.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        btnHdmi2.MinimumSize = new Size(330, 40);
        btnHdmi2.Location = new Point(290, 88);
        btnHdmi2.Text = "Switch to HDMI 2";
        btnHdmi2.UseVisualStyleBackColor = true;

        // lblUsbc
        lblUsbc.AutoSize = true;
        lblUsbc.Location = new Point(20, 148);
        lblUsbc.Text = "USB-C / DP code:";

        // txtUsbcCode
        txtUsbcCode.Location = new Point(180, 144);
        txtUsbcCode.Size = new Size(90, 27);
        txtUsbcCode.Text = "0x0F";

        // btnUsbc
        btnUsbc.AutoSize = true;
        btnUsbc.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        btnUsbc.MinimumSize = new Size(330, 40);
        btnUsbc.Location = new Point(290, 140);
        btnUsbc.Text = "Switch to USB-C / DP";
        btnUsbc.UseVisualStyleBackColor = true;

        // lblHint
        lblHint.Location = new Point(20, 202);
        lblHint.Size = new Size(592, 126);
        lblHint.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
        lblHint.ForeColor = SystemColors.GrayText;
        lblHint.Text = "If a switch does nothing or picks the wrong port, use the monitor's own OSD " +
            "menu to select HDMI 1 by hand, then click \"Read Current\" above to learn its " +
            "real code, and type that value into the box here. Repeat for HDMI 2 and USB-C / DP.";

        // lblStatus
        lblStatus.Location = new Point(16, 466);
        lblStatus.Size = new Size(632, 70);
        lblStatus.Text = "";

        // Form1
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(664, 550);
        Controls.Add(lblMonitor);
        Controls.Add(cmbMonitors);
        Controls.Add(btnRefresh);
        Controls.Add(lblCurrentCaption);
        Controls.Add(lblCurrentValue);
        Controls.Add(btnReadCurrent);
        Controls.Add(grpSwitch);
        Controls.Add(lblStatus);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "MonitorMux";
        grpSwitch.ResumeLayout(false);
        grpSwitch.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
