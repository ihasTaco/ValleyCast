using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using GenericModConfigMenu;

namespace ValleyCast {
    public class ModSettings {
        public static void Settings(IManifest ModManifest) {
            var configMenu = ModEntry.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

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
                getValue: () => ModEntry.Config.Password,
                setValue: value => ModEntry.Config.Password = value
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
