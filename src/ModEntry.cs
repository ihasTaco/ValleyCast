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
            (bool isRecording, bool isStreaming) = await CheckOBSStatus();

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
            (bool isRecording, bool isStreaming) = await CheckOBSStatus();

            // TODO: Add a setting in case the user wants to suppress recording notifications while streaming
            if (isStreaming || !isStreaming) {
                if (isRecording) {
                    ModMonitor.Log("Daily Alert: OBS is recording!", StardewModdingAPI.LogLevel.Alert);
                } else {
                    ModMonitor.Log("Daily Alert: OBS isn't recording!", StardewModdingAPI.LogLevel.Alert);
                }
            } else {
                if (isRecording) {
                    ModMonitor.Log("Daily Alert: OBS is recording!", StardewModdingAPI.LogLevel.Alert);
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

            return (isRecording: isRecording, isStreaming: isStreaming);

        }

        private static async Task AskRecord(bool isRecording) {
            if (!isRecording) {
                PlayerNotify.Dialogue(
                    "Wait! OBS isn't recording! Do you want to start?",
                    new List<Response> {
                        new ("1", "Yes! Lights, Camera, ACTION!"),
                        new ("2", "Nah, not feeling it right now.")
                    },
                    answer => {
                        if (answer == "1") {
                            ModMonitor.Log("Sending Start Record Request.", StardewModdingAPI.LogLevel.Alert);
                            var requestData = new JObject {
                                { "requestType", "StartRecord" },
                                { "requestId", System.Guid.NewGuid().ToString() }
                            };
                            var response = await OBSController.HandleOp6Requests(requestData);

                            // If the response outputActive is null then set it false if its available then use the response
                            isRecording = response["responseData"]?["outputActive"]?.Value<bool>() ?? false;

                            if (isRecording) {
                                ModMonitor.Log("OBS is now recording!", StardewModdingAPI.LogLevel.Alert);
                            } else {
                                ModMonitor.Log($"Uh Oh! OBS is not recording! Comments: {response["responseData"]?["comment"]}", StardewModdingAPI.LogLevel.Alert);
                            }

                            return isRecording;
                        } else {
                            return false;
                        }
                    }
                );
            }
        }
    }
}