<p align="center">
  <img src="assets/banner.png" alt="AirLink" width="128" />
</p>

<h1 align="center">AirLink</h1>

<p align="center">
  <strong>One click. AirPods connected.</strong><br>
  A tiny Windows tray app that does exactly one thing.
</p>

---

Connecting AirPods on Windows means `Settings > Bluetooth > Connect` — every single time. AirLink skips all of that. Left-click the tray icon, done.

No window. No installer. No configuration. Just a single `.exe` in your system tray.

- Left-click → connect your AirPods
- Right-click → options (startup, hotkey, quit)
- Adapts to light/dark theme automatically
- Icon shows connection state in real time

## Documentation

| | |
|---|---|
| [User Guide](docs/user-guide.md) | Download, install, and use AirLink |
| [Developer Guide](docs/developer-guide.md) | Build from source |
| [Why AirLink](docs/why-airlink.md) | Why this exists and what else I considered |

## Build

```shell
dotnet publish src/AirLink/AirLink.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

Output: `src/AirLink/bin/Release/net10.0-windows10.0.19041.0/win-x64/publish/AirLink.exe`

## License

[MIT](LICENSE) — Luis Palacios Derqui
