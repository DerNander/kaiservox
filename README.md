# KaiserVox

<p align="center">
  <img src="src/EasyDictate/Resources/icon.svg" alt="KaiserVox" width="128" />
</p>

<p align="center">
  <strong>Local voice dictation. GPU-accelerated. Zero cloud.</strong>
</p>

<p align="center">
  <a href="https://github.com/DerNander/kaiservox/releases/latest"><img alt="Latest Release" src="https://img.shields.io/github/v/release/DerNander/kaiservox?style=flat-square&color=0f172a&label=Release"></a>
  <img alt=".NET 8" src="https://img.shields.io/badge/.NET_8-512BD4?style=flat-square&logo=dotnet&logoColor=white">
  <img alt="Windows" src="https://img.shields.io/badge/Windows_10%2B-0078D6?style=flat-square&logo=windows&logoColor=white">
  <img alt="CUDA" src="https://img.shields.io/badge/NVIDIA_CUDA-76B900?style=flat-square&logo=nvidia&logoColor=white">
  <img alt="License" src="https://img.shields.io/badge/MIT-0f172a?style=flat-square&label=License">
</p>

---

## What is KaiserVox?

Hold a key. Speak. Text appears. That's it.

KaiserVox runs [OpenAI Whisper](https://github.com/openai/whisper) entirely on your machine — no cloud, no API keys, no subscriptions, no data leaving your device. Ever.

Built for people who type too much and trust cloud services too little.

---

## Features

🚀 **NVIDIA CUDA GPU Acceleration** — Near-zero latency on modern GPUs. Tested with RTX 2080 Super + `large-v3-turbo`: transcription feels instant.

🌍 **Multi-Language** — Auto-detect by default. Speak German, English, Japanese, whatever — Whisper handles it.

🧠 **Dynamic Model Scanner** — Drop any Whisper `.bin` model into your models folder. It shows up in Settings automatically. Swap between `base`, `small`, `medium`, `large-v3`, `large-v3-turbo` with a click.

🎨 **Dark UI** — Clean, minimal dark theme. No eye-burn during late-night sessions.

🔒 **100% Local & Private** — Your voice never leaves your PC. No accounts, no telemetry, no cloud processing.

💸 **Free. Forever.** — Open source, MIT licensed. No trial, no paywall.

---

## How It Works

```
Hold Hotkey (Alt+Space)  →  Speak  →  Release  →  Text in Clipboard  →  Paste anywhere
```

1. **Hold** the hotkey (default: `Alt + Space`, customizable)
2. **Speak** — a minimal overlay shows "Listening..."
3. **Release** — audio is transcribed locally via Whisper
4. **Paste** — text is copied to clipboard, ready for `Ctrl+V`

Works in any app — Discord, browsers, code editors, Word, everywhere.

---

## Quick Start

1. Download `KaiserVox.exe` from [Releases](https://github.com/DerNander/kaiservox/releases/latest)
2. Run it — first launch downloads the default Whisper model (~140MB)
3. Hold `Alt + Space`, speak, release
4. Paste with `Ctrl+V`

> **GPU users:** KaiserVox automatically uses your NVIDIA GPU via CUDA if available. No configuration needed.

### Models

Models live in `%APPDATA%\KaiserVox\models\`. Download any Whisper GGML model and drop it in:

| Model | Size | Speed | Quality | Best for |
|-------|------|-------|---------|----------|
| `ggml-base.en.bin` | 140 MB | ⚡⚡⚡ | Good | Quick English dictation |
| `ggml-small.bin` | 460 MB | ⚡⚡ | Better | Multi-language, daily use |
| `ggml-large-v3-turbo.bin` | 1.6 GB | ⚡⚡ | Excellent | Best balance speed/quality |
| `ggml-large-v3.bin` | 3.0 GB | ⚡ | Best | Maximum accuracy |

Select your preferred model in **Settings → Speech Model**.

---

## Performance

| Setup | Transcription Speed | Notes |
|-------|-------------------|-------|
| RTX 2080 Super + large-v3-turbo | **~instant** | Feels like real-time |
| RTX 3060 + large-v3 | ~25x realtime | 1 min audio ≈ 2.4 sec |
| CPU-only + base.en | ~5x realtime | Still usable for short dictation |

> GPU acceleration requires an NVIDIA GPU with CUDA support. AMD/Intel GPUs fall back to CPU mode.

---

## Architecture

```
┌────────────────────────────────────────────┐
│              KaiserVox.exe                 │
├────────────────────────────────────────────┤
│  System Tray    │    Overlay Window        │
├────────────────────────────────────────────┤
│  Global Hotkey (Win32 RegisterHotKey)      │
├────────────────────────────────────────────┤
│  Audio Capture (NAudio / WASAPI 16kHz)     │
├────────────────────────────────────────────┤
│  Whisper.net + CUDA Runtime                │
├────────────────────────────────────────────┤
│  Clipboard / SendInput                     │
└────────────────────────────────────────────┘
```

### Tech Stack

| Component | Technology |
|-----------|------------|
| Framework | .NET 8 / WPF |
| Speech Engine | [Whisper.net](https://github.com/sandrohanea/whisper.net) (whisper.cpp bindings) |
| GPU Backend | [Whisper.net.Runtime.Cuda](https://www.nuget.org/packages/Whisper.net.Runtime.Cuda) |
| Audio | [NAudio](https://github.com/naudio/NAudio) (WASAPI) |
| Tray Icon | [Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon) |

### Data & Config

```
%APPDATA%\KaiserVox\
├── config.json          # Settings (hotkey, mic, output mode)
└── models\
    └── ggml-*.bin       # Whisper models (auto-discovered)
```

> **Upgrading from EasyDictate?** KaiserVox auto-migrates your settings and models on first launch.

---

## Comparison

| | KaiserVox | Cloud Dictation | Windows Dictation | Wispr Flow |
|---|:---:|:---:|:---:|:---:|
| Local processing | ✅ | ❌ | ❌ | ❌ |
| GPU accelerated | ✅ | N/A | ❌ | N/A |
| Multi-language | ✅ | ✅ | Limited | ✅ |
| Model selection | ✅ | ❌ | ❌ | ❌ |
| Privacy | ✅ Full | ⚠️ Cloud | ⚠️ Microsoft | ⚠️ Cloud |
| Offline capable | ✅ | ❌ | ❌ | ❌ |
| Price | **Free** | $10-15/mo | Free | $12/mo |

---

## Build from Source

```powershell
# Clone
git clone https://github.com/DerNander/kaiservox.git
cd kaiservox

# Build
dotnet build src/EasyDictate/EasyDictate.csproj

# Publish (self-contained single-file)
dotnet publish src/EasyDictate/EasyDictate.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

Output: `src/EasyDictate/bin/Release/net8.0-windows/win-x64/publish/KaiserVox.exe`

### Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows 10+ (64-bit)
- Optional: NVIDIA GPU with CUDA for GPU acceleration

---

## System Requirements

- **OS:** Windows 10 or 11 (64-bit)
- **RAM:** 4 GB minimum (8 GB recommended)
- **Disk:** ~200 MB (app + base model)
- **GPU:** Any NVIDIA GPU with CUDA support (optional, falls back to CPU)
- **Mic:** Any Windows-compatible microphone

---

## Part of the Kaiser Ecosystem

KaiserVox sits alongside [Kaisercloud](https://github.com/DerNander) — local-first tools built for privacy, performance, and zero bullshit.

---

## Credits

KaiserVox is an enhanced fork of [EasyDictate](https://github.com/EasyDictate/EasyDictate) by the original authors. Built on top of excellent open-source work:

- [Whisper.net](https://github.com/sandrohanea/whisper.net) — C# bindings for whisper.cpp
- [whisper.cpp](https://github.com/ggerganov/whisper.cpp) — High-performance Whisper inference
- [OpenAI Whisper](https://github.com/openai/whisper) — The model that started it all
- [NAudio](https://github.com/naudio/NAudio) — .NET audio library
- [Hardcodet WPF NotifyIcon](https://github.com/hardcodet/wpf-notifyicon) — System tray for WPF

---

## License

[MIT](LICENSE) — do whatever you want with it.
