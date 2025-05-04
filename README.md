# ValleyCast

ValleyCast is a utility mod that automatically controls OBS Studio based on in-game events in Stardew Valley.  
Supports dynamic overlays, text updates, recording triggers, and more—with full config control.

🎥 Available on NexusMods: [ValleyCast on Nexus](https://www.nexusmods.com/stardewvalley/mods/26947)

---

## Compatibility

- ✅ Tested with **OBS Studio v31.0.3**
- ✅ Tested with **Stardew Valley 1.6.15**
- 🧪 Not yet tested with Streamlabs OBS
- 🪟 Windows only for now (May work with *unix & mac devices but haven't tested yet)

---

## Getting Started

### 🔗 Dependencies

- [SMAPI](https://smapi.io/)
- [Generic Mod Configuration Menu (GMCM)](https://www.nexusmods.com/stardewvalley/mods/5098)

### 🛠️ Installation & Setup

1. **Configure OBS Studio**
   - Open OBS Studio
   - Go to `Tools > WebSocket Server Settings`
   - Click **Show Connection Info** and keep it open

2. **Launch Stardew Valley**
   - Start the game using SMAPI
   - Make sure GMCM is installed

3. **Configure ValleyCast In-Game**
   - Click the ⚙️ cog icon in the bottom-left of the screen
   - Select **ValleyCast**
   - Enter the OBS connection info (IP and Port)
     - Default values work if OBS is running on the same device

---

## ✅ Features (v0.1.6)

- **NEW! | 🔄 Hot Reload Config**  
  Reload your `config.json` in-game via F5 or a config menu toggle

- **NEW! | 📝 OBS Text Source Updates**  
  Automatically updates OBS text sources with in-game info like day, season, year (fully customizable)

- **🎛 Integrated Mod Settings**  
  Uses GMCM to manage all mod config in-game

- **🎥 OBS Recording Control**  
  Start/stop recordings based on events like save load or day-end

- **🔔 In-Game Notifications**  
  Get alerts when OBS connects, disconnects, or config is reloaded

- **🪵 Robust Logging**  
  Logs all OBS communication and mod activity to SMAPI console

---

## 🐞 Known Issues

- OBS `IsRecording` check is unreliable in OBS 30+ and may not reflect the true recording state
- Scene switching + weekly/monthly event triggers exist but aren't exposed in the UI yet
- Not tested on macOS or Streamlabs OBS

---

## 🚧 Roadmap

### Upcoming Features

- [ ] **Configurable Week-End & Month-End Recording Toggles**  
  (Feature implemented, just needs UI setting)

- [ ] **OBS Scene Switching**  
  Automatically change scenes based on in-game season, event, or time

- [ ] **Event Notification System**  
  Reminders for birthdays, festivals, quest deadlines, etc.

- [ ] **Overlay Integration (Tracker View)**  
  Add a live completion sidebar overlay in OBS  
  Inspired by this tracker: [Example Image](https://i.ytimg.com/vi/M9rPRjzbvWM/maxresdefault.jpg)

- [ ] **Dynamic Text Stats**  
  Display gold earned, days played, crops grown, etc. as live OBS overlays

---

## ❤️ Developer Note

I'm currently trying out some new techniques for managing ADHD and avoiding burnout. That means updates may be paced out more intentionally. Expect the next one in about a week—thanks for your patience and support!
