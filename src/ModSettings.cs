using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using GenericModConfigMenu;

namespace ValleyCast {
    public class ModSettings {
        public static void Settings(IManifest ModManifest) {
            var configMenu = ModEntry.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;
            string currentPassword = ModEntry.Config.Password;

            // register mod
            configMenu.Register(
                mod: ModManifest,
                reset: () => ModEntry.Config = new ModConfig(),
                save: () => ModEntry.Helper.WriteConfig(ModEntry.Config!)
            );

            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => "OBS WebSocket Authorization",
                tooltip: () => "Authorization settings for OBS WebSocket Server"
            );

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "OBS WebSocket IP Address",
                getValue: () => ModEntry.Config.OBSWebSocketIP,
                setValue: value => ModEntry.Config.OBSWebSocketIP = value
            );

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "OBS WebSocket Port",
                getValue: () => ModEntry.Config.OBSWebSocketPort,
                setValue: value => ModEntry.Config.OBSWebSocketPort = value
            );

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "OBS WebSocket Password",
                getValue: () => new string('*', ModEntry.Config.Password.Length),
                setValue: value => {
                    // Update the actual password only if the user enters a value different from asterisks
                    if (value != new string('*', currentPassword.Length) && !string.IsNullOrEmpty(value))
                    {
                        ModEntry.Config.Password = value;
                        currentPassword = value; // Update the currentPassword reference
                    }
                }
            );

            // Add the reconnect button only if the player is in a game
            configMenu.AddComplexOption(
                mod: ModManifest,
                name: () => "Connect to OBS",
                draw: (spriteBatch, position) => {
                    if (Game1.hasLoadedGame)
                    {
                        var buttonBounds = new Rectangle((int)position.X, (int)position.Y, 150, 40);
                        spriteBatch.Draw(Game1.mouseCursors, buttonBounds, new Rectangle(128, 256, 64, 64), Color.White);
                        Utility.drawTextWithShadow(spriteBatch, "Reconnect", Game1.dialogueFont, new Vector2(position.X + 20, position.Y + 10), Color.Black);

                        if (Game1.input.GetMouseState().LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed &&
                            buttonBounds.Contains(Game1.getMouseX(), Game1.getMouseY()))
                        {
                            try
                            {
                                ModEntry.OBSController.ConnectToWebSocket(1);
                            }
                            catch (Exception ex)
                            {
                                ModEntry.ModMonitor.Log($"Error during OBS reconnect: {ex.Message}", LogLevel.Error);
                            }
                        }
                    }
                },
                height: () => 40,
                fieldId: "ConnectButton"
            );

            configMenu.AddSectionTitle(
                mod: ModManifest,
                text: () => "OBS Connection Notifications",
                tooltip: () => "Customize the connection notifications for OBS"
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Enable Connection Notifications",
                getValue: () => ModEntry.Config.EnableConnNotif,
                setValue: value => ModEntry.Config.EnableConnNotif = value
            );

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "Connect Notification Message",
                getValue: () => ModEntry.Config.ConnNotifMessageConnect,
                setValue: value => ModEntry.Config.ConnNotifMessageConnect = value
            );

            configMenu.AddTextOption(
                mod: ModManifest,
                name: () => "Disconnect Notification Message",
                getValue: () => ModEntry.Config.ConnNotifMessageDisconnect,
                setValue: value => ModEntry.Config.ConnNotifMessageDisconnect = value
            );
        }
    }
}
