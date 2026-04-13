# VibeVoskProject

Test project for [VibeVoskPlugin57](Plugins/VibeVoskPlugin57/README.md) — offline speech recognition plugin for Unreal Engine 5.7.

## Setup

1. Install VOSK SDK — see [Installation Guide](Plugins/VibeVoskPlugin57/Docs/INSTALLATION.md)
2. Place a language model into `Plugins/VibeVoskPlugin57/Binaries/Win64/Models/`
3. Open `MyTestGame.uproject` in Unreal Engine 5.7
4. See [Quick Start](Plugins/VibeVoskPlugin57/Docs/QUICK_START.md) for usage

## Build Scripts

| Script | Description |
|--------|-------------|
| `_BUILD_Development.bat` | Build Development configuration |
| `_BUILD_Shipping.bat` | Build Shipping configuration |
| `__BUILD.bat` | General build script |
| `__GEN_PROJECT_FILES.bat` | Regenerate project files |
| `__DEL_BINARIES.bat` | Clean binaries |

## Plugin Documentation

Full documentation is in [Plugins/VibeVoskPlugin57/Docs/](Plugins/VibeVoskPlugin57/Docs/).