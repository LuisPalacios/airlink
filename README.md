<p align="center">
  <img src="assets/logo-connected-darktheme.svg" alt="AirLink" width="128" />
</p>

<h1 align="center">AirLink</h1>

<p align="center">
  <a href="https://github.com/LuisPalacios/airlink/actions/workflows/release.yaml">
    <img src="https://github.com/LuisPalacios/airlink/actions/workflows/release.yaml/badge.svg" alt="Release" />
  </a>
</p>

<p align="center">
  <strong>One click or Shortcut: AirPods connected</strong><br>
  A tiny app that does exactly one thing
</p>

---

Connecting AirPods on Windows means `Settings > Bluetooth > Connect` — every single time. This app skips all of that. No window. No installer. No configuration. Just a single light `.exe`.

- Left-click → connect your AirPods
- Right-click → options
- Adapts to light/dark theme automatically
- Icon shows connection state in real time

## Documentation

| | |
|---|---|
| [User Guide](docs/user-guide.md) | Download, install, and use AirLink |
| [Developer Guide](docs/developer-guide.md) | Build from source |
| [Why AirLink](docs/why-airlink.md) | Why did I do this |

## Build

```shell
dotnet publish src/AirLink/AirLink.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

Output: `src/AirLink/bin/Release/net10.0-windows10.0.19041.0/win-x64/publish/AirLink.exe`

## License

[MIT](LICENSE) — Luis Palacios Derqui
