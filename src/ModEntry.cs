using static System.Net.Mime.MediaTypeNames;

using Microsoft.Xna.Framework.Graphics;

using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using GenericModConfigMenu;

namespace ValleyCast {
    public class ModEntry : Mod {
        private ModConfig? Config;
        private OBSController? obsController;
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
            this.Config = Helper.ReadConfig<ModConfig>();

            // Initialize OBSController with the current config values
            this.obsController = new OBSController(Config.OBSWebSocketIP, Config.OBSWebSocketPort, Config.Password, this.Monitor);

            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => Helper.WriteConfig(this.Config!)
            );

            configMenu.AddSectionTitle(
                mod: this.ModManifest, 
                text:() =>  "OBS WebSocket Authorization", 
                tooltip: () => "Authorization settings for OBS WebSocket Server"
            );

            // Add options to the mod config menu
            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "OBS WebSocket IP Address",
                getValue: () => this.Config!.OBSWebSocketIP,
                setValue: value => this.Config!.OBSWebSocketIP = value
            );

            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "OBS WebSocket Port",
                getValue: () => this.Config!.OBSWebSocketPort,
                setValue: value => this.Config!.OBSWebSocketPort = value
            );

            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "OBS WebSocket Password",
                getValue: () => this.Config!.Password,
                setValue: value => this.Config!.Password = value
            );
        }
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) {
            // Check if OBS is connected
            this.obsController!.CheckRecordingStatus();

            // Check if OBS Studio is recording currently
            // bool isRecording = this.obsController.IsRecording;

            CheckRecordingStatus();
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e) {
            CheckRecordingStatus();
        }

        private void CheckRecordingStatus() {
            // Check if OBS Studio is recording currently
            bool isRecording = this.obsController!.IsRecording;

            if (!isRecording) {
                // Send a message to the player
                PlayerNotify.Notify(
                    "OBS is not recording, do you want to start recording?",
                    new List<Response> {
                        new ("1", "Yes"),
                        new ("2", "No")
                    },
                    answer =>
                    {
                        if (answer == "1")
                        {
                            Console.WriteLine("Recording started.");
                            this.obsController.StartRecording();
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