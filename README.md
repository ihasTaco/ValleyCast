# ValleyCast
ValleyCast is a utility mod that automatically controls OBS Studio based on in-game events in Stardew Valley.

## Compatibility
**Do not use this mod yet, unless for testing and non-critical streams or recordings!** Currently it works™, but there isnt much error handling, and may cause issues during a stream or recording
* Tested with OBS Studio v30.2.2
  * I have no idea if this will work out of the gat with Streamlabs OBS yet, will need more testing

## Getting Started
### Dependencies
* [SMAPI](https://smapi.io/)
* [Generic Mod Configuration Menu](https://www.nexusmods.com/stardewvalley/mods/5098)

### Installation and Setup
1. Configure OBS Studio:
   * Open OBS Studio.
   * Navigate to Tools > WebSocket Server Settings.
   * Click on Show Connection Info and set this window aside.
2. Launch Stardew Valley:
   * Ensure that [SMAPI](https://smapi.io/) and [Generic Mod Configuration Menu](https://www.nexusmods.com/stardewvalley/mods/5098) are installed.
   * Start Stardew Valley.
3. Set Up ValleyCast:
   * Once in the game, click the cog icon in the bottom left corner of the screen.
   * Select ValleyCast from the menu.
   * Enter the OBS connection details (IP and Port) from the WebSocket Server Settings window.
     * **Note**: For users recording and playing on the same device, the default IP and Port should work fine. If you're using a different device, input the OBS IP and Port information.
That's it! (For now, more settings and features will be added soon. See the [Upcoming Features](#Upcoming-Features) section below.)

## Features
* **Integrated Mod Settings**: Uses the Generic Mod Configuration Menu for easy setup and customization.
* **OBS Integration**: Connects Stardew Valley to OBS Studio to control various features.
  * Currently, it supports starting, stopping, and checking the status of recordings.
 
## Roadmap
### Upcoming Features
* **Improved Error Handling**: Currently, the mod functions best in ideal conditions. Avoid using it during critical streams or recordings until improvements are made.
* Automated Recording Control: Start/stop recordings based on in-game events such as:
  * Day start/end
  * Week start/end
  * Season changes
* **In-Game Notifications**: Alerts for connection issues or if OBS closes unexpectedly.
* **Scene Switching**: Automatically change scenes in OBS, triggered by in-game seasons or events.
* **Event Notifications**: In-game reminders for important dates such as birthdays, festivals, and order deadlines (configurable in mod settings).
* Overlay Integration:
  * Add a completion sidebar overlay in OBS and possibly in-game (similar to the LoZ Ship of Harkinian tracker).
    * For inspiration, see [this example image](https://i.ytimg.com/vi/M9rPRjzbvWM/maxresdefault.jpg?sqp=-oaymwEmCIAKENAF8quKqQMa8AEB-AH-CYAC0AWKAgwIABABGGMgZSg-MA8=&rs=AOn4CLBU9mEbOiqFz65SIZpgLIq19zimXQ).
  * Implement a keybind to show/hide the overlay
* **Dynamic Text Elements**: Enable changes to text elements (e.g., total days) in OBS without manual updates.
