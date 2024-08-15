using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using GenericModConfigMenu;

namespace ValleyCast {
    public class ModEntry : Mod {
        public static ModConfig Config { get; set; } = null!;
        public static OBSController OBSController { get; private set; } = null!;
        public new static IModHelper Helper { get; private set; } = null!;

        public override void Entry(IModHelper helper)
        {
            Helper = helper;
            // Register the GameLaunched event
            Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched!;

            // Register events for loading a save or starting a new game
            Helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded!;
            Helper.Events.GameLoop.DayStarted += this.OnDayStarted!;

        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // Load the config using the new ModConfig class
            Config = Helper.ReadConfig<ModConfig>();

            ModSettings.Settings(ModManifest);

            if (Config.FirstLoad != false) {
                // Initialize OBSController with the current config values when returning to the title screen
                OBSController = new OBSController(Config.OBSWebSocketIP, Config.OBSWebSocketPort, Config.Password, this.Monitor);
            } else {
                Config.FirstLoad = false;
            }


        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) {
            // Check if OBS is connected
            OBSController!.CheckRecordingStatus();

            CheckRecordingStatus();
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e) {
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
                            Console.WriteLine("Recording started.");
                            OBSController.StartRecording();
                        }
                        else if (answer == "2")
                        {
                            Console.WriteLine("Recording not started.");
                        }
                    }
                );
            }
        }
    }
}