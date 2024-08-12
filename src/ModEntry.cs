using StardewModdingAPI;
using StardewModdingAPI.Events;
using GenericModConfigMenu;
using ValleyCast;
using static System.Net.Mime.MediaTypeNames;

namespace ValleyCast
{
    public class ModEntry : Mod
    {
        private ModConfig Config = null!;
        private OBSController obsController = null!;

        public override void Entry(IModHelper helper)
        {
            // Load the config using the new ModConfig class
            this.Config = helper.ReadConfig<ModConfig>();

            // Initialize OBSController with the current config values
            this.obsController = new OBSController(Config.OBSWebSocketIP, Config.OBSWebSocketPort, Config.Password, this.Monitor);

            // Register the GameLaunched event
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched!;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            configMenu.AddSectionTitle(
                mod: this.ModManifest, 
                text:() =>  "OBS WebSocket Authorization", 
                tooltip: () => "Authorization settings for OBS WebSocket Server"
            );

            // Add options to the mod config menu
            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "OBS WebSocket IP Address",
                getValue: () => this.Config.OBSWebSocketIP,
                setValue: value =>
                {
                    this.Config.OBSWebSocketIP = value;
                    this.obsController = new OBSController(Config.OBSWebSocketIP, Config.OBSWebSocketPort, Config.Password, this.Monitor);
                    this.obsController.TestConnection();
                }
            );

            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "OBS WebSocket Port",
                getValue: () => this.Config.OBSWebSocketPort,
                setValue: value =>
                {
                    this.Config.OBSWebSocketPort = value;
                    this.obsController = new OBSController(Config.OBSWebSocketIP, Config.OBSWebSocketPort, Config.Password, this.Monitor);
                    this.obsController.TestConnection();
                }
            );

            configMenu.AddTextOption(
                mod: this.ModManifest,
                name: () => "OBS WebSocket Password",
                getValue: () => this.Config.Password,
                setValue: value =>
                {
                    this.Config.Password = value;
                    this.obsController = new OBSController(Config.OBSWebSocketIP, Config.OBSWebSocketPort, Config.Password, this.Monitor);
                    this.obsController.TestConnection();
                }
            );
        }
    }
}