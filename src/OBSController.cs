using WebSocketSharp;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;
using StardewValley;
using StardewValley.Menus;

namespace ValleyCast {
    public class OBSController {
        private WebSocket ws = null!;
        private readonly string url;
        private readonly string password;
        private int currentReconnectAttempt;
        public int maxReconnectAttempts;
        public bool IsRecording { get; private set; } = false;
        private TaskCompletionSource<JToken> tcs;
        private readonly Dictionary<string, TaskCompletionSource<JToken>> pendingRequests = new Dictionary<string, TaskCompletionSource<JToken>>();

        public OBSController(string ipAddress, string port, string password, int maxReconnectAttempts) {
            this.url = $"ws://{ipAddress}:{port}";
            this.password = password;
            this.maxReconnectAttempts = maxReconnectAttempts;
            this.tcs = new TaskCompletionSource<JToken>();
        }

        public async Task Connect() {
            if (ModEntry.IsConnected) { return; }

            ws = new WebSocket(url, "obswebsocket.json");
            ws.OnMessage += OnMessageReceived!;
            ws.OnError += OnError!;
            ws.OnClose += OnClose!;
            ws.Connect();
            // This ensures the method is always asynchronous
            await Task.CompletedTask;
        }

        private void OnMessageReceived(object sender, MessageEventArgs e) {
            var response = JObject.Parse(e.Data);
            int opCode = response["op"]?.Value<int>() ?? -1;

            try {
                ModEntry.ModMonitor.Log($"Received WebSocket message: {e.Data}", StardewModdingAPI.LogLevel.Debug);
                if (opCode == 0) {
                    HandleHello(response["d"]!);
                }
                else if (opCode == 2) {
                    HandleIdentified(response["d"]!);
                }
                else if (opCode == 5) {
                    HandleEvent(response["d"]!);
                }
                else if (opCode == 7) {
                    HandleRequestResponse(response["d"]!);
                }
            } catch (Exception ex) {
                // Log the exception with full details
                ModEntry.ModMonitor.Log($"An error occurred while processing a WebSocket message: {ex}", StardewModdingAPI.LogLevel.Error);
            }
        }

        private void OnError(object sender, WebSocketSharp.ErrorEventArgs e) {
            ModEntry.ModMonitor.Log($"OBS WebSocket Error: {e.Message}", StardewModdingAPI.LogLevel.Error);
        }

        private void OnClose(object sender, CloseEventArgs e) {
            ModEntry.IsConnected = false;
            ModEntry.ModMonitor.Log("OBS WebSocket connection closed.", StardewModdingAPI.LogLevel.Warn);
            AttemptReconnection();
        }

        private JObject CreateIdentifyRequest(JToken data) {
            int rpcVersion = data["rpcVersion"]!.Value<int>();
            string authChallenge = data["authentication"]?["challenge"]?.Value<string>()!;
            string salt = data["authentication"]?["salt"]?.Value<string>()!;

            #pragma warning disable IDE0090 // Disable 'new' expression can be simplified warning
            JObject identifyRequest = new JObject {
                { "op", 1 }, // OpCode 1 for Identify
                { "d", new JObject {
                        { "rpcVersion", rpcVersion }
                    }
                }
            };
            #pragma warning restore IDE0090 // Re-enable the warning

            if (!string.IsNullOrEmpty(authChallenge) && !string.IsNullOrEmpty(salt)) {
                string passwordSalt = this.password + salt;
                byte[] passwordSaltBytes = Encoding.UTF8.GetBytes(passwordSalt);
                byte[] sha256HashBytes;

                using (SHA256 sha256 = SHA256.Create()) {
                    sha256HashBytes = sha256.ComputeHash(passwordSaltBytes);
                }

                string base64Secret = Convert.ToBase64String(sha256HashBytes);
                string secretChallenge = base64Secret + authChallenge;
                byte[] secretChallengeBytes = Encoding.UTF8.GetBytes(secretChallenge);

                using (SHA256 sha256 = SHA256.Create()) {
                    sha256HashBytes = sha256.ComputeHash(secretChallengeBytes);
                }

                string authResponse = Convert.ToBase64String(sha256HashBytes);
                identifyRequest["d"]!["authentication"] = authResponse;
            }

            return identifyRequest;
        }

        private void HandleHello(JToken data) {
            var identifyRequest = CreateIdentifyRequest(data);
            ws.Send(identifyRequest.ToString());
        }

        private static void HandleIdentified(JToken data) {
            if (data["negotiatedRpcVersion"]?.ToString() == "1") {
                ModEntry.IsConnected = true;
            } else {
                ModEntry.IsConnected = false;
            }
        }

        private static void HandleEvent(JToken data) {
            // These events are the ones that I think I will need to get the mod to a point I think is finished.
            // just in case I need to add more, https://github.com/obsproject/obs-websocket/blob/master/docs/generated/protocol.md#events
            // These Event handlers are all placeholders for now, eventually I will add functionality to actually do stuff with these
            if (data["eventType"]?.ToString() == "ExitStarted") {
                ModEntry.ModMonitor.Log("OBS is exiting.", StardewModdingAPI.LogLevel.Debug);
            } else if (data["eventType"]?.ToString() == "SceneCreated") {
                ModEntry.ModMonitor.Log("A new scene was created", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Scene Name | {data["eventData"]!["sceneName"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Scene UUID | {data["eventData"]!["sceneUuid"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|   Is Group | {data["eventData"]!["isGroup"]}", StardewModdingAPI.LogLevel.Debug);
            } else if (data["eventType"]?.ToString() == "SceneRemoved") {
                ModEntry.ModMonitor.Log("A scene was removed", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Scene Name | {data["eventData"]!["sceneName"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Scene UUID | {data["eventData"]!["sceneUuid"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|   Is Group | {data["eventData"]!["isGroup"]}", StardewModdingAPI.LogLevel.Debug);
            } else if (data["eventType"]?.ToString() == "SceneNameChanged") {
                ModEntry.ModMonitor.Log("A scene name was changed", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Scene UUID | {data["eventData"]!["sceneUuid"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|   Old Name | {data["eventData"]!["oldSceneName"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|   New Name | {data["eventData"]!["sceneName"]}", StardewModdingAPI.LogLevel.Debug);
            } else if (data["eventType"]?.ToString() == "SceneListChanged") {
                ModEntry.ModMonitor.Log("The scene list was changed", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|     Scenes | {String.Join(" |", data["eventData"]!["scenes"]!)}", StardewModdingAPI.LogLevel.Debug);
            } else if (data["eventType"]?.ToString() == "InputCreated") {
                ModEntry.ModMonitor.Log("A new input was created", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Input Name | {data["eventData"]!["inputName"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Input UUID | {data["eventData"]!["inputUuid"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Input Kind | {data["eventData"]!["inputKind"]}", StardewModdingAPI.LogLevel.Debug);
            } else if (data["eventType"]?.ToString() == "InputRemoved") {
                ModEntry.ModMonitor.Log("An input name was removed", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Input Name | {data["eventData"]!["inputName"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Input UUID | {data["eventData"]!["inputUuid"]}", StardewModdingAPI.LogLevel.Debug);
            } else if (data["eventType"]?.ToString() == "InputNameChanged") {
                ModEntry.ModMonitor.Log("An input name was changed", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Input UUID | {data["eventData"]!["inputUuid"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|   Old Name | {data["eventData"]!["oldInputName"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|   New Name | {data["eventData"]!["inputName"]}", StardewModdingAPI.LogLevel.Debug);
            } else if (data["eventType"]?.ToString() == "SceneItemCreated") {
                ModEntry.ModMonitor.Log("A scene item was created", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Scene Name | {data["eventData"]!["sceneName"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Scene UUID | {data["eventData"]!["sceneUuid"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|   Src Name | {data["eventData"]!["sourceName"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|   Src UUID | {data["eventData"]!["sourceUuid"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|     Src ID | {data["eventData"]!["sceneItemId"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|  Src Index | {data["eventData"]!["sceneItemIndex"]}", StardewModdingAPI.LogLevel.Debug);
            } else if (data["eventType"]?.ToString() == "SceneItemRemoved") {
                ModEntry.ModMonitor.Log("A scene item was removed", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Scene Name | {data["eventData"]!["sceneName"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Scene UUID | {data["eventData"]!["sceneUuid"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|   Src Name | {data["eventData"]!["sourceName"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|   Src UUID | {data["eventData"]!["sourceUuid"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|     Src ID | {data["eventData"]!["sceneItemId"]}", StardewModdingAPI.LogLevel.Debug);
            } else if (data["eventType"]?.ToString() == "StreamStateChanged") {
                ModEntry.ModMonitor.Log("Stream state has changed", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|  Is Active | {data["eventData"]!["outputActive"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|      State | {data["eventData"]!["outputState"]}", StardewModdingAPI.LogLevel.Debug);
            } else if (data["eventType"]?.ToString() == "RecordingStateChanged") {
                ModEntry.ModMonitor.Log("Recording state has changed", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|  Is Active | {data["eventData"]!["outputActive"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|      State | {data["eventData"]!["outputState"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|       Path | {data["eventData"]!["outputPath"]}", StardewModdingAPI.LogLevel.Debug);
            } else if (data["eventType"]?.ToString() == "RecordFileChanged") {
                ModEntry.ModMonitor.Log("Recording state has changed", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|   New Path | {data["eventData"]!["newOutputPath"]}", StardewModdingAPI.LogLevel.Debug);
            }
        }

        private void HandleRequestResponse(JToken data) {
            var requestId = data["requestId"]?.ToString();

            if (requestId != null && pendingRequests.TryGetValue(requestId, out var tcs))
            {
                if (!tcs.Task.IsCompleted)
                {
                    tcs.SetResult(data);
                    pendingRequests.Remove(requestId);
                }
                else
                {
                    ModEntry.ModMonitor.Log("Attempted to complete an already completed TaskCompletionSource.", StardewModdingAPI.LogLevel.Warn);
                }
            }

            if (data["requestType"]?.ToString() == "GetVersion") {
                ModEntry.ModMonitor.Log("Get Version", StardewModdingAPI.LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| OBS Version | {data["responseData"]!["obsVersion"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|  WS Version | {data["responseData"]!["obsWebSocketVersion"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| RPC Version | {data["responseData"]!["rpcVersion"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|    Platform | {data["responseData"]!["platform"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|    Platform | {data["responseData"]!["platformDescription"]}", StardewModdingAPI.LogLevel.Debug);
            }
            else if (data["requestType"]?.ToString() == "GetRecordDirectory") {
                ModEntry.ModMonitor.Log("Get Record Directory", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|   Directory | {data["responseData"]!["recordDirectory"]}", StardewModdingAPI.LogLevel.Debug);
            }
            else if (data["requestType"]?.ToString() == "SetRecordDirectory") {
                ModEntry.ModMonitor.Log("Set Record Directory", StardewModdingAPI.LogLevel.Debug);
            }
            else if (data["requestType"]?.ToString() == "GetSourceActive") {
                ModEntry.ModMonitor.Log("Get Active Source", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|  Is Video Active | {data["responseData"]!["videoActive"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Is Video Showing | {data["responseData"]!["videoShowing"]}", StardewModdingAPI.LogLevel.Debug);
            }
            else if (data["requestType"]?.ToString() == "GetSceneList") {
                ModEntry.ModMonitor.Log("Get Scene List", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Current Scene Name | {data["responseData"]!["currentProgramSceneName"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Current Scene UUID | {data["responseData"]!["currentProgramSceneUuid"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|             Scenes | {String.Join(" |", data["responseData"]!["scenes"]!)}", StardewModdingAPI.LogLevel.Debug);
            }
            else if (data["requestType"]?.ToString() == "GetInputList") {
                ModEntry.ModMonitor.Log("Get Input List", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Inputs | {String.Join(" |", data["responseData"]!["inputs"]!)}", StardewModdingAPI.LogLevel.Debug);
            }
            else if (data["requestType"]?.ToString() == "GetInputKindList") {
                ModEntry.ModMonitor.Log("Get Input Kind List", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Input Kinds | {String.Join(" |", data["responseData"]!["inputKinds"]!)}", StardewModdingAPI.LogLevel.Debug);
            }
            else if (data["requestType"]?.ToString() == "GetInputSettings") {
                ModEntry.ModMonitor.Log("Get Input Settings", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Input Settings | {data["responseData"]!["inputSettings"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|     Input Kind | {data["responseData"]!["inputKind"]}", StardewModdingAPI.LogLevel.Debug);
            }
            else if (data["requestType"]?.ToString() == "SetInputSettings") {
                ModEntry.ModMonitor.Log("Set Input Settings", StardewModdingAPI.LogLevel.Debug);
            }
            else if (data["requestType"]?.ToString() == "GetSceneItemList") {
                ModEntry.ModMonitor.Log("Get Scene Item List", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Scene Items | {String.Join(" |", data["responseData"]!["sceneItems"]!)}", StardewModdingAPI.LogLevel.Debug);
            }
            else if (data["requestType"]?.ToString() == "GetStreamStatus") {
                ModEntry.ModMonitor.Log("Get Stream Status", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|         Output Active | {data["responseData"]!["outputActive"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|   Output Reconnecting | {data["responseData"]!["outputReconnecting"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|       Output Timecode | {data["responseData"]!["outputTimecode"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|       Output Duration | {data["responseData"]!["outputDuration"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|     Output Congestion | {data["responseData"]!["outputCongestion"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|          Output Bytes | {data["responseData"]!["outputBytes"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Output Skipped Frames | {data["responseData"]!["outputSkippedFrames"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|   Output Total Frames | {data["responseData"]!["outputTotalFrames"]}", StardewModdingAPI.LogLevel.Debug);
            }
            else if (data["requestType"]?.ToString() == "GetRecordStatus") {
                ModEntry.ModMonitor.Log("Get Record Status", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|   Output Active | {data["responseData"]!["outputActive"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|   Output Paused | {data["responseData"]!["outputPaused"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Output Timecode | {data["responseData"]!["outputTimecode"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Output Duration | {data["responseData"]!["outputDuration"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"|    Output Bytes | {data["responseData"]!["outputBytes"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.IsRecording = Convert.ToBoolean(data["responseData"]?["outputActive"]);
            }
            else if (data["requestType"]?.ToString() == "ToggleRecord") {
                ModEntry.ModMonitor.Log("Toggle Record", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Output Active | {data["responseData"]!["outputActive"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.IsRecording = Convert.ToBoolean(data["responseData"]!["outputActive"]);
            }
            else if (data["requestType"]?.ToString() == "StartRecord") {
                ModEntry.ModMonitor.Log("Start Record", StardewModdingAPI.LogLevel.Debug);
                ModEntry.IsRecording = true;
            }
            else if (data["requestType"]?.ToString() == "StopRecord") {
                ModEntry.ModMonitor.Log("StopRecord", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Output Path | {data["responseData"]!["outputPath"]}", StardewModdingAPI.LogLevel.Debug);
                ModEntry.IsRecording = false;
            }
            else if (data["requestType"]?.ToString() == "ToggleRecordPause") {
                ModEntry.ModMonitor.Log("Toggle Record Pause", StardewModdingAPI.LogLevel.Debug);
            }
            else if (data["requestType"]?.ToString() == "PauseRecord") {
                ModEntry.ModMonitor.Log("Pause Record", StardewModdingAPI.LogLevel.Debug);
            }
            else if (data["requestType"]?.ToString() == "ResumeRecord") {
                ModEntry.ModMonitor.Log("Resume Record", StardewModdingAPI.LogLevel.Debug);
            }
            else if (data["requestType"]?.ToString() == "CreateRecordChapter") {
                ModEntry.ModMonitor.Log("Create Record Chapter", StardewModdingAPI.LogLevel.Debug);
            }
            else if (data["requestType"]?.ToString() == "ToggleOutput") {
                ModEntry.ModMonitor.Log("Toggle Output", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Output Active | {data["responseData"]!["outputActive"]}", StardewModdingAPI.LogLevel.Debug);
            }
            else if (data["requestType"]?.ToString() == "StartOutput") {
                ModEntry.ModMonitor.Log("Start Output", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Output Name | {data["responseData"]!["outputName"]}", StardewModdingAPI.LogLevel.Debug);
            }
            else if (data["requestType"]?.ToString() == "StopOutput") {
                ModEntry.ModMonitor.Log("Stop Output", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Output Name | {data["responseData"]!["outputName"]}", StardewModdingAPI.LogLevel.Debug);
            }
            else if (data["requestType"]?.ToString() == "GetOutputSettings") {
                ModEntry.ModMonitor.Log("Get Output Settings", StardewModdingAPI.LogLevel.Debug);
                ModEntry.ModMonitor.Log($"| Scene Items | {String.Join(" |", data["responseData"]!["sceneItems"]!)}", StardewModdingAPI.LogLevel.Debug);
            }
            else if (data["requestType"]?.ToString() == "SetOutputSettings") {
                ModEntry.ModMonitor.Log("Set Output Settings", StardewModdingAPI.LogLevel.Debug);
            }
        }

        private async void AttemptReconnection() {
            if (currentReconnectAttempt < maxReconnectAttempts) {
                ModEntry.ModMonitor.Log($"Attempting to reconnect to OBS... (Attempt {currentReconnectAttempt + 1}/{maxReconnectAttempts})", StardewModdingAPI.LogLevel.Warn);
                currentReconnectAttempt++;
                await Connect(); // Attempt to reconnect
            } else {
                ModEntry.ModMonitor.Log($"Failed to reconnect to OBS after {maxReconnectAttempts} attempts.", StardewModdingAPI.LogLevel.Error);
                Game1.activeClickableMenu = new DialogueBox(ModEntry.Config.ConnNotifMessageDisconnect);
                currentReconnectAttempt = 0;
            }
        }

        public async Task<JToken> HandleOp6Requests(JToken data) {
            var requestId = Guid.NewGuid().ToString();
            data["requestId"] = requestId;

            var tcs = new TaskCompletionSource<JToken>();
            pendingRequests[requestId] = tcs;

            var request = new JObject {
                { "op", 6 },
                { "d", data }
            };

            ws.Send(request.ToString());

            return await tcs.Task;
        }
    }
}
