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
        public static bool IsRestartingRecording { get; set; } = false;

        public override void Entry(IModHelper helper) {
            Helper = helper;
            ModMonitor = Monitor;

            // Key bindings
            Helper.Events.Input.ButtonPressed += OnButtonPressed!; // Test Config Hot Reload Keybind

            // Register events
            Helper.Events.GameLoop.GameLaunched += this.OnGameLaunched!;
            Helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded!;
            Helper.Events.GameLoop.DayStarted += this.OnDayStarted!;
        }
        private static void InitOBSController() {
            if (OBSController == null) {
                OBSController = new OBSController(Config.OBSWebSocketIP, Config.OBSWebSocketPort, Config.Password, 0);
                OBSController.maxReconnectAttempts = Config.ReconnectAttempts;
            }
        }

        private async void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
            Config = Helper.ReadConfig<ModConfig>();
            ModSettings.Settings(ModManifest);
        }

        private async void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) {
            if (!Config.FirstLoad) {
                InitOBSController();
                await OBSController.Connect();
                await VerifyScenesExist();
            } else {
                Config.FirstLoad = false;
                Helper.WriteConfig(Config);
            }

            await EnsureOBSConnectedAndPromptIfIdle();
            await SyncOBSStateForCurrentDay();
        }

        private async void OnDayStarted(object? sender, DayStartedEventArgs e) {
            if (!IsConnected) return;
            await EnsureOBSConnectedAndPromptIfIdle();
            await SyncOBSStateForCurrentDay();
        }

        private static async Task CheckOBSStatus() {
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
        }

        private static async Task EnsureOBSConnectedAndPromptIfIdle() {
            // TODO: When streaming logic is added, consider checking:
            // if (IsStreaming && Config.SuppressRecordPromptWhileStreaming) return;

            if (!IsConnected) {
                ModMonitor.Log("OBS not connected. Prompting connection...", StardewModdingAPI.LogLevel.Warn);
                await AskConnect();

                // Give OBS a moment to finish handshaking
                await Task.Delay(500);
            }

            if (!IsConnected) {
                ModMonitor.Log("OBS connection failed or canceled. Skipping further prompts.", StardewModdingAPI.LogLevel.Warn);
                return;
            }

            await CheckOBSStatus();

            if (!IsRecording) {
                ModMonitor.Log("OBS is connected but not recording. Prompting player...", StardewModdingAPI.LogLevel.Alert);
                await AskRecord();
            } else {
                ModMonitor.Log("OBS is already recording. No action needed.", StardewModdingAPI.LogLevel.Info);
            }
        }

        public static async Task AskRecord() {
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
        }

        private static async Task AskConnect() {
            if (IsConnected) return;

            PlayerNotify.Dialogue(
                "Wait! OBS isn't connected! Do you want to connect?",
                new List<Response> {
                    new ("1", "Yes! Good catch!"),
                    new ("2", "Nah. It's fine.")
                },
                async answer => {
                    if (answer == "1") {
                        await OBSController.Connect();

                        // Give the websocket time to process the Identify response
                        await Task.Delay(250);

                        if (IsConnected) {
                            ModMonitor.Log("✅ OBS connection established after player confirmed. Proceeding to check recording status.", StardewModdingAPI.LogLevel.Info);
                            await CheckOBSStatus();

                            if (!IsRecording) {
                                await AskRecord();
                            }
                        } else {
                            ModMonitor.Log("❌ OBS connection failed after player confirmed. Skipping AskRecord().", StardewModdingAPI.LogLevel.Warn);
                        }
                    }
                }
            );
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

            if (IsRecording) {
                ModMonitor.Log("OBS is now recording!", StardewModdingAPI.LogLevel.Alert);
            } else {
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
        }

        private static async Task RestartRecord() {
            IsRestartingRecording = true;

            // If the user wants daily recording toggles
            ModMonitor.Log("Stopping Recording...", StardewModdingAPI.LogLevel.Alert);
            await StopRecord();

            // Here is where we would set the file name, probably could be "<Farm Name> Day <Day> Year <Year>"
            // For now though, im not messing with that
            ModMonitor.Log("Starting Recording...", StardewModdingAPI.LogLevel.Alert);
            await StartRecord();

            ModMonitor.Log("Recording has been restarted!", StardewModdingAPI.LogLevel.Alert);

            IsRestartingRecording = false;
        }

        // Hot reload the config
        public static void ReloadConfig() {
            ModMonitor.Log("Reloading config from file...", StardewModdingAPI.LogLevel.Debug);
            Config = Helper.ReadConfig<ModConfig>();
            ModMonitor.Log("Config reloaded!", StardewModdingAPI.LogLevel.Info);

            // Immediately update the OBS text after reloading
            if (IsConnected) {
                string formattedText = Config.DayCounterFormat
                    .Replace("{day}", Game1.dayOfMonth.ToString())
                    .Replace("{season}", Game1.currentSeason)
                    .Replace("{year}", Game1.year.ToString());

                _ = OBSController.UpdateDayText(Config.DayCounterSource, formattedText);
                _ = VerifyScenesExist();
            }
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e) {
            // Only trigger on a specific key and only if player is loaded in
            if (!Context.IsWorldReady) return;

            if (e.Button == SButton.F5)
            {
                ReloadConfig();
                Game1.addHUDMessage(new HUDMessage("Config reloaded!", HUDMessage.newQuest_type));
            }
        }

        private static async Task VerifyScenesExist() {
            var scenes = await OBSController.GetSceneNames();

            if (scenes == null) {
                ModMonitor.Log("⚠️ Could not retrieve scene list from OBS.", StardewModdingAPI.LogLevel.Error);
                return;
            }

            var expectedScenes = new[] {
                Config.SpringScene,
                Config.SummerScene,
                Config.FallScene,
                Config.WinterScene
            };

            foreach (var sceneName in expectedScenes) {
                if (!scenes.Contains(sceneName)) {
                    ModMonitor.Log($"❌ Missing OBS scene: '{sceneName}' not found!", StardewModdingAPI.LogLevel.Warn);
                    PlayerNotify.Popup($"Missing OBS scene: {sceneName}", HUDMessage.error_type);
                } else {
                    ModMonitor.Log($"✅ Found OBS scene: {sceneName}", StardewModdingAPI.LogLevel.Trace);
                }
            }
        }

        private static async Task SyncOBSStateForCurrentDay() {
            if (!IsConnected) { return; }
            ModMonitor.Log("🔄 Syncing OBS state for current day...", StardewModdingAPI.LogLevel.Trace);
            await CheckOBSStatus();

            // Recording logic
            if (IsRecording && ShouldRestartRecording()) {
                await RestartRecord();
            } else {
                ModMonitor.Log("OBS isn't recording!", StardewModdingAPI.LogLevel.Alert);
            }

            // Day counter text
            string formattedText = Config.DayCounterFormat
                .Replace("{day}", Game1.dayOfMonth.ToString())
                .Replace("{season}", Game1.currentSeason)
                .Replace("{year}", Game1.year.ToString());

            await OBSController.UpdateDayText(Config.DayCounterSource, formattedText);

            // Scene switching
            string sceneToSwitch = Game1.currentSeason switch {
                "spring" => Config.SpringScene,
                "summer" => Config.SummerScene,
                "fall" => Config.FallScene,
                "winter" => Config.WinterScene,
                _ => null
            };

            if (string.IsNullOrWhiteSpace(sceneToSwitch)) { return; }

            var scenes = await OBSController.GetSceneNames();

            string finalScene = scenes != null && scenes.Contains(sceneToSwitch) ? sceneToSwitch : Config.FallbackScene;

            if (!scenes.Contains(sceneToSwitch)) {
                ModMonitor.Log($"⚠️ Scene '{sceneToSwitch}' not found. Falling back to '{finalScene}'", StardewModdingAPI.LogLevel.Warn);
                PlayerNotify.Popup($"Scene missing: {sceneToSwitch}. Using fallback.", HUDMessage.error_type);
            }

            var switchRequest = new JObject {
                { "requestType", "SetCurrentProgramScene" },
                { "requestId", Guid.NewGuid().ToString() },
                { "requestData", new JObject {
                    { "sceneName", finalScene }
                }}
            };

            await OBSController.HandleOp6Requests(switchRequest);
            ModMonitor.Log($"✅ Switched OBS scene to '{finalScene}'", StardewModdingAPI.LogLevel.Info);
        }

        private static bool ShouldRestartRecording() {
            if (Config.DailyRecording) return true;
            if (Config.WeeklyRecording && (Game1.dayOfMonth - 1) % 7 == 0) return true;
            if (Config.MonthlyRecording && Game1.dayOfMonth == 1) return true;
            return false;
        }
    }
}