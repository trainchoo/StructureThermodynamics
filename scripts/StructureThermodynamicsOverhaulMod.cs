using Assets.Scripts;
using BepInEx;
using HarmonyLib;
using StationeersMods.Interface;


namespace StructureThermodynamicsOverhaulMod.scripts
{
    class StructureThermodynamicsOverhaulMod : ModBehaviour
    {
        public override void OnLoaded(ContentHandler contentHandler)
        {
            Harmony harmony = new Harmony("StructureThermodynamicsOverhaul");
            harmony.PatchAll();
            ConfigManager.ConfigLoad();
        }

    }
}