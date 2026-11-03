# User Guide

## What is AirLink?

AirLink is a lightweight Windows utility that makes connecting your Apple AirPods effortless. Windows does not provide a quick way to connect already-paired Bluetooth audio devices — AirLink solves this with a shortcut or single click from the system tray.

## System Requirements

- Windows 10 version 2004 (May 2020 Update) or later
- Bluetooth adapter
- At least one Bluetooth audio device (e.g., Apple AirPods) paired with Windows — new AirPods or a new PC? Start with [Pairing your AirPods with Windows](#pairing-your-airpods-with-windows-first-time-only)

## Pairing your AirPods with Windows (first time only)

AirLink connects AirPods that Windows already knows about. If these are new AirPods, or this is a new PC, pair them once using the steps below. You never need to repeat this — from then on, AirLink handles connecting.

> Pairing with your PC does not unpair them from your iPhone, iPad or Mac. They stay paired to all of them.

### Step 1 — Open the Bluetooth "Add device" screen in Windows

**Windows 11:**

1. Press `Win + I` to open **Settings**.
2. Go to **Bluetooth & devices** and make sure **Bluetooth** is **On**.
3. Click **Add device** (top of the page).
4. In the "Add a device" dialog, choose **Bluetooth**.

**Windows 10:** **Settings > Devices > Bluetooth & other devices** → turn Bluetooth **On** → **Add Bluetooth or other device** → **Bluetooth**.

Leave this dialog open — Windows is now scanning for nearby devices.

### Step 2 — Put your AirPods in pairing mode

Put both AirPods in the charging case, **open the lid**, and keep the case close to your PC. Then, depending on your model:

| Model | How to enter pairing mode |
| --- | --- |
| **AirPods 4**, **AirPods 5**, **AirPods Pro 3** (no button on the case) | **Double-tap the front of the case**, right on the status light, until it **flashes white** |
| **AirPods 1st, 2nd, 3rd gen**, **AirPods Pro 1st gen**, **AirPods Pro 2** (Lightning or USB-C) | **Press and hold the setup button on the back of the case** until the status light **flashes white** |
| **AirPods Max** | **Press and hold the noise control button** until the status light **flashes white** |

Not sure which one you have? Look at the back of the case: if there is a small round button, use the "press and hold" method; if the back is smooth, use the double-tap.

A **flashing white** light means the AirPods are ready to pair. If it flashes **amber** instead, close the lid, wait a few seconds, and try again.

### Step 3 — Select your AirPods in Windows

1. Back in the "Add a device" dialog, wait for your AirPods to appear (e.g., "Alex's AirPods"). Some models may briefly show up as **Headphones** before the real name loads.
2. Click them. Windows pairs and connects them.
3. When you see **Your device is ready to go!**, click **Done**.

Your AirPods now appear under **Bluetooth & devices**. You can close Settings — you won't need it again. Continue with [Installation](#installation) and let AirLink do the connecting from now on.

> **The AirPods never show up in the list?** On a new or freshly reinstalled PC this is almost always a missing Bluetooth driver. See [AirPods never appear when pairing](#airpods-never-appear-when-pairing-new-pc).

## Installation

AirLink is a portable application — no installer required.

1. Download `AirLink.exe` from the [GitHub Releases](https://github.com/LuisPalacios/airlink/releases) page.
2. Place it anywhere you like (e.g., `C:\bin\AirLink.exe`).
3. Double-click to run.

> **Windows SmartScreen warning:** Since AirLink is not code-signed, Windows may show a "Windows protected your PC" dialog the first time you run it. Click **More info** → **Run anyway** to proceed. This only happens once per download.

That's it. AirLink will appear in your system tray.

## How to Use

### Connecting Your Device

**Left-click** the AirLink tray icon. On the first click, a device picker dialog will show all your paired _audio Bluetooth devices_ — select yours and click OK. AirLink will remember your choice and connect automatically on subsequent clicks.

To switch to a different device later, right-click and select **Select Device**.

### Context Menu (Right-Click)

Right-click the tray icon to access these options:

| Option             | Description                                               |
| ------------------ | --------------------------------------------------------- |
| **Help**           | Shows app information, author, and GitHub link            |
| **Select Device**  | Choose which paired Bluetooth device to connect           |
| **Shortcut**       | Configure a global keyboard shortcut to toggle connection |
| **Notifications**  | Toggle balloon tip notifications on connect/disconnect    |
| **Run at Startup** | Toggle whether AirLink starts automatically with Windows  |
| **Exit**           | Closes AirLink completely                                 |

### Keyboard Shortcut

You can assign a global keyboard shortcut to toggle your AirPods connection from anywhere in Windows — no need to click the tray icon.

1. Right-click the tray icon and select **Shortcut**.
2. In the dialog, press your desired key combination (e.g., `Ctrl+Win+Shift+A`). You need at least one modifier key (Ctrl, Alt, Shift, or Win) plus a letter or number.
3. Check **Enable shortcut** and click **OK**.

The shortcut works system-wide, even when other applications are focused. The menu item will show the active shortcut (e.g., "Shortcut (Ctrl+Win+Shift+A)").

To disable the shortcut, open the dialog again and uncheck **Enable shortcut**.

### Icons

The tray icon changes based on two factors:

- **Connection state** — blue icon when connected, gray when disconnected
- **Windows theme** — adapts to your taskbar's light or dark mode setting

### First Launch

On first launch, AirLink automatically enables "Run at Startup" so it starts with Windows. You can toggle this anytime via the context menu.

> **Note:** If you move `AirLink.exe` to a different folder, just run it once from the new location. AirLink will detect the path change and update the startup registry entry automatically.

## Troubleshooting

### Notifications stay too long

AirLink shows brief notifications when connecting or disconnecting. The duration is controlled by Windows, not AirLink. To change it, go to **Settings > Accessibility > Visual effects** and adjust "Dismiss notifications after this amount of time."

You can also disable AirLink notifications entirely via the **Notifications** option in the right-click context menu.

### AirPods never appear when pairing (new PC)

**Symptom:** the AirPods case flashes white (pairing mode), Bluetooth is **On** in Windows, but the **Add a device** dialog keeps searching and never lists them. Often it doesn't list _any_ device at all — not even phones or TVs around you.

**Cause:** Windows ships with a fallback Bluetooth driver called **Generic Bluetooth Adapter**. It's good enough to turn the radio on, so everything _looks_ fine, but many Bluetooth chips — especially the Wi-Fi + Bluetooth combo cards built into motherboards and laptops (Realtek, MediaTek, Intel) — need their manufacturer's driver to load the chip's firmware. Without it, the radio is on but effectively deaf. This is common on a brand-new PC or after a clean Windows install, before all drivers are in place.

**How to check (30 seconds):**

1. Right-click **Start** → **Device Manager**.
2. Expand **Bluetooth**.
3. Look at the adapter's name:
   - **Generic Bluetooth Adapter** → you're missing the manufacturer's driver. Continue below.
   - A vendor name (e.g., **Realtek Bluetooth Adapter**, **Intel(R) Wireless Bluetooth(R)**, **MediaTek Bluetooth Adapter**) → the driver is fine; the problem is elsewhere (see [AirPods not found](#airpods-not-found)).

**Fix:**

1. Find your exact motherboard or laptop model (**Win + R** → `msinfo32` → look at **BaseBoard Product** or **System Model**).
2. Go to the **manufacturer's support page** for that model (ASUS, MSI, Gigabyte, Dell, Lenovo, HP…) → **Drivers** → your Windows version → **Bluetooth**, and install it. While you're there, install any other missing drivers (chipset, Wi-Fi, audio). Avoid third-party "driver updater" sites.
3. **Shut down** the PC completely and power it back on. A plain restart is not always enough — the Bluetooth chip loads its firmware when it powers up.
4. Check Device Manager again: the adapter should now show the vendor name instead of "Generic Bluetooth Adapter".
5. Repeat [Pairing your AirPods with Windows](#pairing-your-airpods-with-windows-first-time-only) — this time they appear within seconds.

> **Real-world example.** A brand-new workstation with an onboard Wi-Fi 7 + Bluetooth combo card. Bluetooth was **On**, every Bluetooth service was running, and Windows showed no errors — yet AirPods 4 in pairing mode never appeared. A 30-second scan heard **zero** devices, Classic or Low Energy, even with phones nearby. Device Manager showed **Generic Bluetooth Adapter**: the Wi-Fi half of the card had its vendor driver, but the Bluetooth half didn't. After installing the motherboard maker's Bluetooth driver and power-cycling, the adapter came back as **Realtek Bluetooth Adapter**, its reported Bluetooth version went up one step (a sign that the real firmware had loaded), and the AirPods paired on the first try.
>
> **Lesson:** "Bluetooth is on" doesn't mean "Bluetooth works". On a new PC, check the adapter's driver name before blaming the AirPods.

<details>
<summary>For power users: check from PowerShell</summary>

```powershell
Get-PnpDevice -Class Bluetooth -PresentOnly |
  Where-Object InstanceId -match '^(USB|PCI)' |
  ForEach-Object {
    Get-PnpDeviceProperty -InstanceId $_.InstanceId -KeyName DEVPKEY_Device_DriverProvider, DEVPKEY_Device_DriverInfPath, DEVPKEY_Device_LowerFilters |
      Select-Object KeyName, Data
  }
```

**Bad:** `DriverProvider` = `Microsoft`, `DriverInfPath` = `bth.inf`, and no `LowerFilters`. **Good:** a vendor `DriverProvider`, an `oemNN.inf` file and, for Realtek chips, a `RtkBtFilter…` lower filter.

</details>

### AirPods not found

- Make sure your AirPods are paired in **Settings > Bluetooth & devices**. If they aren't listed there, follow [Pairing your AirPods with Windows](#pairing-your-airpods-with-windows-first-time-only).
- Open the AirPods case near your PC before clicking the tray icon.
- Ensure Bluetooth is enabled on your PC.

### Connection fails

- Close and reopen the AirPods case, then try again.
- Remove the AirPods in **Settings > Bluetooth & devices** (click **⋯** next to them → **Remove device**), then pair them again following [Pairing your AirPods with Windows](#pairing-your-airpods-with-windows-first-time-only).
- Restart the Bluetooth adapter: toggle Bluetooth off and on in Windows Settings.

### Icon not visible in the system tray

Windows may hide tray icons by default. Click the **^** arrow in the taskbar to find AirLink, then drag it to the visible area.

## Uninstall

1. Right-click the tray icon and select **Exit**.
2. If "Run at Startup" was enabled, it was stored in the Windows Registry at `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`. Exiting the app and deleting the EXE is sufficient — or disable "Run at Startup" before exiting.
3. Delete `AirLink.exe`.
