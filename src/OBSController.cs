using WebSocketSharp;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Text;
using StardewValley;
using StardewValley.Menus;
using StardewModdingAPI;

namespace ValleyCast
{
    public class OBSController
    {
        private readonly IMonitor Monitor;
        private readonly string websocketUrl;
        private readonly string password;
        private WebSocket ws = null!;

        public bool IsRecording { get; private set; } = false;

        public OBSController(string ipAddress, string port, string password, IMonitor monitor) {
            this.websocketUrl = $"ws://{ipAddress}:{port}";
            this.password = password;
            this.Monitor = monitor;

            ConnectToWebSocket();
        }

        private void ConnectToWebSocket() {
            ws = new WebSocket(this.websocketUrl, "obswebsocket.json");
            PlayerNotify playerNotify = new();

            ws.OnMessage += (sender, e) => {
                JObject response = JObject.Parse(e.Data);
                int opCode = response["op"]!.Value<int>();

                switch (opCode) {
                    case 0: // Hello
                        HandleHello(response["d"]!);
                        break;
                    case 2: // Identified
                        Monitor.Log("OBS WebSocket successfully identified.", StardewModdingAPI.LogLevel.Info);

                        // When the OBS WebSocket is connected, send a message to the player
                        Game1.activeClickableMenu = new DialogueBox(ModEntry.Config.ConnNotifMessageConnect);

                        // Reset the current reconnect attempts variable
                        currentReconnectionAttempts = 0;
                        break;
                    default:
                        Monitor.Log($"Received unexpected OpCode: {opCode}", StardewModdingAPI.LogLevel.Warn);
                        break;
                }
            };

            ws.OnError += (sender, e) => {
                Monitor.Log($"OBS WebSocket Error: {e.Message}", StardewModdingAPI.LogLevel.Error);
            };

            ws.OnClose += (sender, e) => {
                Monitor.Log("OBS WebSocket connection closed.", StardewModdingAPI.LogLevel.Warn);
                AttemptReconnection();
                // When the OBS WebSocket connection is closed, send a message to the player
                //Game1.activeClickableMenu = new DialogueBox(ModEntry.Config.ConnNotifMessageDisconnect);
            };

            ws.Connect();
        }

        private void HandleHello(JToken helloData) {
            Monitor.Log("Received Hello from OBS WebSocket.", StardewModdingAPI.LogLevel.Info);

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
                Monitor.Log("OBS WebSocket requires authentication.", StardewModdingAPI.LogLevel.Info);
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
        public void CheckRecordingStatus() {
            var checkRecordingStatusRequest = new JObject {
                 { "op", 6 }, // OpCode 6 for making a request
                 { "d", new JObject {
                         { "requestType", "GetRecordStatus" },
                         { "requestId", Guid.NewGuid().ToString() } // Generate a unique request ID
                     }
                 }
             };

            ws.Send(checkRecordingStatusRequest.ToString());
            Monitor.Log("Sent GetRecordStatus request to OBS WebSocket.", StardewModdingAPI.LogLevel.Info);

            ws.OnMessage += (sender, e) => {
                var response = JObject.Parse(e.Data);
                string requestType = response["d"]?["requestType"]?.ToString()!;

                if (requestType == "GetRecordStatus") {
                    bool outputActive = response["d"]?["responseData"]?["outputActive"]?.Value<bool>() ?? false;

                    if (outputActive) {
                        Monitor.Log("OBS is currently recording.", StardewModdingAPI.LogLevel.Info);
                        IsRecording = true;
                    } else {
                        Monitor.Log("OBS is not recording.", StardewModdingAPI.LogLevel.Info);
                        IsRecording = false;
                    }
                }
            };
        }

        private int currentReconnectionAttempts = 0;
        private void AttemptReconnection()
        {
            int maxReconnectionAttempts = ModEntry.Config.ReconnectAttempts;
            if (currentReconnectionAttempts < maxReconnectionAttempts)
            {
                Monitor.Log($"Attempting to reconnect to OBS... (Attempt {currentReconnectionAttempts + 1}/{maxReconnectionAttempts})", StardewModdingAPI.LogLevel.Warn);
                currentReconnectionAttempts++;
                ConnectToWebSocket(); // Attempt to reconnect
            }
            else
            {
                Monitor.Log($"Failed to reconnect to OBS after {maxReconnectionAttempts} attempts.", StardewModdingAPI.LogLevel.Error);
                Game1.activeClickableMenu = new DialogueBox(ModEntry.Config.ConnNotifMessageDisconnect);
                currentReconnectionAttempts = 0;
            }
        }
    }
}
