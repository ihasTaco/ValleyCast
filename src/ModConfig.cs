using StardewModdingAPI;

namespace ValleyCast
{
    public class ModConfig
    {
        public bool FirstLoad { get; set; } = true;
        public string OBSWebSocketIP { get; set; } = "localhost";
        public string OBSWebSocketPort { get; set; } = "4455";
        public string Password { get; set; } = "";
        public int ReconnectAttempts { get; set; } = 5;
        public bool EnableConnNotif { get; set; } = true;
        public string ConnNotifMessageDisconnect { get; set; } = "Whoops, OBS got disconnected! Check if it's started.";
        public string ConnNotifMessageConnect { get; set; } = "OBS is connected, and ready to go!";
        public string DayCounterSource { get; set; } = "DayText";
        public string DayCounterFormat { get; set; } = "Day {day}, {season}, Year {year}";
        public bool ReloadTrigger { get; set; } = false;

        public bool WeeklyRecording { get; set; } = false;
        public bool MonthlyRecording { get; set; } = false;
        public bool DailyRecording { get; set; } = true; // default to true
    }
}