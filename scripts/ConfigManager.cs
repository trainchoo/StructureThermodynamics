using BepInEx.Configuration;
using BepInEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace StructureThermodynamicsOverhaulMod.scripts
{
    public static class ConfigManager
    {
        private static readonly string filename = "StructureThermodynamics_config.cfg";
        private static ConfigFile configFile;

        public static float Config_ThermalMultiplier = 1.0f;
        public static bool Config_EnableDebugLogInChat = false;
        public static bool Config_EnableDebugHomeKey = true;

        public static void ConfigLoad()
        {
            configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, filename), true);
            bindConfigs();
        }
        private static void bindConfigs()
        {
            ConfigEntry<float> thermalRateMultiplier = configFile.Bind(
                "General",      // The section under which the option is shown
                "thermalRateMultiplier",  // The key of the configuration option in the configuration file
                Config_ThermalMultiplier, // The default value
                "Sets the rate of heat transfer through solid blocks. Lower numbers reduce thermal effects from environmet, making the game easier. 0 = disables thermal effects; 1.0 = default; 10.0 = realistic (this would make the game too difficult)"); // Description of the option to show in the config file
            Config_ThermalMultiplier = thermalRateMultiplier.Value;

            ConfigEntry<bool> debugLogInChat = configFile.Bind(
               "Debug",      // The section under which the option is shown
               "debugLogInConsole",  // The key of the configuration option in the configuration file
               Config_EnableDebugLogInChat, // The default value
               "Set to true to enable the debug messages in the F3/console window. Warning enabaling this will spam you every thermal update tick (2 fps)"); // Description of the option to show in the config file
            Config_EnableDebugLogInChat = debugLogInChat.Value;

            ConfigEntry<bool> debugKeyLog = configFile.Bind(
               "Debug",      // The section under which the option is shown
               "debugForceUpdate",  // The key of the configuration option in the configuration file
               Config_EnableDebugHomeKey, // The default value
               "If set to true, will enable force structure recalculation (which only happens when you build blocks) when you press the \"Home\" key. Output log will be printed to your console window (F3)"); // Description of the option to show in the config file
            Config_EnableDebugHomeKey = debugKeyLog.Value;
        }
    }
}
