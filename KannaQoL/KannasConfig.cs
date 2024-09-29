using BepInEx.Configuration;
using System.IO;

namespace SylmarDev.KannasQoL
{
    public class KannasConfig
    {
        public static ConfigEntry<bool> enableFastScrapper;
        public static ConfigEntry<bool> enableBazaarScrapper;
        public static ConfigEntry<bool> enableSeerPing;
        public static ConfigEntry<bool> enableInstaTeleporter;
        public static ConfigEntry<bool> enableCleansingPool;
        public static ConfigEntry<bool> enableSingleFrog;
        public static ConfigEntry<int> frogStatueCost;
        public static ConfigEntry<bool> falseSonPortalInPlanetarium;

        public void Init(string configPath)
        {
            var config = new ConfigFile(Path.Combine(configPath, KannasQoL.PluginGUID + ".cfg"), true);

            enableFastScrapper = config.Bind("Tweaks", "Enable Fast Scrapper", true, "Set to true to enable fast scrapper.");
            enableBazaarScrapper = config.Bind("Tweaks", "Enable Scrapper in Bazaar", true, "Set to true to put scrapper in the Bazaar in Time.");
            enableSeerPing = config.Bind("Tweaks", "Enable Seer Ping", true, "Set to true to ping Lunar Seers for destination.");
            enableInstaTeleporter = config.Bind("Tweaks", "Enable Instant Teleporter", true, "Set to true to instantly finish charging teleporter after boss is killed, with time adjusted.");
            enableCleansingPool = config.Bind("Tweaks", "Enable Cleansing Pool", true, "Set to true for guaranteed Cleansing Pool on Alphesian Sanctuary.");
            enableSingleFrog = config.Bind("Tweaks", "Enable Single Frog Pet", true, "Set to true to only pet the Glass Frog once for Deep Void Portal.");
            frogStatueCost = config.Bind("Tweaks", "Frog Statue Cost", 10, "Required Lunar Coins per Glass Frog Pet.");
            falseSonPortalInPlanetarium = config.Bind("Tweaks", "False Son after Voidling", true, "Set to true to enable a portal to Prime Meridian after Voidling is killed.");
        }
    }
}
