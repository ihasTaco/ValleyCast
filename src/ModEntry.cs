using StardewModdingAPI;
using StardewModdingAPI.Events;
using GenericModConfigMenu;
using ValleyCast;
using static System.Net.Mime.MediaTypeNames;
using StardewValley;

namespace ValleyCast
{
    public class ModEntry : Mod
    {
        private ModConfig Config = null!;
        private OBSController obsController = null!;

        public override void Entry(IModHelper helper)
        {
            // Load the config using the new ModConfig class
            this.Config = helper.ReadConfig<ModConfig>();

            // Initialize OBSController with the current config values
            this.obsController = new OBSController(Config.OBSWebSocketIP, Config.OBSWebSocketPort, Config.Password, this.Monitor);

            // Register the GameLaunched event
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched!;

            // Register events for loading a save or starting a new game
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded!;
            helper.Events.GameLoop.DayStarted += this.OnDayStarted!;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
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
                getValue: () => this.Config.OBSWebSocketIP,
                setValue: value => this.Config.OBSWebSocketIP = value
            );

            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "OBS WebSocket Port",
                getValue: () => this.Config.OBSWebSocketPort,
                setValue: value => this.Config.OBSWebSocketPort = value
            );

            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "OBS WebSocket Password",
                getValue: () => this.Config.Password,
                setValue: value => this.Config.Password = value
            );
        }
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            CheckOBSRecordingStatus();
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            CheckOBSRecordingStatus();
        }

        private void CheckOBSRecordingStatus()
        {
            // Check if OBS is connected
            this.obsController.CheckRecordingStatus();

            // Check if OBS Studio is recording currently
            bool isRecording = this.obsController.IsRecording;

            if (!isRecording)
            {
                GameLocation location = Game1.currentLocation;
                Response[] responses = {
                    new Response ("1", "Yes"),
                    new Response ("2", "No")
                };
                location.createQuestionDialogue("OBS is not recording, do you want to start recording?", responses, delegate (Farmer _, string answer)
                {
                    switch (answer)
                    {
                        case "1":
                            this.obsController.StartRecording();
                            break;
                        case "2":
                            break;
                    }
                });

                // Prompt the user if they want to start recording
                // string response;
                // Game1.drawObjectDialogue("Message Here");
                // Response[] responses = {
                //     new Response ("Yes", (response="Yes")),
                //     new Response ("No", (response="No"))
                // };
                // switch (response)
                // {
                //     case "Yes":
                //         this.obsController.StartRecording();
                //         break;
                //     case "No":
                //         break;
                // }
            }
        }
    }
}