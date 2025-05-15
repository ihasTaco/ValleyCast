# ValleyCast

ValleyCast is a utility mod that automatically controls OBS Studio based on in-game events in Stardew Valley.  
Supports dynamic overlays, text updates, recording triggers, and more, with full config control.

🎥 Available on NexusMods: [ValleyCast on Nexus](https://www.nexusmods.com/stardewvalley/mods/26947)

---

## Compatibility

- ✅ Tested with **OBS Studio v31.0.3**
- ✅ Tested with **Stardew Valley 1.6.15**
- 🧪 Not yet tested with Streamlabs OBS
- 🪟 Windows only (may work on Unix/macOS but untested)

---

## Getting Started

### Dependencies

- [SMAPI](https://smapi.io/)
- [Generic Mod Configuration Menu (GMCM)](https://www.nexusmods.com/stardewvalley/mods/5098)

### Installation & Setup

1. **Configure OBS Studio**
   - Open OBS
   - Go to `Tools > WebSocket Server Settings`
   - Click **Show Connection Info** and keep it open

2. **Launch Stardew Valley**
   - Start the game via SMAPI
   - Ensure GMCM is installed

3. **Configure ValleyCast In-Game**
   - Click the gear icon in the bottom-left corner
   - Select **ValleyCast**
   - Enter the OBS IP and Port (defaults are fine for same-device use)

---

## Features

### New in v0.1.8
- **NEW:** Fixed a bug where the game would hang if OBS wasn’t open
- **NEW:** Fixed issue where OBS scenes/text wouldn’t update when loading a save
- **NEW:** Fixed recording prompt suppression when OBS wasn’t connected
- **NEW:** Modularized code for better stability and extension
- **NEW:** Scene switching based on:
  - In-game seasons
  - Fallback/default when no scene matches

### Core Features
- Integrated Mod Settings via GMCM
- OBS WebSocket integration for automated control
- Week-end and month-end recording logic
- Smarter recording prompts (only show when truly needed)
- Hot-reload support for `config.json` (F5 or in-menu toggle)
- OBS text source updates (season, date, year, fully customizable)
- Automated recording control for:
  - Save load
  - New day/week/month
- In-game notifications for OBS connection and recording status
- Robust logging to SMAPI console

---

## Roadmap

### Known Issues
- On save load, the mod currently retries OBS connection up to 5 times. This will be reduced to 1 retry in the next patch.

### Upcoming Features

- **Event-Triggered Scene Switching**
  - Festivals, birthdays, etc.

- **Streaming-Safe Mode**
  - Suppress logs and notifications while live
  - Toggleable in config or settings

- **Revamped In-Game Mod Menu**
  - Current layout is clunky and will be redesigned for clarity

- **Overlay Integration (experimental)**
  - Visual tracker in OBS for player progress (e.g. quests, goals)
  - Toggleable via hotkey or menu
  - Inspired by overlays like Ship of Harkinian
    - [Example overlay](https://i.ytimg.com/vi/M9rPRjzbvWM/maxresdefault.jpg)

- **Dynamic Text Elements**
  - More advanced OBS text control (e.g., live quest status)

- **Optional Default Scene Graphics (?)**
  - Provide a downloadable pack of simple seasonal-themed OBS scene assets (e.g. spring, summer, fall, winter)
  - Useful for quick setup or inspiration, can be customized or replaced
  - Will include matching text overlays for common game info (e.g., Day, Season, Year)