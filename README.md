# MonitorMux

A lightweight Windows tray app for switching a monitor's input source (e.g. HDMI 1 ↔ HDMI 2) without touching the monitor's physical buttons.

## Features

- Switch monitor input source from the tray menu or a small window
- Works with any DDC/CI-capable monitor (it's a VESA standard, not brand-specific) — used day-to-day with both BenQ and Acer displays
- Auto-detects connected monitors and reads their real EDID model name (via WMI), not just the generic Windows driver label
- Learns and saves per-monitor input codes, since the exact VCP value for "HDMI 1" vs "HDMI 2" isn't standardized across manufacturers
- Lives in the system tray; optional launch at Windows startup
- Single-instance guard, minimize/close to tray instead of quitting

## How it works

MonitorMux uses the Windows Monitor Configuration API (`dxva2.dll`) to send DDC/CI VCP commands directly to the monitor over the video cable — the same control channel the monitor's own on-screen menu uses internally. DDC/CI (part of the VESA MCCS standard) is supported by the vast majority of external monitors regardless of brand.

## Requirements

- Windows 10/11
- .NET 10 Desktop Runtime (or the SDK, if building from source)
- A monitor with DDC/CI enabled (usually in the OSD under a "System" menu)
- **A direct connection to the GPU.** Docking stations, USB hubs, and KVM switches very commonly block DDC/CI signaling even though the video image passes through fine — this is a hardware/firmware limitation of the dock, not something software can work around.

## Building

```
dotnet build -c Release
```

## Publishing a single-file exe

```
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

Output lands in `bin/Release/net10.0-windows/win-x64/publish/MonitorMux.exe`.

## Usage

1. Launch `MonitorMux.exe`.
2. Pick your monitor from the dropdown.
3. Click **Read Current** to see its current input code.
4. Click **Switch to HDMI 1** / **Switch to HDMI 2**.

The default codes (`0x11` / `0x12`) match the VESA spec, but some monitors use different values. If a switch does nothing or selects the wrong port: use the monitor's own OSD to manually select HDMI 1, click **Read Current** to learn its real code, and type that value into the HDMI 1 box here. Repeat for HDMI 2 — the app remembers these per monitor.

Right-click the tray icon to switch inputs directly, or to toggle **Start with Windows**.

## Troubleshooting

- **"Read failed" / switch does nothing**: Almost always either DDC/CI is disabled in the monitor's OSD, or the monitor is connected through a dock/hub/KVM that blocks the DDC/CI channel. Try a direct cable from the PC/GPU to the monitor to confirm.
- **Wrong input gets selected**: Your monitor uses non-default VCP codes for its inputs — see the "Read Current" workflow above.

## License

No license has been chosen yet — all rights reserved by default until one is added.
