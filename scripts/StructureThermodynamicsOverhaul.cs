using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts.Serialization;
using Assets.Scripts.UI;
using BepInEx.Configuration;
using HarmonyLib;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using StructureThermodynamicsOverhaul.scripts;
using UnityEngine;
using StructureThermodynamicsOverhaulMod.scripts;




namespace StructureThermodynamicsOverhaul.scripts
{
    [HarmonyPatch]
    public static class StructureThermodynamicsOverhaul
    {
        public static ConductionNetwork conductionNetwork = new ConductionNetwork();
        //private static bool completedStructureAnalysis = false;
        private static Stopwatch watch = Stopwatch.StartNew();

        private static bool updateStructuresFlag = false;
        private static int updateStructuresTickCounter = 0;
        private static int updateStructureThermalsAfterTicks = 6; //try and update every 3 seconds
        private static bool debugAnalysis = false;

        public static void debugLogToChat(string message)
        {
            debugLogToChat(message, false);
        }

        public static void debugLogToChat(string message, bool forced)
        {
            if (ConfigManager.Config_EnableDebugLogInChat || forced)
            {
                ConsoleWindow.Print("[STO]: " + message);
            }
        }

        private static void markForStructureAnalysis()
        {
            markForStructureAnalysis(null, true);
        }
        private static void markForStructureAnalysis(Structure structureUpdated)
        {
            markForStructureAnalysis(structureUpdated, false);
        }
        private static void markForStructureAnalysis(Structure structureUpdated, bool force)
        {
            if (force)
            {
                updateStructuresFlag = true;
            }

            if (structureUpdated != null && ThermalConstants.StructuralPrefabNames.Contains(structureUpdated.PrefabName))
            {
                updateStructuresFlag = true;
            }

        }

        public static void runStructureAnalysis()
        {
            if (updateStructuresFlag)
            {
                updateStructuresFlag = false;
                try
                {
                    conductionNetwork.ThermalResistancesUpdate();
                }
                catch (Exception e)
                {
                    debugLogToChat("<onEvalStructureEvent> " + e.Message);
                }
            }
        }

        [UsedImplicitly]
        [HarmonyPatch(typeof(GameManager), "StartGame")]
        [HarmonyPostfix]
        public static void onGameStart()
        {
            debugLogToChat("[Structure Thermodynamics Overhaul]: Loaded", true);
            Structure.OnAnyConstructed += constructedEvent;
            //Structure.On += constructedEvent;

            //structureAnalysis(null);
            markForStructureAnalysis();
            watch = Stopwatch.StartNew();
        }

        [UsedImplicitly]
        [HarmonyPatch(typeof(GameManager), "Update")]
        [HarmonyPostfix]
        public static void onGameUpdateTick()
        {
            if (ConfigManager.Config_EnableDebugHomeKey)
            {
                //debugLogToChat("Test???");

                if (debugAnalysis)
                {
                    if (conductionNetwork.isReadyForUpdateTick())
                    {
                        debugAnalysis = false;
                        try
                        {
                            Stopwatch debugWatch = Stopwatch.StartNew();
                            string debugS = conductionNetwork.ThermalUpdateSimulation(0.5f, false);
                            debugWatch.Stop();
                            debugLogToChat(debugS, true);
                            debugLogToChat("Compute time: Structural calculations (" + conductionNetwork.lastCalculationTimeMs.ToString() + "ms); " +
                                "Thermal tick (" + debugWatch.ElapsedMilliseconds.ToString() + "ms)", true);
                        }
                        catch (Exception e)
                        {

                        }
                    }

                }

                else if (new KeyboardShortcut(KeyCode.F8).IsDown())
                {
                    debugAnalysis = true;
                    markForStructureAnalysis();
                }
            }
        }

        [UsedImplicitly]
        [HarmonyPatch(typeof(Structure))]
        [HarmonyPatch("OnBuildStateUpdated")]
        [HarmonyPostfix]
        public static void onConstruct(Structure __instance)
        {
            //debugLogToChat("[STO]: Build state updated " + __instance.PrefabName);
            markForStructureAnalysis(__instance);
        }
        public static void constructedEvent(Structure structure)
        {
            //debugLogToChat("[STO]: Constructed " + structure.PrefabName);
            markForStructureAnalysis(structure);
        }

        [UsedImplicitly]
        [HarmonyPatch(typeof(AtmosphericsManager), "AtmosphericsNetworksTick")]
        [HarmonyPostfix]
        public static void onEvaluateAtmosphereEvents()
        {
            if (updateStructuresFlag)
            {
                runStructureAnalysis();
            }

            else if (conductionNetwork.isReadyForUpdateTick())
            {
                try
                {
                    float elapsedMilliseconds = (float)watch.ElapsedMilliseconds;
                    if (elapsedMilliseconds > 1000f)
                    {
                        elapsedMilliseconds = 1000f;
                    }
                    
                    Stopwatch debugWatch = Stopwatch.StartNew();
                    string debugS = conductionNetwork.ThermalUpdateSimulation(elapsedMilliseconds / 1000f);
                    debugWatch.Stop();
                    debugLogToChat("Thermal tick (" + debugWatch.ElapsedMilliseconds.ToString() + "ms) " + debugS);
                }
                catch (Exception e)
                {
                    debugLogToChat("<onEvalAtmoEvent> " + e.Message);
                    markForStructureAnalysis();
                }
            }

            else
            {
                debugLogToChat("Calculating thermals...");
            }

            watch.Restart();

            //room numbers get shifted every so often..
            if (updateStructuresTickCounter > updateStructureThermalsAfterTicks)
            {
                markForStructureAnalysis();
                updateStructuresTickCounter = 0;
            }
            updateStructuresTickCounter++;
        }

        private static string ListToString<T>(string name, List<T> list)
        {
            string output = name + "-";
            if (list.Count > 0)
            {
                output += list.ToString() + "\n";

                foreach (T item in list)
                {
                    output += " * " + item.ToString() + "\n";
                }
            }

            return output;
        }

        private static void LogToFile(string text)
        {
            if (ConfigManager.Config_EnableDebugLogInChat)
            {
                string fn = "TestLogTest.txt";
                try
                {
                    if (File.Exists(fn))
                    {
                        File.Delete(fn);
                    }

                    using (FileStream fs = File.Create(fn))
                    {
                        // Add some text to file
                        Byte[] bs = new UTF8Encoding(true).GetBytes(text);
                        fs.Write(bs, 0, bs.Length);
                    }
                }
                catch (Exception Ex)
                {
                    Console.WriteLine(Ex.ToString());
                }
            }
        }
    }
}
