# KaiserVox

<p align="center">
  <img src="src/EasyDictate/Resources/icon.svg" alt="KaiserVox" width="96" />
</p>

<p align="center">
  <strong>GPU-accelerated local voice dictation for Windows.</strong><br>
  Fast. Private. Cyber-clean.
</p>

<p align="center">
  <img alt=".NET 8" src="https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet&logoColor=white">
  <img alt="Windows" src="https://img.shields.io/badge/Windows-10%2B-0078D6?logo=windows&logoColor=white">
  <img alt="CUDA" src="https://img.shields.io/badge/NVIDIA-CUDA-76B900?logo=nvidia&logoColor=white">
  <img alt="License" src="https://img.shields.io/badge/License-MIT-0f172a">
</p>

---

## What is KaiserVox?

**KaiserVox** is an enhanced fork of EasyDictate focused on one thing: local dictation that feels instant and stays private.

No cloud hop. No subscriptions. No telemetry wall. Just your mic, your machine, your models.

### Highlights

- NVIDIA **CUDA GPU acceleration** (Whisper runtime CUDA backend)
- **Multi-language transcription** (`auto` by default)
- **Dynamic model scanner** (`*.bin` in your models folder)
- **Model selection dropdown** in Settings
- **Dark UI** optimized for low-glare workflows
- 100% local and free

---

## Privacy

KaiserVox runs fully on your machine.

- Audio is processed locally
- Models are local files
- No external transcription API
- No account required

---

## Architecture

```text
Push-to-Talk Hotkey
        │
        ▼
AudioCaptureService (NAudio)
        │
        ▼
TranscriptionService (Whisper.net + CUDA runtime)
        │
        ▼
OutputService (paste active window / clipboard)
```

Core modules:

- `SettingsService` - config + startup + data migration
- `ModelManager` - model download + model discovery + selected model resolution
- `DictationCoordinator` - workflow orchestration (record -> transcribe -> output)
- WPF Views - first-run wizard, settings, overlay, tray state

### Data paths

Current app data root:

`%APPDATA%\KaiserVox`

Includes:

- `config.json`
- `models\*.bin`

Migration is built in: if `%APPDATA%\EasyDictate` exists and `%APPDATA%\KaiserVox` does not, settings/models are copied automatically on first start.

---

## Comparison

| Feature | KaiserVox | EasyDictate (original) | Typical Cloud Dictation |
|---|---|---|---|
| Local-only processing | Yes | Yes | Usually No |
| NVIDIA CUDA acceleration | Yes | No | N/A |
| Dynamic model discovery | Yes | Limited/default model flow | N/A |
| Settings model picker | Yes | Limited | N/A |
| Multi-language support | Yes (`auto`) | Basic/limited | Usually Yes |
| Privacy control | Full local control | Local-first | Provider-dependent |
| Cost | Free | Free | Often subscription/usage-based |

---

## Build

From repo root:

```powershell
cd src/EasyDictate
"C:\Program Files\dotnet\dotnet.exe" build
"C:\Program Files\dotnet\dotnet.exe" publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

Published executable:

`src/EasyDictate/bin/Release/net8.0-windows/win-x64/publish/KaiserVox.exe`

---

## Part of the Kaiser ecosystem

KaiserVox is a sibling project in the Kaiser ecosystem (alongside **Kaisercloud**), built for users who want local-first tooling with zero fluff.

---

## Credits

- Original project: **EasyDictate**
- Whisper runtime: `Whisper.net`
- Audio capture: `NAudio`

Respect to the original foundation - KaiserVox builds on top of that work.

---

## License

MIT
