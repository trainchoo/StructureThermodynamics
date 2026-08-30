using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static StructureThermodynamicsOverhaul.scripts.ThermalConstants;

namespace StructureThermodynamicsOverhaul.scripts
{
    public static class ThermalConstants
    {
        public static readonly IEnumerable<string> StructuralPrefabNames = new HashSet<string> {
            //Frames
            "StructureFrameIron", 
            "StructureFrame",
            //"StructureFrameSide",
            //"StructureFrameCornerCut",

            //Composite walls
            "StructureCompositeWall",
            "StructureCompositeWall02",
            "StructureCompositeWall03",
            "StructureCompositeWall04",
            "StructureCompositeWindow",

            //Composite window shutters
            "StructureCompositeWindowShutter",
            "StructureCompositeWindowShutterConnector",
            "StructureCompositeWindowShutterController",

            //Arch walls
            "StructureWallArch",
            "StructureWallArchArrow",
            "StructureWallArchPlating",
            "StructureWallArchTwoTone",

            //Flat walls
            "StructureWallFlat",
            "StructureWallLargePanel",
            "StructureWallLargePanelArrow",
            "StructureWallPlating",
            "StructureWallSmallPanelsAndHatch",
            "StructureWallSmallPanelsArrow",
            "StructureWallSmallPanelsMonoChrome",
            "StructureWallSmallPanelsTwoTone",
            "StructureWallSmallPanelsOpen",

            //Geometry walls
            "StructureWallGeometryStreight",
            "StructureWallGeometryCorner",
            "StructureWallGeometryTMirrored",
            "StructureWallGeometryT", 

            //Iron walls
            "StructureWallIron",
            "StructureWallIron02",
            "StructureWallIron03",
            "StructureWallIron04",
            "StructureCompositeWindowIron",

            //Padded walls
            "StructureWallPadding",
            "StructureWallPaddedArch",
            "StructureWallPaddingLightFitting",
            "StructureWallPaddedArchLightsFittings",
            "StructureWallPaddedArchLightFittingTop",
            "StructureWallPaddingArchVent",
            "StructureWallPaddedNoBorder",
            "StructureWallPaddingThin",
            "StructureWallPaddedThinNoBorder",
            "StructureWallPaddedWindow",
            "StructureWallPaddedWindowThin",

            //Reinforced walls
            "StructureReinforcedWall",
            "StructureReinforcedCompositeWindowSteel",
            "StructureReinforcedCompositeWindow",
            "StructureReinforcedWallPaddedWindow",
            "StructureReinforcedWallPaddedWindowThin",

            //Doors
            //Blast door
            "StructureBlastDoor",

            //Exterior doors
            "StructureGlassDoor",
            "StructureCompositeDoor",
            //"CompositeRollCover",
            "StructureManualHatch",

            //Interior doors
            "StructureInteriorDoorGlass",
            "StructureInteriorDoorPadded",
            "StructureInteriorDoorPaddedThin",
            "StructureInteriorDoorTriangle",

            //Robot arm doors
            "StructureRobotArmDoor"
        };
        public static readonly IEnumerable<string> StructuralFramePrefabNames = new HashSet<string> {
            "StructureFrameIron", 
            "StructureFrame"
            //"StructureFrameSide",
            //"StructureFrameCornerCut"
        };

        public enum Solid
        {
            Default,
            Iron,
            Steel,
            Composite,
            Insulation,
            Glass
        };
        private static readonly Dictionary<string, Solid> prefabToMatDict = new Dictionary<string, Solid> {

            //Frames
            {"StructureFrameIron", Solid.Iron},
            {"StructureFrame", Solid.Steel},
            //{"StructureFrameSide", Solid.Default},
            //{"StructureFrameCornerCut", Solid.Default},

            //Composite walls
            {"StructureCompositeWall", Solid.Composite},
            {"StructureCompositeWall02", Solid.Composite},
            {"StructureCompositeWall03", Solid.Composite},
            {"StructureCompositeWall04", Solid.Composite},
            {"StructureCompositeWindow", Solid.Glass},

            //Composite window shutters
            {"StructureCompositeWindowShutter", Solid.Glass},
            {"StructureCompositeWindowShutterConnector", Solid.Composite},
            {"StructureCompositeWindowShutterController", Solid.Composite},

            //Arch walls
            {"StructureWallArch", Solid.Steel},
            {"StructureWallArchArrow", Solid.Steel},
            {"StructureWallArchPlating", Solid.Steel},
            {"StructureWallArchTwoTone", Solid.Steel},

            //Flat walls
            {"StructureWallFlat", Solid.Steel},
            {"StructureWallLargePanel", Solid.Steel},
            {"StructureWallLargePanelArrow", Solid.Steel},
            {"StructureWallPlating", Solid.Steel},
            {"StructureWallSmallPanelsAndHatch", Solid.Steel},
            {"StructureWallSmallPanelsArrow", Solid.Steel},
            {"StructureWallSmallPanelsMonoChrome", Solid.Steel},
            {"StructureWallSmallPanelsTwoTone", Solid.Steel},
            {"StructureWallSmallPanelsOpen", Solid.Steel},

            //Geometry walls
            {"StructureWallGeometryStreight", Solid.Steel},
            {"StructureWallGeometryCorner", Solid.Steel},
            {"StructureWallGeometryTMirrored", Solid.Steel},
            {"StructureWallGeometryT", Solid.Steel}, 

            //Iron walls
            {"StructureWallIron", Solid.Iron},
            {"StructureWallIron02", Solid.Iron},
            {"StructureWallIron03", Solid.Iron},
            {"StructureWallIron04", Solid.Iron},
            {"StructureCompositeWindowIron", Solid.Glass},

            //Padded walls
            {"StructureWallPadding", Solid.Insulation},
            {"StructureWallPaddedArch", Solid.Insulation},
            {"StructureWallPaddingLightFitting", Solid.Insulation},
            {"StructureWallPaddedArchLightsFittings", Solid.Insulation},
            {"StructureWallPaddedArchLightFittingTop", Solid.Insulation},
            {"StructureWallPaddingArchVent", Solid.Insulation},
            {"StructureWallPaddedNoBorder", Solid.Insulation},
            {"StructureWallPaddingThin", Solid.Insulation},
            {"StructureWallPaddedThinNoBorder", Solid.Insulation},
            {"StructureWallPaddedWindow", Solid.Glass},
            {"StructureWallPaddedWindowThin", Solid.Glass}, 

            //Reinforced walls
            {"StructureReinforcedWall", Solid.Steel},
            {"StructureReinforcedCompositeWindowSteel", Solid.Glass},
            {"StructureReinforcedCompositeWindow", Solid.Glass},
            {"StructureReinforcedWallPaddedWindow", Solid.Glass},
            {"StructureReinforcedWallPaddedWindowThin", Solid.Glass},
            
            //Doors
            //Blast door
            {"StructureBlastDoor", Solid.Steel},

            //Exterior doors
            {"StructureGlassDoor", Solid.Glass},
            {"StructureCompositeDoor", Solid.Composite},
            //"CompositeRollCover", Solid.Default},
            {"StructureManualHatch", Solid.Iron},

            //Interior doors
            {"StructureInteriorDoorGlass", Solid.Glass},
            {"StructureInteriorDoorPadded", Solid.Insulation},
            {"StructureInteriorDoorPaddedThin", Solid.Insulation},
            {"StructureInteriorDoorTriangle", Solid.Composite},

            //Robot arm doors
            {"StructureRobotArmDoor", Solid.Steel},
        };

        private static readonly Dictionary<Solid, float> materialThermalConductivity = new Dictionary<Solid, float>  {
            {Solid.Default, 20f},
            {Solid.Iron, 55f},
            {Solid.Steel, 20f},
            {Solid.Composite, 5f},
            {Solid.Insulation, 0.5f},
            {Solid.Glass, 2f}
        };

        private static readonly Dictionary<Solid, float> materialThermalEmissivity = new Dictionary<Solid, float>  {
            {Solid.Default, 0.5f},
            {Solid.Iron, 0.2f},
            {Solid.Steel, 0.1f},
            {Solid.Composite, 0.9f},
            {Solid.Insulation, 0.8f},
            {Solid.Glass, 0.8f}
        };

        public static float getBuildStateThermalMultiplier(int currentBuildState, int totalBuildStates)
        {
            //the lower the build state, the lower the resistance (to account for empty area thermal transfer by the atmosphere)
            if (totalBuildStates == 2)
            {
                return (currentBuildState == 2) ? 1.0f : 0.3f;
            }
            else if (totalBuildStates == 3)
            {
                return (currentBuildState == 3) ? 1.0f : (currentBuildState == 2) ? 0.5f : 0.3f;
            }
            else if (totalBuildStates == 4)
            {
                return (currentBuildState == 4) ? 1.0f : (currentBuildState == 3) ? 0.7f : (currentBuildState == 2) ? 0.5f : 0.3f;
            }
            else
            {
                return ((float)currentBuildState / (float)totalBuildStates);
            }
        }

        public static Solid getMaterialFromPrefabName(string prefabName)
        {
            if (prefabToMatDict.ContainsKey(prefabName))
            {
                return prefabToMatDict[prefabName];
            }
            return Solid.Default;
        }

        public static float getThermalConductivity(Solid material)
        {
            return materialThermalConductivity[material];
        }
        public static float getThermalConductivity(string prefabName)
        {
            return getThermalConductivity(getMaterialFromPrefabName(prefabName));
        }

        public static float getThermalEmissivity(Solid material)
        {
            return materialThermalEmissivity[material];
        }
        public static float getThermalEmissivity(string prefabName)
        {
            return getThermalEmissivity(getMaterialFromPrefabName(prefabName));
        }

        public enum Gas
        {
            Default,
            Oxygen
        }

        private static readonly Dictionary<Gas, float> gasThermalConductionCoeff = new Dictionary<Gas, float>  {
            {Gas.Default, 5f},
            {Gas.Oxygen, 5f}
        };

        public static float getGasConductionCoefficent(Gas gas)
        {
            return gasThermalConductionCoeff[gas];
        }

        //difficulty
        //ThermalMultiplierOffset = realistic
        //0.0f = no thermals
        public static readonly float ThermalMultiplierOffset = 10.0f;

        //Calculations thresholds
        public static readonly float MinDeltaT = 1.0f;
        public static readonly float MinPressure = 0.1f;
        public static readonly float EnergyPerAtmoBlock = 200.0f;

        //in meters
        public static readonly float BlockLength = 2.0f;
        public static readonly float BlockDiagonalLength = 1.7f;
        public static readonly float WallLength = 0.2f;
        public static readonly float WallDiagonalLength = 3.0f; //no shortcuts

        //in meters^2
        public static readonly float BlockArea = 4.0f;
        public static readonly float BlockDiagonalArea = 0.4f;
        public static readonly float WallArea = 4.0f;
        public static readonly float WallDiagonalArea = 0.6f;

        public static readonly float StefanBoltzmannConst = 0.0000000567f;
        public static readonly float StefanBoltzmannConstInv = 17636684.30f;
    }
}
