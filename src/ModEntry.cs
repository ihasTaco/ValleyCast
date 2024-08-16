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
        public static bool IsRecording { get; set; } = null!;
        public static bool IsStreaming { get; set; } = null!;

        public override void Entry(IModHelper helper) {
            Helper = helper;
            ModMonitor = this.Monitor;

            // Register events
            Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched!;
            Helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded!;
            Helper.Events.GameLoop.DayStarted += this.OnDayStarted!;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
            Config = Helper.ReadConfig<ModConfig>();
            ModSettings.Settings(ModManifest);

            if (!Config.FirstLoad) {
                OBSController = new OBSController(Config.OBSWebSocketIP, Config.OBSWebSocketPort, Config.Password, 1);
            } else {
                Config.FirstLoad = false;
                Helper.WriteConfig(Config);
            }
        }

        private static async void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) {
            (isRecording, isStreaming) = await CheckOBSStatus();

            // TODO: Add a setting in case the user wants to suppress recording notifications while streaming
            if (isStreaming || !isStreaming) {
                if (isRecording) {
                    ModMonitor.Log("OBS is recording!", StardewModdingAPI.LogLevel.Alert);
                } else {
                    ModMonitor.Log("OBS isn't recording! Asking player if they want to record!", StardewModdingAPI.LogLevel.Alert);
                    await AskRecord(isRecording);
                }
            } else {
                if (isRecording) {
                    ModMonitor.Log("OBS is recording!", StardewModdingAPI.LogLevel.Alert);
                } else {
                    ModMonitor.Log("OBS isn't recording, but the player has turned on 'suppress record notifications while streaming' setting!", StardewModdingAPI.LogLevel.Alert);
                }
            }
        }

        private static async void OnDayStarted(object? sender, DayStartedEventArgs e) {
            (isRecording, isStreaming) = await CheckOBSStatus();

            // TODO: Add a setting in case the user wants to suppress recording notifications while streaming
            if (true) {
                if (isRecording) {
                    // Here is where we should check the setting for if the user wants daily, weekly or monthly recording toggle
                    // For now we are just going to say were always going to restart recording daily
                    if (true) {
                        RestartRecord();
                    } else {
                        // If the user doesnt want daily toggles well need to check the current day and see if its the start of a new week or new month
                        if ((Game1.dayOfMonth - 1) % 7 == 0)
                        {
                            RestartRecord();
                        }
                        else if (Game1.dayOfMonth == 1)
                        {
                            RestartRecord();
                        }
                    }
                } else {
                    ModMonitor.Log("Daily Alert: OBS isn't recording!", StardewModdingAPI.LogLevel.Alert);
                }
            } else {
                if (isRecording)
                {
                    // Here is where we should check the setting for if the user wants daily, weekly or monthly recording toggle
                    // For now we are just going to say were always going to restart recording daily
                    if (true) {
                        RestartRecord();
                    } else {
                        // If the user doesnt want daily toggles well need to check the current day and see if its the start of a new week or new month
                        if ((Game1.dayOfMonth - 1) % 7 == 0) {
                            RestartRecord();
                        } else if (Game1.dayOfMonth == 1) {
                            RestartRecord();
                        }
                    }
                } else {
                    ModMonitor.Log("Daily Alert: OBS isn't recording!", StardewModdingAPI.LogLevel.Alert);
                }
            }
        }

        private static async Task CheckOBSStatus()
        {
            // Check if OBS is recording
            var requestData = new JObject {
                { "requestType", "GetRecordStatus" },
                { "requestId", System.Guid.NewGuid().ToString() }
            };

            // Send the request and wait for the response
            var response = await OBSController.HandleOp6Requests(requestData);

            // If the response outputActive is null then set it false if its available then use the response
            bool isRecording = response["responseData"]?["outputActive"]?.Value<bool>() ?? false;

            // Check if OBS is streaming
            var requestData = new JObject {
                { "requestType", "GetStreamStatus" },
                { "requestId", System.Guid.NewGuid().ToString() }
            };

            // Send the request and wait for the response
            var response = await OBSController.HandleOp6Requests(requestData);

            // If the response outputActive is null then set it false if its available then use the response
            bool isStreaming = response["responseData"]?["outputActive"]?.Value<bool>() ?? false;

            return (isRecording, isStreaming);

        }

        private static async Task AskRecord() {
            if (!isRecording) {
                PlayerNotify.Dialogue(
                    "Wait! OBS isn't recording! Do you want to start?",
                    new List<Response> {
                        new ("1", "Yes! Lights, Camera, ACTION!"),
                        new ("2", "Nah, not feeling it right now.")
                    },
                    answer => {
                        if (answer == "1") {
                            StartRecord();
                        } else {
                            return;
                        }
                    }
                );
            }
        }

        private static async Task StartRecord() {
            ModMonitor.Log("Sending Start Record Request.", StardewModdingAPI.LogLevel.Alert);
            var requestData = new JObject {
                { "requestType", "StartRecord" },
                { "requestId", System.Guid.NewGuid().ToString() }
            };
            var response = await OBSController.HandleOp6Requests(requestData);

            // If the response outputActive is null then set it false if its available then use the response
            isRecording = response["responseData"]?["outputActive"]?.Value<bool>() ?? false;

            if (isRecording)
            {
                ModMonitor.Log("OBS is now recording!", StardewModdingAPI.LogLevel.Alert);
            }
            else
            {
                ModMonitor.Log($"Uh Oh! OBS is not recording! Comments: {response["responseData"]?["comment"]}", StardewModdingAPI.LogLevel.Alert);
            }
        }

        private static async Task StopRecord() {
            ModMonitor.Log("Sending Stop Record Request.", StardewModdingAPI.LogLevel.Alert);
            var requestData = new JObject {
                { "requestType", "StopRecord" },
                { "requestId", System.Guid.NewGuid().ToString() }
            };
            var response = await OBSController.HandleOp6Requests(requestData);

            ModMonitor.Log($"OBS has stopped recording! Output Path: {response["responseData"]?["outputPath"]}", StardewModdingAPI.LogLevel.Alert);

            isRecording = false;
        }

        private static async Task RestartRecord() {
            // If the user wants daily recording toggles
            ModMonitor.Log("Daily Alert: Stopping Recording...", StardewModdingAPI.LogLevel.Alert);
            StopRecord();

            // Here is where we would set the file name, probably could be "<Farm Name> Day <Day> Year <Year>"
            // For now though, im not messing with that
            ModMonitor.Log("Daily Alert: Starting Recording...", StardewModdingAPI.LogLevel.Alert);
            StartRecord();

            ModMonitor.Log("Daily Alert: Recording has been restarted!", StardewModdingAPI.LogLevel.Alert);
        }
    }
}