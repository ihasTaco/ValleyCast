using WebSocketSharp;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;
using StardewValley;
using StardewValley.Menus;

namespace ValleyCast
{
    public class OBSController
    {
        private readonly string websocketUrl;
        private readonly string password;
        public WebSocket ws = null!;

        public bool IsRecording { get; private set; } = false;

        public OBSController(string ipAddress, string port, string password)
        {
            this.websocketUrl = $"ws://{ipAddress}:{port}";
            this.password = password;

            ConnectToWebSocket(0);

            // Add the handler globally
            ws.OnMessage += HandleWebSocketResponse!;
        }

        public void ConnectToWebSocket(int? attempts = null) {
            // if maxAttempts isn't set then use the setting, else use the provided amount
            int maxAttempts = attempts ?? ModEntry.Config.ReconnectAttempts! - 1;

            ws = new WebSocket(this.websocketUrl, "obswebsocket.json");
            PlayerNotify playerNotify = new();

            ws.OnMessage += (sender, e) => {
                JObject response = JObject.Parse(e.Data);
                int opCode = response["op"]!.Value<int>();

                switch (opCode) {
                    case 0: // Send the Hello message to start connection process
                        HandleHello(response["d"]!);
                        break;
                    case 2: // Identified
                        ModEntry.ModMonitor.Log("OBS WebSocket successfully identified.", StardewModdingAPI.LogLevel.Info);

                        try {
                            // When the OBS WebSocket is connected, try to send a message to the player
                            Game1.activeClickableMenu = new DialogueBox(ModEntry.Config.ConnNotifMessageConnect);
                        } catch (Exception ex){
                            // If there is an issue trying to display the dialog box try to send a status popup instead
                            // This will also cause an error... probably...
                            //PlayerNotify.ShowStatusPopup(ModEntry.Config.ConnNotifMessageConnect, 1);
                            ModEntry.ModMonitor.Log(ModEntry.Config.ConnNotifMessageConnect, StardewModdingAPI.LogLevel.Error);
                        }

                        // Reset the current reconnect attempt variable
                        currentAttempt = 0;
                        break;
                    case 7:
                        ModEntry.ModMonitor.Log("OBS WebSocket successfully identified.", StardewModdingAPI.LogLevel.Info);
                        break;
                    default:
                        ModEntry.ModMonitor.Log($"Received unexpected OpCode: {opCode}", StardewModdingAPI.LogLevel.Warn);
                        break;
                }
            };

            ws.OnError += (sender, e) => {
                ModEntry.ModMonitor.Log($"OBS WebSocket Error: {e.Message}", StardewModdingAPI.LogLevel.Error);
            };

            ws.OnClose += (sender, e) => {
                PlayerNotify.ShowStatusPopup($"Trying to reconnect to OBS (Attempt {currentAttempt + 1}/{ModEntry.Config.ReconnectAttempts})", 3);
                ModEntry.ModMonitor.Log("OBS WebSocket connection closed.", StardewModdingAPI.LogLevel.Warn);
                AttemptReconnection(maxAttempts);
            };

            ws.Connect();
        }

        private void HandleHello(JToken helloData) {
            ModEntry.ModMonitor.Log("Received Hello from OBS WebSocket.", StardewModdingAPI.LogLevel.Info);

            int rpcVersion = helloData["rpcVersion"]!.Value<int>();
            string authChallenge = helloData["authentication"]?["challenge"]?.Value<string>()!;
            string salt = helloData["authentication"]?["salt"]?.Value<string>()!;

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
                ModEntry.ModMonitor.Log("OBS WebSocket requires authentication.", StardewModdingAPI.LogLevel.Info);
                string authString = CreateAuthenticationString(authChallenge, salt);
                identifyRequest["d"]!["authentication"] = authString;
            }

            ws.Send(identifyRequest.ToString());
        }

        private string CreateAuthenticationString(string authChallenge, string salt) {
            // Step 1: Concatenate the password with the salt
            string passwordSalt = this.password + salt;

            // Step 2: Generate an SHA256 binary hash of the result and base64 encode it
            byte[] passwordSaltBytes = Encoding.UTF8.GetBytes(passwordSalt);
            byte[] sha256HashBytes;

            using (SHA256 sha256 = SHA256.Create()) {
                sha256HashBytes = sha256.ComputeHash(passwordSaltBytes);
            }

            string base64Secret = Convert.ToBase64String(sha256HashBytes);

            // Step 3: Concatenate the base64 secret with the challenge
            string secretChallenge = base64Secret + authChallenge;

            // Step 4: Generate a binary SHA256 hash of that result and base64 encode it
            byte[] secretChallengeBytes = Encoding.UTF8.GetBytes(secretChallenge);

            using (SHA256 sha256 = SHA256.Create()) {
                sha256HashBytes = sha256.ComputeHash(secretChallengeBytes);
            }

            string authResponse = Convert.ToBase64String(sha256HashBytes);

            return authResponse;
        }

        public void StartRecording() {
            var startRecordingRequest = new JObject {
                { "op", 6 }, // OpCode 6 for making a request
                { "d", new JObject {
                        { "requestType", "StartRecord" },
                        { "requestId", Guid.NewGuid().ToString() } // Generate a unique request ID
                    }
                }
            };
            ws.Send(startRecordingRequest.ToString());
        }

        public void StopRecording() {
            var stopRecordingRequest = new JObject {
                { "op", 6 }, // OpCode 6 for making a request
                { "d", new JObject {
                        { "requestType", "StopRecord" },
                        { "requestId", Guid.NewGuid().ToString() } // Generate a unique request ID
                    }
                }
            };
            ws.Send(stopRecordingRequest.ToString());
        }
        public void CheckRecordingStatus()
        {
            var checkRecordingStatusRequest = new JObject {
                { "op", 6 }, // OpCode 6 for making a request
                { "d", new JObject {
                        { "requestType", "GetRecordStatus" },
                        { "requestId", Guid.NewGuid().ToString() } // Generate a unique request ID
                    }
                }
            };

            ws.Send(checkRecordingStatusRequest.ToString());
            ModEntry.ModMonitor.Log("Sent GetRecordStatus request to OBS WebSocket.", StardewModdingAPI.LogLevel.Info);
        }

        // Add a method to handle responses globally
        private void HandleWebSocketResponse(object sender, MessageEventArgs e)
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

                    if (outputActive)
                    {
                        ModEntry.ModMonitor.Log("OBS is currently recording.", StardewModdingAPI.LogLevel.Info);
                        IsRecording = true;
                    }
                    else
                    {
                        ModEntry.ModMonitor.Log("OBS is not recording.", StardewModdingAPI.LogLevel.Info);
                        IsRecording = false;
                    }
                }
            }
        }

        private int currentAttempt = 0;
        private void AttemptReconnection(int maxAttempts)
        {
            if (currentAttempt < maxAttempts)
            {
                ModEntry.ModMonitor.Log($"Attempting to reconnect to OBS... (Attempt {currentAttempt + 1}/{maxAttempts})", StardewModdingAPI.LogLevel.Warn);
                currentAttempt++;
                ConnectToWebSocket(maxAttempts); // Attempt to reconnect
            }
            else
            {
                ModEntry.ModMonitor.Log($"Failed to reconnect to OBS after {maxAttempts} attempts.", StardewModdingAPI.LogLevel.Error);
                Game1.activeClickableMenu = new DialogueBox(ModEntry.Config.ConnNotifMessageDisconnect);
                currentAttempt = 0;
            }
        }
    }
}
