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
        public static bool IsRecording { get; set; } = false;
        public static bool IsStreaming { get; set; } = false;
        public static bool IsConnected { get; set; } = false;

        public override void Entry(IModHelper helper) {
            Helper = helper;
            ModMonitor = Monitor;

            // Register events
            Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched!;
            Helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded!;
            Helper.Events.GameLoop.DayStarted += this.OnDayStarted!;
        }

        private async void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
            Config = Helper.ReadConfig<ModConfig>();
            ModSettings.Settings(ModManifest);

            if (!Config.FirstLoad) {
                OBSController = new OBSController(Config.OBSWebSocketIP, Config.OBSWebSocketPort, Config.Password, 0);
                await OBSController.Connect();
                OBSController.maxReconnectAttempts = Config.ReconnectAttempts;
            } else {
                Config.FirstLoad = false;
                Helper.WriteConfig(Config);
            }
        }

        private async void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) {
            if (!IsConnected) {
                ModMonitor.Log("OBS isn't Connected!", StardewModdingAPI.LogLevel.Warn);
                await AskConnect();
                if (!IsConnected) { return; }
            }
            await CheckOBSStatus();

            // TODO: Add a setting in case the user wants to suppress recording notifications while streaming
            // for now it will always ask
            #pragma warning disable CS0162 // Disable the unreachable code warning
            if (true) {
                if (IsRecording) {
                    ModMonitor.Log("OBS is recording!", StardewModdingAPI.LogLevel.Alert);
                } else {
                    ModMonitor.Log("OBS isn't recording! Asking player if they want to record!", StardewModdingAPI.LogLevel.Alert);
                    await AskRecord();
                }
            } else {
                if (IsRecording) {
                    ModMonitor.Log("OBS is recording!", StardewModdingAPI.LogLevel.Alert);
                } else {
                    ModMonitor.Log("OBS isn't recording, but the player has turned on 'suppress record notifications while streaming' setting!", StardewModdingAPI.LogLevel.Alert);
                }
            }
            #pragma warning restore CS0162 // Re-enable the warning
        }

        private async void OnDayStarted(object? sender, DayStartedEventArgs e) {
            if (!IsConnected) { return; }
            await CheckOBSStatus();

            #pragma warning disable CS0162 // Disable the unreachable code warning
            // for now it will always ask
            if (true) {
                if (IsRecording){
                    // I would like to create recording chapters via CreateRecordChapter here

                    // Here is where we should check the setting for if the user wants daily, weekly or monthly recording toggle
                    // For now we are just going to say were always going to restart recording daily
                    if (true) {
                        await RestartRecord();
                    } else {
                        // If the user doesnt want daily toggles well need to check the current day and see if its the start of a new week or new month
                        if ((Game1.dayOfMonth - 1) % 7 == 0)
                        {
                            await RestartRecord();
                        }
                        else if (Game1.dayOfMonth == 1)
                        {
                            await RestartRecord();
                        }
                    }
                } else {
                    ModMonitor.Log("OBS isn't recording!", StardewModdingAPI.LogLevel.Alert);
                }
            } else {
                if (IsRecording) {
                    // I would like to create recording chapters via CreateRecordChapter here

                    // Here is where we should check the setting for if the user wants daily, weekly or monthly recording toggle
                    // For now we are just going to say were always going to restart recording daily
                    if (true) {
                        await RestartRecord();
                    } else {
                        // If the user doesnt want daily toggles well need to check the current day and see if its the start of a new week or new month
                        if ((Game1.dayOfMonth - 1) % 7 == 0) {
                            // I would like to create recording chapters via CreateRecordChapter here
                            await RestartRecord();
                        } else if (Game1.dayOfMonth == 1) {
                            await RestartRecord();
                        }
                    }
                } else {
                    ModMonitor.Log("OBS isn't recording!", StardewModdingAPI.LogLevel.Alert);
                }
            }
            #pragma warning restore CS0162 // Re-enable the warning
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
            IsRecording = response["responseData"]?["outputActive"]?.Value<bool>() ?? false;

            // Check if OBS is streaming
            requestData = new JObject {
                { "requestType", "GetStreamStatus" },
                { "requestId", System.Guid.NewGuid().ToString() }
            };

            // Send the request and wait for the response
            response = await OBSController.HandleOp6Requests(requestData);
            
            // If the response outputActive is null then set it false if its available then use the response
            IsStreaming = response["responseData"]?["outputActive"]?.Value<bool>() ?? false;

            // This ensures the method is always asynchronous
            await Task.CompletedTask;
        }

        private static async Task AskRecord() {
            if (!IsRecording) {
                PlayerNotify.Dialogue(
                    "Wait! OBS isn't recording! Do you want to start?",
                    new List<Response> {
                        new ("1", "Yes! Lights, Camera, ACTION!"),
                        new ("2", "Nah, not feeling it right now.")
                    },
                    async answer => {
                        if (answer == "1") {
                            await StartRecord();
                        }
                    }
                );
            }

            // This ensures the method is always asynchronous
            await Task.CompletedTask;
        }

        private static async Task AskConnect() {
            if (!IsRecording) {
                PlayerNotify.Dialogue(
                    "Wait! OBS isn't connected! Do you want to connect?",
                    new List<Response> {
                        new ("1", "Yes! Good catch!"),
                        new ("2", "Nah. It's fine.")
                    },
                    async answer => {
                        if (answer == "1")
                        {
                            await OBSController.Connect();
                        }
                    }
                );
            }

            // This ensures the method is always asynchronous
            await Task.CompletedTask;
        }

        private static async Task StartRecord() {
            ModMonitor.Log("Sending Start Record Request.", StardewModdingAPI.LogLevel.Alert);
            var requestData = new JObject {
                { "requestType", "StartRecord" },
                { "requestId", System.Guid.NewGuid().ToString() }
            };
            var response = await OBSController.HandleOp6Requests(requestData);

            // If the response outputActive is null then set it false if its available then use the response
            IsRecording = response["responseData"]?["outputActive"]?.Value<bool>() ?? false;

            if (IsRecording)
            {
                ModMonitor.Log("OBS is now recording!", StardewModdingAPI.LogLevel.Alert);
            }
            else
            {
                ModMonitor.Log($"Uh Oh! OBS is not recording! Comments: {response["responseData"]?["comment"]}", StardewModdingAPI.LogLevel.Alert);
            }

            // This ensures the method is always asynchronous
            await Task.CompletedTask;
        }

        private static async Task StopRecord()
        {
            ModMonitor.Log("Sending Stop Record Request.", StardewModdingAPI.LogLevel.Alert);
            var requestData = new JObject {
                { "requestType", "StopRecord" },
                { "requestId", System.Guid.NewGuid().ToString() }
            };
            var response = await OBSController.HandleOp6Requests(requestData);

            // Wait until OBS confirms recording has fully stopped
            bool hasStopped = false;
            while (!hasStopped) {
                // Get the current recording status
                requestData = new JObject {
                    { "requestType", "GetRecordStatus" },
                    { "requestId", System.Guid.NewGuid().ToString() }
                };
                response = await OBSController.HandleOp6Requests(requestData);

                hasStopped = response["responseData"]?["outputActive"]?.Value<bool>() == false;

                if (!hasStopped) {
                    // If still recording, wait a short time before checking again
                    await Task.Delay(500);
                }
            }

            ModMonitor.Log($"OBS has stopped recording! Output Path: {response["responseData"]?["outputPath"]}", StardewModdingAPI.LogLevel.Alert);

            IsRecording = false;

            await Task.CompletedTask;
        }

        private static async Task RestartRecord() {
            // If the user wants daily recording toggles
            ModMonitor.Log("Stopping Recording...", StardewModdingAPI.LogLevel.Alert);
            await StopRecord();

            // Here is where we would set the file name, probably could be "<Farm Name> Day <Day> Year <Year>"
            // For now though, im not messing with that
            ModMonitor.Log("Starting Recording...", StardewModdingAPI.LogLevel.Alert);
            await StartRecord();

            ModMonitor.Log("Recording has been restarted!", StardewModdingAPI.LogLevel.Alert);

            // This ensures the method is always asynchronous
            await Task.CompletedTask;
        }
    }
}