Corsair Headset Battery Tray
A lightweight Windows system-tray battery indicator for the Corsair VOID ELITE Wireless headset connected through its USB dongle (VID_1B1C, PID_0A51). The app reads battery data using HeadsetControl. iCUE is not used or required.

Features
Displays the battery percentage directly in a compact colored tray tile.

Shows teal above 30%, yellow from 15% through 30%, red below 15%, and blue while charging.

Refreshes automatically every 30 seconds.

Offers Refresh now and Exit from the tray icon’s right-click menu.

Shows the exact percentage and charging status in the icon tooltip.

Displays a gray tile when battery data is unavailable.

Focuses on battery only; microphone position and mute state are not shown.

Installer
Download CorsairHeadsetBattery-Setup.exe from the repository’s Releases page and run it.

The installer places the app in your per-user Programs folder, creates a Start Menu shortcut, optionally creates a desktop shortcut, and offers to launch the app when setup finishes. Later, start it from the Corsair Headset Battery entry in the Start Menu. The app runs in the Windows notification area; if its icon isn’t visible beside the clock, check the hidden-icons menu.

The app includes the .NET runtime, so you don’t need to install .NET separately. HeadsetControl is a separate prerequisite and is not included in the installer.

Requirements
Windows 10 or later, 64-bit.

Corsair VOID ELITE Wireless headset and USB dongle connected to the PC.

HeadsetControl for Windows, version 4.1.0 or compatible, 

| Battery state         | Tile color                     |
| --------------------- | ------------------------------ |
| More than 30%         | Teal                           |
| 15% to 30%, inclusive | Yellow                         |
| Less than 15%         | Red                            |
| Charging              | Blue, regardless of percentage |
| Battery unavailable   | Gray                           |
