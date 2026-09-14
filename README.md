# MonitorMux

A lightweight Windows tray app for switching a monitor's input source (e.g. HDMI 1 ↔ HDMI 2 ↔ USB-C/DP) without touching the monitor's physical buttons.

**Windows only.** MonitorMux is built on Windows Forms and the Windows Monitor Configuration API (`dxva2.dll`), neither of which exist on macOS or Linux. There is no Mac or Linux version, and none is planned — see [How it works](#how-it-works) for why.

## Features

- Switch monitor input source — HDMI 1, HDMI 2, or USB-C/DisplayPort — from the tray menu or a small window
- Works with any DDC/CI-capable monitor (it's a VESA standard, not brand-specific) — used day-to-day with both BenQ and Acer displays
- Auto-detects connected monitors and reads their real EDID model name (via WMI), not just the generic Windows driver label
- Learns and saves per-monitor input codes, since the exact VCP value for "HDMI 1" vs "HDMI 2" vs "USB-C/DP" isn't standardized across manufacturers
- Lives in the system tray; optional launch at Windows startup
- Single-instance guard, minimize/close to tray instead of quitting

## How it works

MonitorMux uses the Windows Monitor Configuration API (`dxva2.dll`) to send DDC/CI VCP commands directly to the monitor over the video cable — the same control channel the monitor's own on-screen menu uses internally. DDC/CI (part of the VESA MCCS standard) is supported by the vast majority of external monitors regardless of brand.

## Requirements

- Windows 10/11
- No .NET runtime install needed — the published exe is self-contained (the .NET 10 SDK is only needed if building from source)
- A monitor with DDC/CI enabled (usually in the OSD under a "System" menu)
- **A direct connection to the GPU.** Docking stations, USB hubs, and KVM switches very commonly block DDC/CI signaling even though the video image passes through fine — this is a hardware/firmware limitation of the dock, not something software can work around.

## Building

```
dotnet build -c Release
```

## Publishing a single-file exe

```
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Output lands in `bin/Release/net10.0-windows/win-x64/publish/MonitorMux.exe`.

## Usage

1. Launch `MonitorMux.exe`.
2. Pick your monitor from the dropdown.
3. Click **Read Current** to see its current input code.
4. Click **Switch to HDMI 1** / **Switch to HDMI 2** / **Switch to USB-C / DP**.

The default codes (`0x11` / `0x12` / `0x0F`) match the VESA spec, but some monitors use different values — this is especially common for USB-C, since MCCS never standardized a dedicated "USB-C" input code, so manufacturers reuse the DisplayPort code or pick their own. If a switch does nothing or selects the wrong port: use the monitor's own OSD to manually select that input, click **Read Current** to learn its real code, and type that value into the matching box here. Repeat for each input — the app remembers these per monitor.

Note that USB-C only works as a video input if the monitor's USB-C port supports DisplayPort Alt Mode and the connected cable/port on the PC side supports DP Alt Mode — plain USB-C data/charging ports won't carry a display signal at all.

Right-click the tray icon to switch inputs directly, or to toggle **Start with Windows**.

## Troubleshooting

- **"Read failed" / switch does nothing**: Almost always either DDC/CI is disabled in the monitor's OSD, or the monitor is connected through a dock/hub/KVM that blocks the DDC/CI channel. Try a direct cable from the PC/GPU to the monitor to confirm.
- **Wrong input gets selected**: Your monitor uses non-default VCP codes for its inputs — see the "Read Current" workflow above.

## License

[MIT](LICENSE)
