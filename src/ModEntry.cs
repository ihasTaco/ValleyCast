using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace ValleyCast {
    public class ModEntry : Mod {
        public static ModConfig Config { get; set; } = null!;
        public static OBSController OBSController { get; private set; } = null!;
        public new static IModHelper Helper { get; private set; } = null!;
        public static IMonitor ModMonitor { get; private set; } = null!;

        public override void Entry(IModHelper helper)
        {
            Helper = helper;
            ModMonitor = this.Monitor;
            // Register the GameLaunched event
            Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched!;

            // Register events for loading a save or starting a new game
            Helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded!;
            Helper.Events.GameLoop.DayStarted += this.OnDayStarted!;

            // Register event for when the player returns to the title screen
            Helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle!;

        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // Load the config using the new ModConfig class
            Config = Helper.ReadConfig<ModConfig>();

            // Setup mod settings with GenericModConfigMenu
            ModSettings.Settings(ModManifest);

            // Check if this isn't the first time loading the mod
            if (Config.FirstLoad != true) {
                // If it isnt the first time loading the mod, then try to connect like normal
                // Initialize OBSController with the current config values when returning to the title screen
                OBSController = new OBSController(Config.OBSWebSocketIP, Config.OBSWebSocketPort, Config.Password);
            } else {
                // If it is, skip connecting to OBS and set FirstLoad to false
                Config.FirstLoad = false;
            }


        }
        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
        {
            return;
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) {
            // Check if OBS is connected
            // TODO: Add a check to see if the user is streaming, if they are then skip this check, or maybe just silence the initial "do you want to record" dialogue
            OBSController!.CheckRecordingStatus();
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e) {
            // This will be similar functionality to OnSaveLoaded but wont ask for permission to start recording (if obs is recording)
            // This function will check the config and see if the user has daily, weekly, or seasonal recording restarts set
            // if the current day matches the above settings then it will stop recording, and start it back up
            CheckRecordingStatus();
        }

        private static void CheckRecordingStatus() {
            // Check if OBS Studio is recording currently
            bool isRecording = OBSController!.IsRecording;

            if (!isRecording) {
                // Send a message to the player
                PlayerNotify.Notify(
                    "OBS is not recording, do you want to start recording?",
                    new List<Response> {
                        new ("1", "Yes. Lights. Camera. Action!"),
                        new ("2", "Nah, not feeling it right now")
                    },
                    answer =>
                    {
                        if (answer == "1")
                        {
                            ModMonitor.Log("Recording started.", LogLevel.Alert);
                            OBSController.StartRecording();
                        }
                        else if (answer == "2")
                        {
                            ModMonitor.Log("Recording not started.", LogLevel.Alert);
                        }
                    }
                );
            }
        }
    }
}