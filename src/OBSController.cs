using WebSocketSharp;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;
using StardewValley;
using StardewValley.Menus;

namespace ValleyCast {
    public class OBSController {
        private WebSocket ws;
        private readonly string url;
        private readonly string password;
        private bool currentlyConnected;
        private int currentAttempt;
        private readonly int maxReconnectAttempts;
        public bool IsRecording { get; private set; } = false;
        private TaskCompletionSource<JToken> tcs;

        public OBSController(string ipAddress, string port, string password, int maxReconnectAttempts) {
            this.url = $"ws://{ipAddress}:{port}";
            this.password = password;
            this.maxReconnectAttempts = maxReconnectAttempts;
            this.tcs = new TaskCompletionSource<JToken>();
        }

        public void Connect() {
            ws = new WebSocket(url, "obswebsocket.json");
            ws.OnMessage += OnMessageReceived;
            ws.OnError += OnError;
            ws.OnClose += OnClose;
            ws.Connect();
        }

        private void OnMessageReceived(object sender, MessageEventArgs e) {
            var response = JObject.parse(e.Data);
            int opCode = response["op"]?.Value<int>() ?? -1;

            if (opCode == 0) {
                HandleHello(response["d"]);
            } else if (opCode == 2) {
                HandleIdentified(response["d"]);
            } else if (opCode == 5) {
                HandleEvent(response["d"]);
            } else if (opCode == 7) {
                HandleRequestResponse(response["d"]);
            }
        }

        private void OnError(object sender, ErrorEventArgs e) {
            ModEntry.ModMonitor.Log($"OBS WebSocket Error: {e.Message}", LogLevel.Error);
        }

        private void OnClose(object sender, ErrorEventArgs e) {
            currentlyConnected = false;
            ModEntry.ModMonitor.Log("OBS WebSocket connection closed.", LogLevel.Warn);
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
                identifyRequest["d"]!["authentication"] = authString;
            }

            return identifyRequest;
        }

        private void HandleHello(JToken data) {
            var identifyRequest = CreateIdentifyRequest(data);
            ws.Send(identifyRequest.ToString());
        }

        private void HandleIdentified(JToken data) {
            if (data["negotiatedRpcVersion"] == 1) {
                currentlyConnected = true;
            } else {
                currentlyConnected = false;
            }
        }

        private void HandleEvent(JToken data) {
            // These events are the ones that I think I will need to get the mod to a point I think is finished.
            // just in case I need to add more, https://github.com/obsproject/obs-websocket/blob/master/docs/generated/protocol.md#events
            // These Event handlers are all placeholders for now, eventually I will add functionality to actually do stuff with these
            if (data["eventType"] == "ExitStarted") {
                ModEntry.ModMonitor.Log("OBS is exiting.", LogLevel.Alert);
            } else if (data["eventType"] == "SceneCreated") {
                ModEntry.ModMonitor.Log("A new scene was created", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Scene Name | {data["eventData"]["sceneName"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Scene UUID | {data["eventData"]["sceneUuid"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   Is Group | {data["eventData"]["isGroup"]}", LogLevel.Alert);
            } else if (data["eventType"] == "SceneRemoved") {
                ModEntry.ModMonitor.Log("A scene was removed", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Scene Name | {data["eventData"]["sceneName"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Scene UUID | {data["eventData"]["sceneUuid"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   Is Group | {data["eventData"]["isGroup"]}", LogLevel.Alert);
            } else if (data["eventType"] == "SceneNameChanged") {
                ModEntry.ModMonitor.Log("A scene name was changed", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Scene UUID | {data["eventData"]["sceneUuid"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   Old Name | {data["eventData"]["oldSceneName"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   New Name | {data["eventData"]["sceneName"]}", LogLevel.Alert);
            } else if (data["eventType"] == "SceneListChanged") {
                ModEntry.ModMonitor.Log("The scene list was changed", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|     Scenes | {string.join(" |", data["eventData"]["scenes"])}", LogLevel.Alert);
            } else if (data["eventType"] == "InputCreated") {
                ModEntry.ModMonitor.Log("A new input was created", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Input Name | {data["eventData"]["inputName"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Input UUID | {data["eventData"]["inputUuid"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Input Kind | {data["eventData"]["inputKind"]}", LogLevel.Alert);
            } else if (data["eventType"] == "InputRemoved") {
                ModEntry.ModMonitor.Log("An input name was removed", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Input Name | {data["eventData"]["inputName"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Input UUID | {data["eventData"]["inputUuid"]}", LogLevel.Alert);
            } else if (data["eventType"] == "InputNameChanged") {
                ModEntry.ModMonitor.Log("An input name was changed", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Input UUID | {data["eventData"]["inputUuid"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   Old Name | {data["eventData"]["oldInputName"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   New Name | {data["eventData"]["inputName"]}", LogLevel.Alert);
            } else if (data["eventType"] == "SceneItemCreated") {
                ModEntry.ModMonitor.Log("A scene item was created", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Scene Name | {data["eventData"]["sceneName"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Scene UUID | {data["eventData"]["sceneUuid"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   Src Name | {data["eventData"]["sourceName"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   Src UUID | {data["eventData"]["sourceUuid"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|     Src ID | {data["eventData"]["sceneItemId"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|  Src Index | {data["eventData"]["sceneItemIndex"]}", LogLevel.Alert);
            } else if (data["eventType"] == "SceneItemRemoved") {
                ModEntry.ModMonitor.Log("A scene item was removed", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Scene Name | {data["eventData"]["sceneName"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Scene UUID | {data["eventData"]["sceneUuid"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   Src Name | {data["eventData"]["sourceName"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   Src UUID | {data["eventData"]["sourceUuid"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|     Src ID | {data["eventData"]["sceneItemId"]}", LogLevel.Alert);
            } else if (data["eventType"] == "StreamStateChanged") {
                ModEntry.ModMonitor.Log("Stream state has changed", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|  Is Active | {data["eventData"]["outputActive"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|      State | {data["eventData"]["outputState"]}", LogLevel.Alert);
            } else if (data["eventType"] == "RecordingStateChanged") {
                ModEntry.ModMonitor.Log("Recording state has changed", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|  Is Active | {data["eventData"]["outputActive"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|      State | {data["eventData"]["outputState"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|       Path | {data["eventData"]["outputPath"]}", LogLevel.Alert);
            } else if (data["eventType"] == "RecordFileChanged") {
                ModEntry.ModMonitor.Log("Recording state has changed", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   New Path | {data["eventData"]["newOutputPath"]}", LogLevel.Alert);
            }
        }

        private void HandleRequestResponse(JToken data) {
            // Check if this is a response to our OpCode 6 request
            if (data["requestId"] != null) {
                // Complete the TaskCompletionSource with the response data
                tcs.SetResult(data);
            }

            if (data["requestType"] == "GetVersion") {
                ModEntry.ModMonitor.Log("Get Version", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| OBS Version | {data["responseData"]["obsVersion"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|  WS Version | {data["responseData"]["obsWebSocketVersion"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| RPC Version | {data["responseData"]["rpcVersion"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|    Platform | {data["responseData"]["platform"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|    Platform | {data["responseData"]["platformDescription"]}", LogLevel.Alert);
            }
            else if (data["requestType"] == "GetRecordDirectory") {
                ModEntry.ModMonitor.Log("Get Record Directory", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   Directory | {data["responseData"]["recordDirectory"]}", LogLevel.Alert);
            }
            else if (data["requestType"] == "SetRecordDirectory") {
                ModEntry.ModMonitor.Log("Set Record Directory", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Succeeded | {data["responseData"]["result"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   Comment | {data["responseData"]["comment"]}", LogLevel.Alert);
            }
            else if (data["requestType"] == "GetSourceActive") {
                ModEntry.ModMonitor.Log("Get Active Source", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|  Is Video Active | {data["responseData"]["videoActive"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Is Video Showing | {data["responseData"]["videoShowing"]}", LogLevel.Alert);
            }
            else if (data["requestType"] == "GetSceneList") {
                ModEntry.ModMonitor.Log("Get Scene List", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Current Scene Name | {data["responseData"]["currentProgramSceneName"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Current Scene UUID | {data["responseData"]["currentProgramSceneUuid"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|             Scenes | {string.join(" |", data["responseData"]["scenes"])}", LogLevel.Alert);
            }
            else if (data["requestType"] == "GetInputList") {
                ModEntry.ModMonitor.Log("Get Input List", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Inputs | {string.join(" |", data["responseData"]["inputs"])}", LogLevel.Alert);
            }
            else if (data["requestType"] == "GetInputKindList") {
                ModEntry.ModMonitor.Log("Get Input Kind List", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Input Kinds | {string.join(" |", data["responseData"]["inputKinds"])}", LogLevel.Alert);
            }
            else if (data["requestType"] == "GetInputSettings") {
                ModEntry.ModMonitor.Log("Get Input Settings", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Input Settings | {data["responseData"]["inputSettings"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|     Input Kind | {data["responseData"]["inputKind"]}", LogLevel.Alert);
            }
            else if (data["requestType"] == "SetInputSettings") {
                ModEntry.ModMonitor.Log("Set Input Settings", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Succeeded | {data["responseData"]["result"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   Comment | {data["responseData"]["comment"]}", LogLevel.Alert);
            }
            else if (data["requestType"] == "GetSceneItemList") {
                ModEntry.ModMonitor.Log("Get Scene Item List", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Scene Items | {string.join(" |", data["responseData"]["sceneItems"])}", LogLevel.Alert);
            }
            else if (data["requestType"] == "GetStreamStatus") {
                ModEntry.ModMonitor.Log("Get Stream Status", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|         Output Active | {data["responseData"]["outputActive"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   Output Reconnecting | {data["responseData"]["outputReconnecting"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|       Output Timecode | {data["responseData"]["outputTimecode"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|       Output Duration | {data["responseData"]["outputDuration"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|     Output Congestion | {data["responseData"]["outputCongestion"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|          Output Bytes | {data["responseData"]["outputBytes"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Output Skipped Frames | {data["responseData"]["outputSkippedFrames"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   Output Total Frames | {data["responseData"]["outputTotalFrames"]}", LogLevel.Alert);
            }
            else if (data["requestType"] == "GetRecordStatus") {
                ModEntry.ModMonitor.Log("Get Record Status", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   Output Active | {data["responseData"]["outputActive"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   Output Paused | {data["responseData"]["outputPaused"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Output Timecode | {data["responseData"]["outputTimecode"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Output Duration | {data["responseData"]["outputDuration"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|    Output Bytes | {data["responseData"]["outputBytes"]}", LogLevel.Alert);
                IsRecording = data["responseData"]?["outputActive"];
            }
            else if (data["requestType"] == "ToggleRecord") {
                ModEntry.ModMonitor.Log("Toggle Record", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Output Active | {data["responseData"]["outputActive"]}", LogLevel.Alert);
            }
            else if (data["requestType"] == "StartRecord") {
                ModEntry.ModMonitor.Log("", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Succeeded | {data["responseData"]["result"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   Comment | {data["responseData"]["comment"]}", LogLevel.Alert);
                IsRecording = true;
            }
            else if (data["requestType"] == "StopRecord") {
                ModEntry.ModMonitor.Log("StopRecord", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Output Path | {data["responseData"]["outputPath"]}", LogLevel.Alert);
                IsRecording = false;
            }
            else if (data["requestType"] == "ToggleRecordPause") {
                ModEntry.ModMonitor.Log("Toggle Record Pause", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Succeeded | {data["responseData"]["result"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   Comment | {data["responseData"]["comment"]}", LogLevel.Alert);
            }
            else if (data["requestType"] == "PauseRecord") {
                ModEntry.ModMonitor.Log("Pause Record", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Succeeded | {data["responseData"]["result"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   Comment | {data["responseData"]["comment"]}", LogLevel.Alert);
            }
            else if (data["requestType"] == "ResumeRecord") {
                ModEntry.ModMonitor.Log("Resume Record", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Succeeded | {data["responseData"]["result"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   Comment | {data["responseData"]["comment"]}", LogLevel.Alert);
            }
            else if (data["requestType"] == "CreateRecordChapter") {
                ModEntry.ModMonitor.Log("Create Record Chapter", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Succeeded | {data["responseData"]["result"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   Comment | {data["responseData"]["comment"]}", LogLevel.Alert);
            }
            else if (data["requestType"] == "ToggleOutput") {
                ModEntry.ModMonitor.Log("Toggle Output", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Output Active | {data["responseData"]["outputActive"]}", LogLevel.Alert);
            }
            else if (data["requestType"] == "StartOutput") {
                ModEntry.ModMonitor.Log("Start Output", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Output Name | {data["responseData"]["outputName"]}", LogLevel.Alert);
            }
            else if (data["requestType"] == "StopOutput") {
                ModEntry.ModMonitor.Log("Stop Output", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Output Name | {data["responseData"]["outputName"]}", LogLevel.Alert);
            }
            else if (data["requestType"] == "GetOutputSettings") {
                ModEntry.ModMonitor.Log("Get Output Settings", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Scene Items | {string.join(" |", data["responseData"]["sceneItems"])}", LogLevel.Alert);
            }
            else if (data["requestType"] == "SetOutputSettings") {
                ModEntry.ModMonitor.Log("Set Output Settings", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"| Succeeded | {data["responseData"]["result"]}", LogLevel.Alert);
                ModEntry.ModMonitor.Log($"|   Comment | {data["responseData"]["comment"]}", LogLevel.Alert);
            }
        }


        private void AttemptReconnection(int maxAttempts) {
            if (currentAttempt < maxAttempts) {
                ModEntry.ModMonitor.Log($"Attempting to reconnect to OBS... (Attempt {currentAttempt + 1}/{maxAttempts})", StardewModdingAPI.LogLevel.Warn);
                currentAttempt++;
                ConnectToWebSocket(maxAttempts); // Attempt to reconnect
            } else {
                ModEntry.ModMonitor.Log($"Failed to reconnect to OBS after {maxAttempts} attempts.", StardewModdingAPI.LogLevel.Error);
                Game1.activeClickableMenu = new DialogueBox(ModEntry.Config.ConnNotifMessageDisconnect);
                currentAttempt = 0;
            }
        }

        public async Task<JToken> HandleOp6Requests(JToken data) {
            tcs = new TaskCompletionSource<JToken>();

            var request = new JObject {
                { "op", 6 },
                { "d", data }
            };

            ws.Send(request.ToString());

            return await tcs.Task;
        }
    }
}
