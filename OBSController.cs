using WebSocketSharp;
using StardewModdingAPI;

namespace ValleyCast
{
    public class OBSController
    {
        private readonly IMonitor Monitor;
        private readonly string websocketUrl;
        private readonly string password;

        public OBSController(string ipAddress, string port, string password, IMonitor monitor)
        {
            this.websocketUrl = $"ws://{ipAddress}:{port}";
            this.password = password;
            this.Monitor = monitor;
        }

        public bool TestConnection()
        {
            try
            {
                using (var ws = new WebSocket(this.websocketUrl))
                {
                    ws.SetCredentials(this.password, null, true);
                    ws.Connect();

                    if (ws.IsAlive)
                    {
                        Monitor.Log("Successfully connected to OBS WebSocket server.", StardewModdingAPI.LogLevel.Info);
                        return true;
                    }
                    else
                    {
                        Monitor.Log("Failed to connect to OBS WebSocket server.", StardewModdingAPI.LogLevel.Warn);
                        return false;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Monitor.Log($"Error connecting to OBS WebSocket server: {ex.Message}", StardewModdingAPI.LogLevel.Error);
                return false;
            }
        }

        // You can add more methods here for other OBS operations, like starting/stopping recording, switching scenes, etc.
    }
}