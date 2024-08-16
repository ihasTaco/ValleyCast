using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

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

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) {
            // Check if OBS is connected
            // TODO: Add a check to see if the user is streaming, if they are then skip this check, or maybe just silence the initial "do you want to record" dialogue
            OBSController!.CheckRecordingStatus();
        }

        async private void OnDayStarted(object? sender, DayStartedEventArgs e) {
            // This will be similar functionality to OnSaveLoaded but wont ask for permission to start recording (if obs is recording)
            // This function will check the config and see if the user has daily, weekly, or seasonal recording restarts set
            // if the current day matches the above settings then it will stop recording, and start it back up
            await CheckRecordingStatus();
        }
        private static async Task CheckRecordingStatus()
        {
            // Create a TaskCompletionSource to wait for the op: 7 response
            var tcs = new TaskCompletionSource<bool>();

            // Define a temporary event handler to capture the response
            void OnMessageReceived(object? sender, MessageEventArgs e)
            {
                var response = JObject.Parse(e.Data);
                int opCode = response["op"]?.Value<int>() ?? -1;

                if (opCode == 7)
                {
                    // Ensure we are handling the specific response for GetRecordStatus
                    string requestType = response["d"]!["requestType"]!.ToString();
                    if (requestType == "GetRecordStatus")
                    {
                        bool outputActive = response["d"]?["responseData"]?["outputActive"]?.Value<bool>() ?? false;

                        // Set the result in the TaskCompletionSource
                        tcs.SetResult(outputActive);

                        // Unsubscribe from the event
                        OBSController!.ws.OnMessage -= OnMessageReceived;
                    }
                }
            }

            // Subscribe to the WebSocket OnMessage event
            OBSController!.ws.OnMessage += OnMessageReceived;

            // Send the request to OBS
            OBSController.CheckRecordingStatus();

            // Await the result
            bool isRecording = await tcs.Task;

            // Now that we know whether OBS is recording, proceed with the logic
            if (!isRecording)
            {
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
                            ModMonitor.Log("Recording started.", StardewModdingAPI.LogLevel.Alert);
                            OBSController.StartRecording();
                        }
                        else if (answer == "2")
                        {
                            ModMonitor.Log("Recording not started.", StardewModdingAPI.LogLevel.Alert);
                        }
                    }
                );
            }
        }
    }
}