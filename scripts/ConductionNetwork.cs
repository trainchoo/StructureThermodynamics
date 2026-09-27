using Assets.Scripts.GridSystem;
using Assets.Scripts.Objects;
using Assets.Scripts;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Permissions;
using static StructureThermodynamicsOverhaul.scripts.ConductionNetwork;
using UI.Tooltips;
using Assets.Scripts.Util;
using Assets.Scripts.Atmospherics;
using Networks;
using static Assets.Scripts.Objects.Items.GameConstants;
using System.Diagnostics;
using Reagents;
using System.Drawing.Text;
using StructureThermodynamicsOverhaulMod.scripts;
using System.Net.Sockets;
using System.Threading;

namespace StructureThermodynamicsOverhaul.scripts
{
    public class ConductionNetwork
    {
        private static Node dummyNode = new Node(Node.Type.Dummy);
        private static readonly Grid3[] adjGridVectors = new Grid3[] {
                    new Grid3(20.0f, 0.0f, 0.0f),
                    new Grid3(-20.0f, 0.0f, 0.0f),
                    new Grid3(0.0f, 20.0f, 0.0f),
                    new Grid3(0.0f, -20.0f, 0.0f),
                    new Grid3(0.0f, 0.0f, 20.0f),
                    new Grid3(0.0f, 0.0f, -20.0f) };
        
        private List<Node> wordNodes;
        private Dictionary<long, List<Node>> roomNodes;

        private bool thermalsCalculated;
        public int lastCalculationTimeMs;
        private Stopwatch debugTimer;
        private List<AverageRoomConduit> averageRoomConduits;
        private Dictionary<long, Room> allRoomsDictionary;
        private Dictionary<long, List<Atmosphere>> roomAtmosphereDictionary;
        private Dictionary<long, List<ThermalResistances>> thermalChannels;

        #region internal Class defintions
        public struct AverageRoomConduit
        {
            //index 0 = outward flux resistance, 1 = inward flux resistance
            public float[] Rin;
            public float[] Rbulk;
            public float[] Rout;
            public float ratioCvRin;
            public float ratioCvRout;

            public long currentRoomID;
            public bool isToAnotherRoom;
            public long destinationRoomID;

            public AverageRoomConduit(float Rin_inward, float Rin_outward, float Rbulk_inward, float Rbulk_outward, float Rout_inward, float Rout_outward, float ratioCvRin, float ratioCvRout, long currentRoomID, bool isToAnotherRoom, long destinationRoomID)
            {
                this.Rin = new float[] { Rin_inward, Rin_outward };
                this.Rbulk = new float[] { Rbulk_inward, Rbulk_outward };
                this.Rout = new float[] { Rout_inward, Rout_outward };
                this.ratioCvRin = ratioCvRin;
                this.ratioCvRout = ratioCvRout;

                this.currentRoomID = currentRoomID;
                this.isToAnotherRoom = isToAnotherRoom;
                this.destinationRoomID = destinationRoomID;
            }

            public AverageRoomConduit(float[] Rin, float[] Rbulk, float[] Rout, float ratioCvRin, float ratioCvRout, long currentRoomID, bool isToAnotherRoom, long destinationRoomID)
            {
                this.Rin = Rin;
                this.Rbulk = Rbulk;
                this.Rout = Rout;
                this.ratioCvRin = ratioCvRin;
                this.ratioCvRout = ratioCvRout;

                this.currentRoomID = currentRoomID;
                this.isToAnotherRoom = isToAnotherRoom;
                this.destinationRoomID = destinationRoomID;
            }

            public override string ToString()
            {
                string output = "R" + currentRoomID.ToString() + "->" + (isToAnotherRoom ? "R" + destinationRoomID.ToString() : "A");

                output += Rin[0] >= 0 ? "; I->:" + Rin[0].ToString("0.00000") : "";
                output += Rin[1] >= 0 ? "; I<-:" + Rin[1].ToString("0.00000") : "";
                output += Rbulk[0] >= 0 ? "; B->:" + Rbulk[0].ToString("0.00000") : "";
                output += Rbulk[1] >= 0 ? "; B<-:" + Rbulk[1].ToString("0.00000") : "";
                output += Rout[0] >= 0 ? "; E->:" + Rout[0].ToString("0.00000") : "";
                output += Rout[1] >= 0 ? "; E<-:" + Rout[1].ToString("0.00000") : "";
                output += ratioCvRin >= 0 ? "; CvRi:" + ratioCvRin.ToString("0.00") : "";
                output += ratioCvRout >= 0 ? "; CvRe:" + ratioCvRout.ToString("0.00") : "";

                return output;
            }
        }
        public class ThermalResistances
        { 
            public float internalConduction;
            public float internalRadiation;
            public float bulkConduction;
            public float externalConduction;
            public float externalRadiation;
            public bool isExternalRoom;
            public long externalRoomID;

            public float sharedInternalAreaFraction;

            public ThermalResistances(float internalConduction, float internalRadiation, float bulkConduction, float externalConduction, float externalRadiation, bool isExternalRoom, long externalRoomID)
            {
                this.internalConduction = internalConduction;
                this.internalRadiation = internalRadiation;

                this.bulkConduction = bulkConduction;
                this.externalConduction = externalConduction;
                this.externalRadiation = externalRadiation;
                this.isExternalRoom = isExternalRoom;
                this.externalRoomID = externalRoomID;

                sharedInternalAreaFraction = 1.0f;
            }

            public override string ToString()
            {
                return "IC:" + internalConduction.ToString("0.000") + "; IR:" + internalRadiation.ToString("0.000") + 
                    "; EC:" + externalConduction.ToString("0.000") + "; ER:" + externalRadiation.ToString("0.000") + 
                    "; Bulk:" + bulkConduction.ToString("0.000") + " => " + (isExternalRoom ? "R" + externalRoomID.ToString() : "A");
            }
        }
        public class Link
        {
            public float resistance;
            public Node[] nodes;
            public Type type;
            //public long linkID;

            public enum Type
            {
                BulkConduction,
                SurfaceConduction,
                Radiation,
                Dummy
            }
            public Link(Type type, float resistance, Node node1, Node node2)
            {
                this.type = type;
                this.resistance = resistance;
                this.nodes = new Node[2];
                this.nodes[0] = node1;
                this.nodes[1] = node2;
                //this.isUsed = false;
            }

            public Link(Type type, float resistance, Node[] nodes)
            {
                this.type = type;
                this.resistance = resistance;
                this.nodes = new Node[2];
                this.nodes = nodes;
                //this.isUsed = false;
            }

            public Node getNextNode(Node previousNode)
            {
                if (nodes[0].Equals(previousNode))
                {
                    return nodes[1];
                }
                else
                {
                    return nodes[0];
                }
            }

            public override string ToString()
            {
                string s1 = (nodes[0].type == Node.Type.ExternalSurface) ? "ES" : (nodes[0].type == Node.Type.InternalSurface) ? "IS" : (nodes[0].type == Node.Type.Room) ? "R" : (nodes[0].type == Node.Type.Atmosphere) ? "A" : "D";
                string s2 = (nodes[1].type == Node.Type.ExternalSurface) ? "ES" : (nodes[1].type == Node.Type.InternalSurface) ? "IS" : (nodes[1].type == Node.Type.Room) ? "R" : (nodes[1].type == Node.Type.Atmosphere) ? "A" : "D";
                return s1 + "-" + resistance.ToString("0.0") + "-" + s2;
            }
        }
        public class Node
        {
            public List<Link> links;
            public long roomID;
            public Type type;

            public enum Type
            {
                ExternalSurface,
                InternalSurface,
                Room,
                Atmosphere,
                Dummy,
            }

            public Node(Type type)
            {
                this.type = type;
                this.roomID = -1;
                this.links = new List<Link>();
            }
            public void addLink(Link link)
            {
                links.Add(link);
            }
            public override string ToString()
            {
                string output = "Type: " + ((type == Type.ExternalSurface) ? "Ex Surface" : (type == Type.InternalSurface) ? "In Surface" : (type == Type.Room) ? "Room" : (type == Type.Atmosphere) ? "Atmosphere" : "Dummy") + "\n";
                output += type == Type.Room ? "Room ID: " + roomID + "\n" : "";

                List<Link> inLinks = new List<Link>();
                List<Link> exLinks = new List<Link>();

                foreach (Link link in links)
                {
                    if (link.type == Link.Type.BulkConduction)
                    {
                        inLinks.Add(link);
                    }
                    else
                    {
                        exLinks.Add(link);
                    }
                }

                if (inLinks.Count > 0)
                {
                    output += "Internal Links: " + inLinks.Count.ToString() + " => ";
                    foreach (Link link in inLinks)
                    {
                        output += link.ToString() + "; ";
                    }
                    output += "\n";
                }
                if (exLinks.Count > 0)
                {
                    output += "External Links => ";
                    foreach (Link link in exLinks)
                    {
                        if (link.type == Link.Type.SurfaceConduction)
                        {
                            output += "Cond: " + link.ToString() + "; ";
                        }
                        else if (link.type == Link.Type.Radiation)
                        {
                            output += "Rad: " + link.ToString() + "; ";
                        }
                        else
                        {
                            output += "Other: " + link.ToString() + "; ";
                        }
                    }
                    output += "\n";
                }

                return output;
            }
        }
        private struct debugRoomData
        {
            public string resistances;

            public debugRoomData(string resistances)
            {
                this.resistances = resistances;
            }
        }
        #endregion

        public ConductionNetwork()
        {
            thermalChannels = new Dictionary<long, List<ThermalResistances>>();
            averageRoomConduits = new List<AverageRoomConduit>();
            //wordNodes = new List<Node>();
            roomNodes = new Dictionary<long, List<Node>>();
            thermalsCalculated = false;
            lastCalculationTimeMs = -1;
            debugTimer = new Stopwatch();

            allRoomsDictionary = new Dictionary<long, Room>();
            roomAtmosphereDictionary = new Dictionary<long, List<Atmosphere>>();

        }
        public Node createNode(Node.Type type)
        {
            Node newNode = new Node(type);
            wordNodes.Add(newNode);
            return newNode;
        }
        public Node createNode(long roomID)
        {
            //StructureThermodynamicsOverhaul.debugLogToChat("Adding RID: " + roomID.ToString());
            Node newNode = createNode(Node.Type.Room);
            newNode.roomID = roomID;
            if (roomNodes.ContainsKey(roomID))
            {
                roomNodes[roomID].Add(newNode);
            }
            else
            {
                List<Node> newRoomNodeList = new List<Node> { newNode };
                roomNodes.Add(roomID, newRoomNodeList);
            }          
            return newNode;
        }

        #region helper functions (lookups and resistance math)
        private int oppositeSideIndex(int index)
        {
            if (index == 0 || index == 2 || index == 4)
            {
                return index + 1;
            }
            else
            {
                return index - 1; 
            }
        }
        private bool isStructure(Structure structure)
        {
            return ThermalConstants.StructuralPrefabNames.Contains<string>(structure.PrefabName);
        }
        private bool isFrame(Structure structure)
        {
            return ThermalConstants.StructuralFramePrefabNames.Contains<string>(structure.PrefabName);
        }
        private float getEffectiveResistance(Link.Type linkType, float area, float length, Structure structure)
        {
            float buildStateMultiplier = ThermalConstants.getBuildStateThermalMultiplier(structure.CurrentBuildStateIndex + 1, structure.BuildStates.Count);
            ThermalConstants.Solid material = ThermalConstants.getMaterialFromPrefabName(structure.PrefabName);

            if (structure.PrefabName == "StructureFrame")
            {
                // simulate plastic cladding on steel frames at last build stage, as turning the material into composite
                if (structure.CurrentBuildStateIndex + 1 == structure.BuildStates.Count)
                {
                    material = ThermalConstants.Solid.Composite;
                }
                else if (structure.CurrentBuildStateIndex + 1 == structure.BuildStates.Count - 1)
                {
                    // treat airtight steel frame (stage 3 of 4) as finished
                    buildStateMultiplier = 1;
                }
            }

            if (linkType == Link.Type.BulkConduction)
            {
                return (buildStateMultiplier * length) / (ThermalConstants.getThermalConductivity(material) * area);
            }
            else if (linkType == Link.Type.SurfaceConduction)
            {
                return buildStateMultiplier / area;
            }
            else if (linkType == Link.Type.Radiation)
            {
                return buildStateMultiplier / (ThermalConstants.getThermalEmissivity(material) * area);
            }
            return 6969f;
        }
        private float parallelResistance(float resistance1, float resistance2) 
        {
            return 1 / ((1.0f / resistance1) + (1.0f / resistance2));
        }
        private float parallelResistance(float[] resistances)
        {
            float output = 0;

            for (int i = 0; i < resistances.Length; i++)
            {
                output += 1.0f / resistances[i];
            }

            return 1.0f / output;
        }
        #endregion

        #region onStructureUpdate functions
        
        public void ThermalResistancesUpdate()
        {
            thermalsCalculated = false;

            if (allRoomsDictionary == null) { allRoomsDictionary = new Dictionary<long, Room>(); }
            else { allRoomsDictionary.Clear(); }
            if (roomAtmosphereDictionary == null) { roomAtmosphereDictionary = new Dictionary<long, List<Atmosphere>>(); }
            else { roomAtmosphereDictionary.Clear(); }
            if (wordNodes == null) { wordNodes = new List<Node>(); }
            else { wordNodes.Clear(); }
            if (roomNodes == null) { roomNodes = new Dictionary<long, List<Node>>(); }
            else { roomNodes.Clear(); }
            averageRoomConduits.Clear();
            thermalChannels.Clear();

            //new thread
            Thread nodeUpdateThread = new Thread(new ThreadStart(UpdateNodes));     
            nodeUpdateThread.Start();
        }
        private void UpdateNodes()
        {
            debugTimer.Reset();
            debugTimer.Start();

            List<Structure> allStructures = GridController.AllStructuresPool.ToList();
            List<Structure> GridWalls = new List<Structure>();
            foreach (Structure structure in allStructures)
            {
                if (structure == null)
                {
                    continue;
                }
                if (isStructure(structure))
                {
                    GridWalls.Add(structure);
                }
            }

            ILookup<Grid3, Structure> structureLookup = GridWalls.ToLookup(el => el.LocalGrid);
            HashSet<Grid3> structurePositions = structureLookup.Select(el => el.Key).ToHashSet();
            Dictionary<Grid3, Node[]> computedStructures = new Dictionary<Grid3, Node[]>();

            List<Room> rooms = RoomController.World.Rooms;
            Dictionary<Grid3, Room> dictionaryGridRoom = new Dictionary<Grid3, Room>();

            if (rooms.Count > 0)
            {
                foreach (Room room in rooms)
                {
                    List<WorldGrid> grids = room.Grids;
                    List<Atmosphere> atmosList = new List<Atmosphere>();
                    allRoomsDictionary.Add(room.RoomId, room);
                    if (grids.Count > 0)
                    {
                        foreach (WorldGrid grid in grids)
                        {
                            dictionaryGridRoom.Add(grid.Value, room);
                            atmosList.Add(GridController.World.AtmosphericsController.GetAtmosphereLocal(grid));
                        }
                    }
                    roomAtmosphereDictionary.Add(room.RoomId, atmosList);
                }
            }

            foreach (Grid3 position in structurePositions)
            {
                List<Structure> currentStructureList = structureLookup[position].ToList();
                Structure currentStructure = currentStructureList.First();
                bool isStructureFrame = isFrame(currentStructure);
                Node[] surfaceNodes = new Node[6];
                string[] wallTypes = new string[6];
                bool[] wallFlags = new bool[6] { false, false, false, false, false, false };
                bool isWallInRoom = dictionaryGridRoom.ContainsKey(position);

                //calculate resistances
                float rBulkAcross = isStructureFrame ? getEffectiveResistance(Link.Type.BulkConduction, ThermalConstants.BlockArea, ThermalConstants.BlockLength, currentStructure) :
                    getEffectiveResistance(Link.Type.BulkConduction, ThermalConstants.WallArea, ThermalConstants.WallLength, currentStructure);
                float rBulkSide = isStructureFrame ? getEffectiveResistance(Link.Type.BulkConduction, ThermalConstants.BlockDiagonalArea, ThermalConstants.BlockDiagonalLength, currentStructure) :
                    getEffectiveResistance(Link.Type.BulkConduction, ThermalConstants.WallDiagonalArea, ThermalConstants.WallDiagonalLength, currentStructure);
                float rExConduction = getEffectiveResistance(Link.Type.SurfaceConduction, ThermalConstants.BlockArea, 0, currentStructure);
                float rExRadiation = getEffectiveResistance(Link.Type.Radiation, ThermalConstants.BlockArea, 0, currentStructure); ;


                if (!isStructureFrame)
                {
                    foreach (Structure wall in currentStructureList)
                    {
                        int xD = (int)(wall.Position.x * 10f) - position.x;
                        int yD = (int)(wall.Position.y * 10f) - position.y;
                        int zD = (int)(wall.Position.z * 10f) - position.z;

                        int dirIdx = xD > 0 ? 0 : xD < 0 ? 1 : yD > 0 ? 2 : yD < 0 ? 3 : zD > 0 ? 4 : 5;

                        wallTypes[dirIdx] = wall.PrefabName;
                        wallFlags[dirIdx] = true;
                    }
                }
                //create extenral links and nodes
                for (int i = 0; i < adjGridVectors.Length; i++)
                {
                    Grid3 neighbor = currentStructure.LocalGrid + adjGridVectors[i];
                    int oi = oppositeSideIndex(i);
                    if (structurePositions.Contains(neighbor))
                    {
                        //internal structure

                        //frame frame - ok
                        //frame wall - leaves neightbor connection to external; needs to be updated when wall is calucalted
                        //wall frame - need to update frame external node - possibly add link to atmo or room, or change to internal node
                        //wall wall -??
                        bool isNeighborFrame = isFrame(structureLookup[neighbor].ToList().First());

                        if (computedStructures.ContainsKey(neighbor)) //neighbor already computed
                        {
                            Node[] neighborNodes = computedStructures[neighbor];

                            if (isStructureFrame) //me frame 
                            {
                                surfaceNodes[i] = neighborNodes[oi];
                            }
                            else //me wall
                            {
                                if (isNeighborFrame && wallFlags[i]) //if neighbor frame, and wall flag; change their node to internal
                                {
                                    neighborNodes[oi].type = Node.Type.InternalSurface;
                                    surfaceNodes[i] = neighborNodes[oi];
                                }
                                else if (isNeighborFrame) //if neighbor frame, but no wall; add ex node and links to neighbors surface node
                                {
                                    Node exNode = isWallInRoom ? createNode(dictionaryGridRoom[position].RoomId) : createNode(Node.Type.Atmosphere);
                                    Link exCondLink = new Link(Link.Type.SurfaceConduction, rExConduction, exNode, neighborNodes[oi]);
                                    Link exRadLink = new Link(Link.Type.Radiation, rExRadiation, exNode, neighborNodes[oi]);
                                    neighborNodes[oi].addLink(exCondLink);
                                    neighborNodes[oi].addLink(exRadLink);
                                    exNode.addLink(exCondLink);
                                    exNode.addLink(exRadLink);
                                    surfaceNodes[i] = neighborNodes[oi];
                                }
                                else if (wallFlags[i] && (neighborNodes[oi].type == Node.Type.Dummy)) //if neighbor is wall and has no node, but i have wall flag; create ex node
                                {
                                    Node exNode = dictionaryGridRoom.ContainsKey(neighbor) ? createNode(dictionaryGridRoom[neighbor].RoomId) : createNode(Node.Type.Atmosphere);
                                    surfaceNodes[i] = createNode(Node.Type.ExternalSurface);
                                    Link exCondLink = new Link(Link.Type.SurfaceConduction, rExConduction, exNode, surfaceNodes[i]);
                                    Link exRadLink = new Link(Link.Type.Radiation, rExRadiation, exNode, surfaceNodes[i]);
                                    surfaceNodes[i].addLink(exCondLink);
                                    surfaceNodes[i].addLink(exRadLink);
                                    exNode.addLink(exCondLink);
                                    exNode.addLink(exRadLink);
                                    neighborNodes[oi] = surfaceNodes[i];
                                }
                                else if (neighborNodes[oi].type == Node.Type.ExternalSurface) //wall neighbor has wall facing me
                                {
                                    if (wallFlags[i]) //i also has wall
                                    {
                                        neighborNodes[oi].type = Node.Type.InternalSurface;
                                        surfaceNodes[i] = neighborNodes[oi];
                                    }
                                    else
                                    {
                                        Node exNode = isWallInRoom ? createNode(dictionaryGridRoom[position].RoomId) : createNode(Node.Type.Atmosphere);
                                        Link exCondLink = new Link(Link.Type.SurfaceConduction, rExConduction, exNode, neighborNodes[oi]);
                                        Link exRadLink = new Link(Link.Type.Radiation, rExRadiation, exNode, neighborNodes[oi]);
                                        neighborNodes[oi].addLink(exCondLink);
                                        neighborNodes[oi].addLink(exRadLink);
                                        exNode.addLink(exCondLink);
                                        exNode.addLink(exRadLink);
                                        surfaceNodes[i] = neighborNodes[oi];
                                    }
                                }
                                else if ((neighborNodes[oi].type == Node.Type.Dummy) && !wallFlags[i]) //no wall in either case
                                {
                                    surfaceNodes[i] = dummyNode;
                                }
                            }

                        }

                        else //neighbor not yet computed
                        {
                            if (isStructureFrame) //me frame
                            {
                                if (isNeighborFrame)
                                {
                                    surfaceNodes[i] = createNode(Node.Type.InternalSurface);
                                }
                                else
                                {
                                    surfaceNodes[i] = createNode(Node.Type.ExternalSurface); //let the wall code handle extenral node creation
                                }
                            }
                            else //me wall
                            {
                                if (isNeighborFrame) //neighbor is frame
                                {
                                    if (wallFlags[i]) //and i have a facing wall
                                    {
                                        surfaceNodes[i] = createNode(Node.Type.InternalSurface);
                                    }
                                    else //no wall, so create external node for neighbor frame to use when its being computed
                                    {
                                        surfaceNodes[i] = createNode(Node.Type.ExternalSurface);
                                        Node exNode = isWallInRoom ? createNode(dictionaryGridRoom[position].RoomId) : createNode(Node.Type.Atmosphere);
                                        Link exCondLink = new Link(Link.Type.SurfaceConduction, rExConduction, exNode, surfaceNodes[i]);
                                        Link exRadLink = new Link(Link.Type.Radiation, rExRadiation, exNode, surfaceNodes[i]);
                                        surfaceNodes[i].addLink(exCondLink);
                                        surfaceNodes[i].addLink(exRadLink);
                                        exNode.addLink(exCondLink);
                                        exNode.addLink(exRadLink);
                                    }
                                }
                                else //neighbour is wall
                                {
                                    if (wallFlags[i]) //me has facing wall
                                    {
                                        surfaceNodes[i] = createNode(Node.Type.ExternalSurface); //let the wall to be created code handle extenral node creation
                                    }
                                    else //no facing wall
                                    {
                                        surfaceNodes[i] = dummyNode; //let the other wall create its own nodes
                                    }
                                }
                            }
                        }
                    }
                    else if (dictionaryGridRoom.ContainsKey(neighbor))
                    {
                        //inside a room
                        if (isStructureFrame || wallFlags[i]) //me either frame or has wall facing
                        {
                            Node roomNode = createNode(dictionaryGridRoom[neighbor].RoomId);
                            Node surfaceNode = createNode(Node.Type.ExternalSurface);
                            Link exCondlink = new Link(Link.Type.SurfaceConduction, rExConduction, roomNode, surfaceNode);
                            Link exRadlink = new Link(Link.Type.Radiation, rExRadiation, roomNode, surfaceNode);
                            roomNode.addLink(exCondlink);
                            roomNode.addLink(exRadlink);
                            surfaceNode.addLink(exCondlink);
                            surfaceNode.addLink(exRadlink);
                            surfaceNodes[i] = surfaceNode;
                        }
                        else
                        {
                            surfaceNodes[i] = dummyNode;
                        }
                    }
                    else
                    {
                        //atmosphere
                        if (isStructureFrame || wallFlags[i]) //me either frame or has wall facing
                        {
                            Node atmoNode = createNode(Node.Type.Atmosphere);
                            Node surfaceNode = createNode(Node.Type.ExternalSurface);
                            Link exCondlink = new Link(Link.Type.SurfaceConduction, rExConduction, atmoNode, surfaceNode);
                            Link exRadlink = new Link(Link.Type.Radiation, rExRadiation, atmoNode, surfaceNode);
                            atmoNode.addLink(exCondlink);
                            surfaceNode.addLink(exCondlink);
                            atmoNode.addLink(exRadlink);
                            surfaceNode.addLink(exRadlink);
                            surfaceNodes[i] = surfaceNode;
                        }
                        else
                        {
                            surfaceNodes[i] = dummyNode;
                        }
                    }

                }

                //create internal links
                for (int i = 0; i < 5; i++) //frame and wall adjacent connections
                {
                    for (int j = i + 1; j < 6; j++)
                    {
                        Link link;
                        if (j == i + 1 && (i == 0 || i == 2 || i == 4))
                        {
                            //opposite sides
                            if (isStructureFrame) //make frames hollow in the middle?
                            {
                                //link = new Link(Link.Type.BulkConduction, rBulkAcross, surfaceNodes[i], surfaceNodes[j]);
                                //surfaceNodes[i].addLink(link);
                                //surfaceNodes[j].addLink(link);
                            }
                        }
                        else
                        {
                            //adjacent sides
                            if (isStructureFrame)
                            {
                                link = new Link(Link.Type.BulkConduction, rBulkSide, surfaceNodes[i], surfaceNodes[j]);
                                surfaceNodes[i].addLink(link);
                                surfaceNodes[j].addLink(link);
                            }
                            else
                            {
                                if (surfaceNodes[i].type == Node.Type.Dummy || surfaceNodes[j].type == Node.Type.Dummy)
                                {
                                    continue;
                                }
                                if (wallFlags[i] && wallFlags[j]) //stronger wall conduction
                                {
                                    link = new Link(Link.Type.BulkConduction, rBulkAcross, surfaceNodes[i], surfaceNodes[j]);
                                    surfaceNodes[i].addLink(link);
                                    surfaceNodes[j].addLink(link);
                                }
                                else if (wallFlags[i] || wallFlags[j]) //weaker wall conduction
                                {
                                    link = new Link(Link.Type.BulkConduction, rBulkSide, surfaceNodes[i], surfaceNodes[j]);
                                    surfaceNodes[i].addLink(link);
                                    surfaceNodes[j].addLink(link);
                                }
                            }

                        }
                    }
                }
                for (int i = 0; i < surfaceNodes.Length; i++) //wall face connections to outside
                {
                    if (wallFlags[i])
                    {
                        Node internalConductionNode = createNode(Node.Type.ExternalSurface);
                        Link internalConductionLink = new Link(Link.Type.BulkConduction, rBulkAcross, internalConductionNode, surfaceNodes[i]);
                        internalConductionNode.addLink(internalConductionLink);
                        surfaceNodes[i].addLink(internalConductionLink);

                        Node exNode = isWallInRoom ? createNode(dictionaryGridRoom[position].RoomId) : createNode(Node.Type.Atmosphere);
                        Link exCondLink = new Link(Link.Type.SurfaceConduction, rExConduction, exNode, internalConductionNode);
                        Link exRadLink = new Link(Link.Type.Radiation, rExRadiation, exNode, internalConductionNode);
                        internalConductionNode.addLink(exCondLink);
                        exNode.addLink(exCondLink);
                        internalConductionNode.addLink(exRadLink);
                        exNode.addLink(exRadLink);
                    }
                }
                computedStructures.Add(position, surfaceNodes);
            }
            PostNodeUpdate();
        }
        private void PostNodeUpdate()
        {
            foreach (KeyValuePair<long, List<Node>> kvp in roomNodes)
            {
                long roomID = kvp.Key;
                List<ThermalResistances> thermalResistances = CalculateResistancePaths(roomID, kvp.Value);
                averageRoomConduits.AddRange(CalculateRoomAverageResistances(roomID, thermalResistances));
            }
            debugTimer.Stop();
            lastCalculationTimeMs = (int)debugTimer.ElapsedMilliseconds;
            thermalsCalculated = true;
        }
        public List<ThermalResistances> CalculateResistancePaths(long currentRoomID, List<Node> roomThermalNodes)
        {
            List<ThermalResistances> roomThermalResistances = new List<ThermalResistances>();

            Dictionary<Node, List<ThermalResistances>> findThermalResistancesByNode = new Dictionary<Node, List<ThermalResistances>>();
            Dictionary<Node, float> findNodeTotalPathResistanceSums = new Dictionary<Node, float>();
            //StructureThermodynamicsOverhaul.debugLogToChat("rnodes -" + kvp.Value.Count().ToString());
            Dictionary<Link, int> usedLinkTimes = new Dictionary<Link, int>();

            int maxConductionChannelsPerNode = 2;
            for (int calcCount = 0; calcCount < maxConductionChannelsPerNode; calcCount++)
            {
                foreach (Node currentRoomNode in roomThermalNodes)
                {
                    float bulkResistance;
                    Dictionary<Node, Link> path;
                    Node outletNode;

                    if (SearchPathFrom(currentRoomNode, usedLinkTimes, out bulkResistance, out path, out outletNode))
                    {
                        //StructureThermodynamicsOverhaul.debugLogToChat("found path for room " + currentRoomID.ToString());
                        long exRoomID = -1;
                        bool isRoom = false;
                        if (outletNode.type == Node.Type.Room)
                        {
                            exRoomID = outletNode.roomID;
                            isRoom = true;
                        }

                        float inCond = -1f;
                        float inRad = -1f;
                        float exCond = -1f;
                        float exRad = -1f;

                        // do stuff to create calculations for Update loop

                        foreach (Link link in currentRoomNode.links)
                        {
                            if (link.type == Link.Type.SurfaceConduction)
                            {
                                inCond = link.resistance;
                            }
                            else if (link.type == Link.Type.Radiation)
                            {
                                inRad = link.resistance;
                            }
                        }

                        foreach (Link link in outletNode.links)
                        {
                            if (link.type == Link.Type.SurfaceConduction)
                            {
                                exCond = link.resistance;
                            }
                            else if (link.type == Link.Type.Radiation)
                            {
                                exRad = link.resistance;
                            }
                        }

                        ThermalResistances currentNewThermalChannel = new ThermalResistances(inCond, inRad, bulkResistance, exCond, exRad, isRoom, exRoomID);
                        //StructureThermodynamicsOverhaul.debugLogToChat(currentNewThermalChannel.ToString());
                        float currentRTotal = parallelResistance(exCond, exRad) + bulkResistance;

                        List<ThermalResistances> adjacentThermalChannels;
                        if (findThermalResistancesByNode.ContainsKey(currentRoomNode))
                        {
                            float totalRforAllChannels = findNodeTotalPathResistanceSums[currentRoomNode] + currentRTotal;
                            findNodeTotalPathResistanceSums[currentRoomNode] = totalRforAllChannels;
                            adjacentThermalChannels = findThermalResistancesByNode[currentRoomNode];
                            foreach (ThermalResistances adjacentChannel in adjacentThermalChannels)
                            {                         
                                float adjChannelR = parallelResistance(adjacentChannel.externalConduction, adjacentChannel.externalRadiation) + adjacentChannel.bulkConduction;
                                float newRatio = adjChannelR / totalRforAllChannels;
                                adjacentChannel.sharedInternalAreaFraction = newRatio;
                            }
                            currentNewThermalChannel.sharedInternalAreaFraction = currentRTotal / totalRforAllChannels;
                            adjacentThermalChannels.Add(currentNewThermalChannel);
                            findThermalResistancesByNode[currentRoomNode] = adjacentThermalChannels;
                            roomThermalResistances.Add(currentNewThermalChannel);
                        }
                        else
                        {
                            adjacentThermalChannels = new List<ThermalResistances>();
                            adjacentThermalChannels.Add(currentNewThermalChannel);
                            findThermalResistancesByNode.Add(currentRoomNode, adjacentThermalChannels);
                            findNodeTotalPathResistanceSums.Add(currentRoomNode, currentRTotal);
                            roomThermalResistances.Add(currentNewThermalChannel);
                        }

                        //temporary increase resistances of current path for next iterations (to discourage same path)
                        Node startSurfaceNode = currentRoomNode.links.FirstOrDefault().getNextNode(currentRoomNode);
                        Node endSurfaceNode = outletNode.links.FirstOrDefault().getNextNode(outletNode);
                        foreach(Link link in outletNode.links)
                        {
                            if (usedLinkTimes.ContainsKey(link))
                            {
                                usedLinkTimes[link]++;
                            }
                            else
                            {
                                usedLinkTimes.Add(link, 1);
                            }
                        }

                        while (!endSurfaceNode.Equals(startSurfaceNode))
                        {
                            //remove links
                            Link link = path[endSurfaceNode];
                            if (usedLinkTimes.ContainsKey(link))
                            {
                                usedLinkTimes[link]++;
                            }
                            else
                            {
                                usedLinkTimes.Add(link, 1);
                            }
                            endSurfaceNode = link.getNextNode(endSurfaceNode);
                        }
                    }
                }
            }
            
            //roomThermalsCalculated = true;
            return roomThermalResistances;         
        }

        //returns true if successful 
        public bool SearchPathFrom(Node startNode, Dictionary<Link, int> usedLinkTimes, out float pathResistance, out Dictionary<Node, Link> pathBack, out Node endNode)
        {
            Dictionary<Node, float> openNodes = new Dictionary<Node, float>();
            List<Node> closedNodes = new List<Node>();
            pathBack = new Dictionary<Node, Link>();
            pathResistance = -1f;
            endNode = startNode;

            long startRoomID = -1;
            if (startNode.type == Node.Type.Room)
            {
                startRoomID = startNode.roomID;
                openNodes.Add(startNode.links.First().getNextNode(startNode), 0.0f);
                closedNodes.Add(startNode);
            }
            else
            {
                openNodes.Add(startNode, 0.0f);
            }
            
            int maxIter = 100;
            bool search = true;
            for (int iter = 0; ((iter < maxIter) && search); iter++)
            {
                Node currentNode = minValueInDictionary(openNodes);
                float currentCost = openNodes[currentNode];

                foreach (Link link in currentNode.links)
                {
                    if (link.type == Link.Type.Dummy || usedLinkTimes.ContainsKey(link))
                    {
                        continue;
                    }

                    Node nighbourNode = link.getNextNode(currentNode);

                    if (nighbourNode.type == Node.Type.Dummy)
                    {
                        continue;
                    }

                    if (link.type == Link.Type.BulkConduction)
                    {
                        if (!closedNodes.Contains(nighbourNode))
                        {
                            //evaluate node
                            float cost = currentCost + link.resistance;

                            if (openNodes.ContainsKey(nighbourNode))
                            {
                                if (openNodes[nighbourNode] > cost)
                                {
                                    openNodes[nighbourNode] = cost;
                                    pathBack[nighbourNode] = link;
                                }
                            }
                            else
                            {
                                openNodes.Add(nighbourNode, cost);
                                pathBack.Add(nighbourNode, link);
                            }
                        }
                    }
                    else
                    {
                        //check if path found
                        if ((nighbourNode.type == Node.Type.Atmosphere) || (nighbourNode.type == Node.Type.Room && nighbourNode.roomID != startRoomID))
                        {
                            //solution found, leads to atmosphere
                            search = false;
                            pathResistance = currentCost;
                            endNode = nighbourNode;
                            return true;
                        }
                    }
                }
                //close node
                closedNodes.Add(currentNode);
                openNodes.Remove(currentNode);

                if (openNodes.Count == 0)
                {
                    //return no solution found
                    search = false;
                    StructureThermodynamicsOverhaul.debugLogToChat("No outlet for node in room " + startNode.roomID.ToString());
                }
            }
            if (search)
            {
                StructureThermodynamicsOverhaul.debugLogToChat("Max iterations reached for node in room " + startNode.roomID.ToString());
            }

            return false;
        }
        private Node minValueInDictionary(Dictionary<Node, float> list)
        {
            KeyValuePair<Node, float> first = list.First();
            Node output = first.Key;
            float minVal = first.Value;
            foreach (KeyValuePair<Node, float> kvp in list)
            {
                if (kvp.Value < minVal)
                {
                    output = kvp.Key;
                    minVal = kvp.Value;
                }
            }
            return output;
        }
        private List<AverageRoomConduit> CalculateRoomAverageResistances(long roomID, List<ThermalResistances> thermalChannels)
        {
            List<AverageRoomConduit> outputConduits = new List<AverageRoomConduit>();

            //find all same outlets
            Dictionary<long, List<ThermalResistances>> exRoomChannels = new Dictionary<long, List<ThermalResistances>>();
            List<ThermalResistances> atmoChannels = new List<ThermalResistances>();

            //group channels into outlets
            foreach (ThermalResistances thermalResistances in thermalChannels)
            {
                if (thermalResistances.isExternalRoom)
                {
                    if (exRoomChannels.ContainsKey(thermalResistances.externalRoomID))
                    {
                        exRoomChannels[thermalResistances.externalRoomID].Add(thermalResistances);
                    }
                    else
                    {
                        List<ThermalResistances> newList = new List<ThermalResistances>();
                        newList.Add(thermalResistances);
                        exRoomChannels.Add(thermalResistances.externalRoomID, newList);
                    }
                }
                else
                {
                    atmoChannels.Add(thermalResistances);
                }
            }

            AverageRoomConduit atmoAverageThermals = calculteAverageResistances(atmoChannels, roomID, false, -1);
            outputConduits.Add(atmoAverageThermals);

            foreach (KeyValuePair<long, List<ThermalResistances>> kvp in exRoomChannels)
            {
                long externalRoomId = kvp.Key;
                AverageRoomConduit roomAverageThermals = calculteAverageResistances(kvp.Value, roomID, true, externalRoomId);
                outputConduits.Add(roomAverageThermals);
            }
            
            return outputConduits;
        }
        private AverageRoomConduit calculteAverageResistances (List<ThermalResistances> resistancesList, long currentRoomID, bool isToAnotherRoom, long externalRoomID)
        {
            //index 0 = outflux, 1 = influx
            float[] inRAv = new float[] { 0.0f, 0.0f };
            float[] RbAv = new float[] { 0.0f, 0.0f };
            float[] exRAv = new float[] { 0.0f, 0.0f };

            float[] inRP = new float[] { 0.0f, 0.0f };
            float[] RbP = new float[] { 0.0f, 0.0f };
            float[] exRP = new float[] { 0.0f, 0.0f };
            float inRatioCvR = 0.0f;
            float exRatioCvR = 0.0f;

            float[] totaR = new float[] { 0.0f, 0.0f };

            bool first = true;
            int count = 0;
            foreach (ThermalResistances resistances in resistancesList)
            {
                count++;
                float inR0 = resistances.internalConduction * resistances.sharedInternalAreaFraction;
                float inR1 = parallelResistance(resistances.internalConduction, resistances.internalRadiation) * resistances.sharedInternalAreaFraction;
                float exR0 = parallelResistance(resistances.externalConduction, resistances.externalRadiation);
                float exR1 = resistances.externalConduction;

                float totalR0 = inR0 + resistances.bulkConduction + exR0;
                float totalR1 = inR1 + resistances.bulkConduction + exR1;

                if (first)
                {
                    inRAv[0] = inR0;
                    inRAv[1] = inR1;
                    RbAv[0] = resistances.bulkConduction;
                    RbAv[1] = RbAv[0];
                    exRAv[0] = exR0;
                    exRAv[1] = exR1;

                    totaR[0] = totalR0;
                    totaR[1] = totalR1;
                }
                else
                {
                    inRAv[0] = parallelResistance(inRAv[0], inR0);
                    inRAv[1] = parallelResistance(inRAv[1], inR1);
                    RbAv[0] = parallelResistance(RbAv[0], resistances.bulkConduction);
                    RbAv[1] = RbAv[0];
                    exRAv[0] = parallelResistance(exRAv[0], exR0);
                    exRAv[1] = parallelResistance(exRAv[1], exR1);

                    totaR[0] = parallelResistance(totaR[0], totalR0);
                    totaR[1] = parallelResistance(totaR[1], totalR1);
                }

                inRP[0] += inR0 / totalR0;
                inRP[1] += inR1 / totalR1;
                exRP[0] += exR0 / totalR0;
                exRP[1] += exR1 / totalR1;
                RbP[0] += resistances.bulkConduction / totalR0;
                RbP[1] += resistances.bulkConduction / totalR1;

                inRatioCvR += resistances.internalConduction / (resistances.internalConduction + resistances.internalRadiation);
                exRatioCvR += resistances.externalConduction / (resistances.externalConduction + resistances.externalRadiation);

                first = false;
            }

            if (count > 0)
            {
                inRP[0] = inRP[0] / (float)(count);
                inRP[1] = inRP[1] / (float)(count);
                exRP[0] = exRP[0] / (float)(count);
                exRP[1] = exRP[1] / (float)(count);
                RbP[0] = RbP[0] / (float)(count);
                RbP[1] = RbP[1] / (float)(count);

                inRatioCvR = inRatioCvR / (float)(count);
                exRatioCvR = exRatioCvR / (float)(count);
            }
            float[] diff = new float[] { (totaR[0] - (inRAv[0] + RbAv[0] + exRAv[0])) / totaR[0], (totaR[1] - (inRAv[1] + RbAv[1] + exRAv[1])) / totaR[1] };

            float[] finalInR = new float[2];
            finalInR[0] = (totaR[0] * inRP[0] * (1 - diff[0])) + (inRAv[0] * diff[0]);
            finalInR[1] = (totaR[1] * inRP[1] * (1 - diff[1])) + (inRAv[1] * diff[1]);
            float[] finalExR = new float[2];
            finalExR[0] = (totaR[0] * exRP[0] * (1 - diff[0])) + (exRAv[0] * diff[0]);
            finalExR[1] = (totaR[1] * exRP[1] * (1 - diff[1])) + (exRAv[1] * diff[1]);
            float[] finalBulkR = new float[2];
            finalBulkR[0] = (totaR[0] * RbP[0] * (1 - diff[0])) + (RbAv[0] * diff[0]);
            finalBulkR[1] = (totaR[1] * RbP[1] * (1 - diff[1])) + (RbAv[1] * diff[1]);

            return new AverageRoomConduit(finalInR, finalBulkR, finalExR, inRatioCvR, exRatioCvR, currentRoomID, isToAnotherRoom, externalRoomID);
        }
        #endregion

        #region onThermalGameTick functions
        public bool isReadyForUpdateTick()
        {
            return thermalsCalculated;
        }
        public string ThermalUpdateSimulation(float simulateSeconds)
        {
            return ThermalUpdateSimulation(simulateSeconds, true);
        }

        public string ThermalUpdateSimulation(float simulateSeconds, bool updateRooms)
        {
            //Stopwatch debugTime = Stopwatch.StartNew();

            if (!thermalsCalculated)
            {
                return "No thermal calculated";
            }

            Dictionary<Room, float> roomEnergyChanges = new Dictionary<Room, float>();

            Dictionary<Room, debugRoomData> debugDictRoomEnergy = new Dictionary<Room, debugRoomData>();
            string debugOut = "";
            //int rooms = 0;
            int paths = 0;       

            foreach (AverageRoomConduit conduit in averageRoomConduits)
            {
                paths++;
                long currentRoomID = conduit.currentRoomID;
                Room currentRoom = allRoomsDictionary[currentRoomID];
                float internalPressure = currentRoom.Pressure.ToFloat();
                float internalTemperature = currentRoom.Temperature.ToFloat();

                //debug stuff
                debugRoomData debugRoomData;
                AverageRoomConduit debugConduit = new AverageRoomConduit(-1, -1, -1, -1, -1, -1, -1, -1, currentRoomID, conduit.isToAnotherRoom, conduit.destinationRoomID);
                if (debugDictRoomEnergy.ContainsKey(currentRoom))
                {
                    debugRoomData = debugDictRoomEnergy[currentRoom];
                }
                else
                {
                    debugRoomData = new debugRoomData("");
                }
                //end of debug stuff

                if (internalPressure < ThermalConstants.MinPressure) //skip checking if no energy in the room
                {
                    continue;
                }

                Atmosphere exAtmosphere = PlanetaryAtmosphereSimulation.ReadOnlyGlobal(allRoomsDictionary[currentRoomID].Grids.First());
                float exAtmosphereTemperature = exAtmosphere.Temperature.ToFloat();
                float exAtmospherePressure = exAtmosphere.PressureGasses.ToFloat();

                Room destinationRoom = conduit.isToAnotherRoom ? allRoomsDictionary[conduit.destinationRoomID] : null;
                long destinationRoomID = conduit.destinationRoomID;

                float exTemp = conduit.isToAnotherRoom ? destinationRoom.Temperature.ToFloat() : exAtmosphereTemperature;
                float exPressure = conduit.isToAnotherRoom ? destinationRoom.Pressure.ToFloat() : exAtmospherePressure;

                float deltaT = internalTemperature - exTemp;
                bool dirOut = deltaT > 0;
                int index = dirOut ? 0 : 1;

                if ((dirOut && deltaT < ThermalConstants.MinDeltaT) || (!dirOut && (-1 * deltaT) < ThermalConstants.MinDeltaT))
                {
                    continue; //skip if minimmal energy transfer
                }

                float inGasCondCoeff = ((internalPressure > 1.0f) ? 1.0f : internalPressure) * ThermalConstants.getGasConductionCoefficent(ThermalConstants.Gas.Default);
                float exGasCondCoeff = ((exPressure > 1.0f) ? 1.0f : exPressure) * ThermalConstants.getGasConductionCoefficent(ThermalConstants.Gas.Default);
                float radCoeff = (((exTemp * exTemp) + (internalTemperature * internalTemperature)) * (exTemp + internalTemperature) / ThermalConstants.StefanBoltzmannConstInv);

                //if outward, inR = inRc * inCoeff, exR = exRC || exRad
                //if inward, inR = inRC || exRad, exR = exRC * exCoeff

                float totalR = conduit.Rbulk[index];
                debugConduit.Rbulk[index] = conduit.Rbulk[index];

                if (dirOut)
                {
                    totalR += conduit.Rin[index] / inGasCondCoeff;
                    debugConduit.Rin[index] = totalR - conduit.Rbulk[index];
                    debugConduit.ratioCvRin = 1.0f;

                    if (exPressure > ThermalConstants.MinPressure)
                    {
                        totalR += conduit.Rout[index] / ((exGasCondCoeff * radCoeff) * ((conduit.ratioCvRout / exGasCondCoeff) + ((1 - conduit.ratioCvRout) / radCoeff)));
                        debugConduit.ratioCvRout = conduit.ratioCvRout * (exGasCondCoeff / radCoeff);
                    }
                    else
                    {
                        totalR += conduit.Rout[index] / (conduit.ratioCvRout * radCoeff);
                        debugConduit.ratioCvRout = 0.0f;
                    }
                    debugConduit.Rout[index] = totalR - debugConduit.Rin[index] - debugConduit.Rbulk[index];
                }
                else
                {
                    if (internalPressure > ThermalConstants.MinPressure)
                    {
                        totalR += conduit.Rin[index] / ((inGasCondCoeff * radCoeff) * ((conduit.ratioCvRin / inGasCondCoeff) + ((1 - conduit.ratioCvRin) / radCoeff)));
                        debugConduit.ratioCvRin = conduit.ratioCvRin * (inGasCondCoeff / radCoeff);
                    }
                    else
                    {
                        totalR += conduit.Rin[index] / (conduit.ratioCvRin * radCoeff);
                        debugConduit.ratioCvRin = 0.0f;
                    }

                    debugConduit.Rin[index] = totalR - conduit.Rbulk[index];

                    if (exPressure > ThermalConstants.MinPressure)
                    {
                        totalR += conduit.Rout[index] / exGasCondCoeff;
                        debugConduit.Rout[index] = totalR - debugConduit.Rin[index] - debugConduit.Rbulk[index];
                        debugConduit.ratioCvRout = 1.0f;
                    }
                    else
                    {
                        continue; //if no atmo in destination, no energy is taken
                    }
                }

                float Q = deltaT / totalR;
                float totalEnergyLost = Q * simulateSeconds * (ConfigManager.Config_ThermalMultiplier / ThermalConstants.ThermalMultiplierOffset);
                totalEnergyLost = conduit.isToAnotherRoom ? totalEnergyLost / 2.0f : totalEnergyLost;

                //debug stuff
                debugRoomData.resistances += debugConduit.ToString() + "\n";

                if (debugDictRoomEnergy.ContainsKey(currentRoom))
                {
                    debugDictRoomEnergy[currentRoom] = debugRoomData;
                }
                else
                {
                    debugDictRoomEnergy.Add(currentRoom, debugRoomData);
                }
                //end of debug stuff


                if (roomEnergyChanges.ContainsKey(currentRoom))
                {
                    roomEnergyChanges[currentRoom] = roomEnergyChanges[currentRoom] - totalEnergyLost;
                }
                else
                {
                    roomEnergyChanges.Add(currentRoom, (-1f * totalEnergyLost));
                }
                if (conduit.isToAnotherRoom)
                {
                    if (roomEnergyChanges.ContainsKey(destinationRoom))
                    {
                        roomEnergyChanges[destinationRoom] = roomEnergyChanges[destinationRoom] + totalEnergyLost;
                    }
                    else
                    {
                        roomEnergyChanges.Add(destinationRoom, totalEnergyLost);
                    }
                }

            }

            if (updateRooms)
            {
                foreach (KeyValuePair<Room, float> roomKvP in roomEnergyChanges)
                {
                    Room room = roomKvP.Key;
                    float energyChange = roomKvP.Value;

                    if ((room.GasMixture.TotalEnergy.ToFloat() - energyChange) > 0 && room.Pressure.ToFloat() > ThermalConstants.MinPressure)
                    {
                        changeRoomEnergy(room.RoomId, energyChange);
                    }
                }
            }

            debugOut = "Channels: " + paths.ToString() + "\n";
            foreach(var kvp in debugDictRoomEnergy)
            {
                Room room = kvp.Key;
                if (roomEnergyChanges.ContainsKey(room))
                {
                    debugRoomData drd = kvp.Value;
                    debugOut += "[R" + room.RoomId.ToString() + " T:" + room.Temperature.ToFloat().ToString("0.0") + " Q:" + roomEnergyChanges[room].ToString("#,##0") + "]\n";
                    debugOut += drd.resistances;
                }
            }
            return debugOut;
        }
        private void changeRoomEnergy(long roomID, float energy)
        {
            if (roomAtmosphereDictionary.ContainsKey(roomID))
            {
                bool gainEnergy = energy > 0;
                energy = Math.Abs(energy);

                List<Atmosphere> roomAtmospheres = roomAtmosphereDictionary[roomID];
                int totalBlocks = roomAtmospheres.Count;
                int blocksToChangeEnergy = (int)(energy / ThermalConstants.EnergyPerAtmoBlock) + 1;
                blocksToChangeEnergy = blocksToChangeEnergy > totalBlocks ? totalBlocks : blocksToChangeEnergy;

                float energyPerBlock = energy / (float)(blocksToChangeEnergy);
                MoleEnergy energyDelta = new MoleEnergy(energyPerBlock);

                //StructureThermodynamicsOverhaul.debugLogToChat("-- " + energyPerBlock.ToString() + " x " + blocksToChangeEnergy.ToString(), true);

                for (int i = 0; i < blocksToChangeEnergy; i++)
                {
                    if (gainEnergy)
                    {
                        roomAtmospheres[i].GasMixture.AddEnergy(energyDelta);
                        //StructureThermodynamicsOverhaul.debugLogToChat("add -- " + energyDelta.ToString(), true);
                    }
                    else
                    {
                        roomAtmospheres[i].GasMixture.RemoveEnergy(energyDelta);
                        //StructureThermodynamicsOverhaul.debugLogToChat("remove -- " + energyDelta.ToString(), true);
                    }

                }             
            }
        }
        #endregion

        #region Debugging functions
        public string serealizeNodes()
        {
            if (wordNodes.Count == 0)
            {
                return "No nodes";
            }

            string output = "";
            foreach (Node node in wordNodes)
            {
                output += node.ToString() + "\n";
            }

            return output;
        }
        public string serealizeResistances()
        {
            if (thermalChannels.Count == 0)
            {
                return "No thermals calculated";
            }

            string output = "";
            foreach (var room in thermalChannels)
            {
                output += "Room: " + room.Key.ToString() + "\n";

                //StructureThermodynamicsOverhaul.debugLogToChat(room.Value.Count().ToString());

                foreach (ThermalResistances res in room.Value)
                {
                    //StructureThermodynamicsOverhaul.debugLogToChat(" * " + res.ToString());
                    output += " * " + res.ToString() + "\n";
                }
                output += "\n";
            }

            return output;
        }
        public string allPathsToString()
        {
            string output = "";
            foreach (KeyValuePair<long, List<ThermalResistances>> roomThermalResistances in thermalChannels)
            {
                output += "Room:" + roomThermalResistances.Key.ToString() + "\n";
                foreach (ThermalResistances thermalChannel in roomThermalResistances.Value)
                {
                    output += thermalChannel.ToString() + "\n";
                }
            }
            return output;
        }
        public string allAverageConduitsToString()
        {
            string output = "";
            foreach (AverageRoomConduit cond in averageRoomConduits)
            {
                output += cond.ToString() + "\n";
            }
            return output;
        }
        #endregion
    }
}
