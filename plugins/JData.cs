// Requires: Picasso
#define DEBUG
#define DISABLED_DEBUG_PINS
using System.Collections.Generic;
using System;
using System.Drawing;
using System.IO;
using Facepunch;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Game.Rust.Libraries.Covalence;
using UnityEngine;
namespace Oxide.Plugins {

    [Info("JData", "JohnWillikers", "0.1.0")]
    [Description("Data Stuff")]
    class JData : CovalencePlugin {
        // Begin Plugin References
       [PluginReference] 
        private Picasso Picasso;
        // End Plugin References
        private DataFileSystem data_dir = new DataFileSystem($"{Interface.Oxide.DataDirectory}");
        private Dictionary<string, Project> user_loaded_projects = new Dictionary<string, Project>(); 
        public void SetNewDataDir(string name)
        {
            user_loaded_projects.Clear();
            data_dir = new DataFileSystem($"{Interface.Oxide.DataDirectory}\\{name}");
        }

        [HookMethod("BuildChip")]
        public void BuildChip(string chip_name, IPlayer player)
        {
            Project loaded_project = GetUserProject(player.Id);
            if (loaded_project == null)
            {
                player.Reply($"No project loaded | /c_load <project_name>");
                return;
            }
            Chip chip = loaded_project.ReadChip(chip_name);
            if (chip == null)
                return;

            player.Reply($"Spawning {chip.Name}");

            GenericPosition position = player.Position();

            chip.Init(0);
            var buildObjectStuff = chip.Build(player, this, loaded_project, new Vector3(position.X, position.Y, position.Z - 3), new Quaternion(0, 0, 0, 0)); //new Quaternion(1, 1, 0, 0));
            player.Reply($"Built {buildObjectStuff[2]} electrical entities");
        }

        [Command("c_clear")]
        private void ProjectClearCommand(IPlayer player, string command, string[] args)
        {   
            ClearEntities(player);
        }

        public void ClearEntities(IPlayer player) {
            player.Reply("Clearing");
            Project project = GetUserProject(player.Id.ToString());
            project.ClearCommonEntities();
            player.Reply("Done Clearing");
        }

        public Project AssignProjectToUser(string uId, string project_name)
        {
            Project loaded_project = ReadProject(project_name);
            if (loaded_project == null)
                return null;

            if (user_loaded_projects.ContainsKey(uId))
                user_loaded_projects[uId] = loaded_project;
            else
                user_loaded_projects.Add(uId, loaded_project);

            return loaded_project;
        }

        public bool UnloadUserProject(string uId)
        {
            if (user_loaded_projects.ContainsKey(uId)) {
                user_loaded_projects.Remove(uId);
                Puts($"Unloaded {uId}'s project");
                return true;
            } else {
                return false;
            }

        }

        public Project GetUserProject(string uId)
        {
            return (user_loaded_projects.ContainsKey(uId))
                    ? user_loaded_projects[uId]
                    : null;
        }

        public void BindSaveSign(Vector3 position, Quaternion rotation, Picasso.Signs sign_type, int width, int height, int yOffset, Picasso.FontSize fontSize, Dictionary<string, Brush> lines, System.Drawing.Color backgroundColor = default(System.Drawing.Color))
        {
            Picasso.SpawnSign(
                position,
                rotation,
                sign_type,
                width,
                height,
                yOffset,
                fontSize,
                lines,
                backgroundColor
            );
        }

        private Project ReadProject(string name)
        {
            if (data_dir.ExistsDatafile($"{name}/ProjectSettings"))
            {
                Project project =  data_dir.ReadObject<Project>($"{name}/ProjectSettings");
                project.dataFileSystem = new DataFileSystem($"{Interface.Oxide.DataDirectory}\\rust_circuit_boss\\{name}");
                return project;
            }
            else
                return null;
        }
    }

     // BEGIN DATATYPES FOR DE-SERIALIZATION 
    enum Gates {
        SPLITTER = 0,
        BLOCKER = 1,
        MEMORY_CELL = 2,
        E_BRANCH = 3,
        AND = 4,
        OR = 5,
        XOR = 6,
        TEST_GENERATOR = 7,
        SIMPLE_SWITCH = 8,
        SMART_SWITCH = 9,
        GREEN_LIGHT = 10,
        RED_LIGHT = 11,
        WHITE_LIGHT = 12,
        NOT = 13,
        TIMER = 14
    }

    class RustComponentDefinition {
        public byte slotNumber;
        public Vector3 offset;
    }

    class ParentHelpers {
        // Prefabs
        public Dictionary<Gates, string> prefab_gate_bindings = new Dictionary<Gates, string>{
            { Gates.SPLITTER, "assets/prefabs/deployable/playerioents/splitter/splitter.prefab" },
            { Gates.BLOCKER, "assets/prefabs/deployable/playerioents/gates/blocker/electrical.blocker.deployed.prefab" },
            { Gates.MEMORY_CELL, "assets/prefabs/deployable/playerioents/gates/dflipflop/electrical.memorycell.deployed.prefab" },
            { Gates.E_BRANCH, "assets/prefabs/deployable/playerioents/gates/branch/electrical.branch.deployed.prefab" },
            { Gates.AND, "assets/prefabs/deployable/playerioents/gates/andswitch/andswitch.entity.prefab" },
            { Gates.OR, "assets/prefabs/deployable/playerioents/gates/orswitch/orswitch.entity.prefab" },
            { Gates.XOR, "assets/prefabs/deployable/playerioents/gates/xorswitch/xorswitch.entity.prefab" },
            { Gates.TEST_GENERATOR, "assets/prefabs/deployable/playerioents/generators/generator.small.prefab" },
            { Gates.SIMPLE_SWITCH, "assets/prefabs/deployable/playerioents/simpleswitch/switch.prefab" },
            { Gates.SMART_SWITCH, "assets/prefabs/deployable/playerioents/app/smartswitch/smartswitch.prefab" },
            { Gates.GREEN_LIGHT, "assets/prefabs/misc/permstore/industriallight/industrial.wall.lamp.green.deployed.prefab" },
            { Gates.RED_LIGHT, "assets/prefabs/misc/permstore/industriallight/industrial.wall.lamp.red.deployed.prefab" },
            { Gates.WHITE_LIGHT, "assets/prefabs/misc/permstore/industriallight/industrial.wall.lamp.deployed.prefab" },
            { Gates.NOT, "na"},
            { Gates.TIMER, "assets/prefabs/deployable/playerioents/timers/timer.prefab"}
        };
        // Strings
        public Dictionary<string, Gates> string_to_gates = new Dictionary<string, Gates> {
            { "SPLITTER", Gates.SPLITTER },
            { "BLOCKER", Gates.BLOCKER },
            { "MEMORY_CELL", Gates.MEMORY_CELL },
            { "E_BRANCH", Gates.E_BRANCH },
            { "AND", Gates.AND },
            { "OR", Gates.OR },
            { "XOR", Gates.XOR },
            { "TEST_GENERATOR", Gates.TEST_GENERATOR },
            { "SWITCH", Gates.SIMPLE_SWITCH },
            { "SMART_SWITCH", Gates.SMART_SWITCH },
            { "G_LIGHT", Gates.GREEN_LIGHT },
            { "R_LIGHT", Gates.RED_LIGHT },
            { "W_LIGHT", Gates.WHITE_LIGHT },
            { "NOT", Gates.NOT },
            { "TIMER", Gates.TIMER }
        };
        // IO Definitions
        public Dictionary<Gates, Dictionary<string,RustComponentDefinition>> io_definitions = new Dictionary<Gates, Dictionary<string, RustComponentDefinition>>
        {
            {Gates.TEST_GENERATOR, new Dictionary<string, RustComponentDefinition>
                {
                    {
                        "Power Output 1",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(0f, -.77f, 0f)
                        }
                    },
                    {
                        "Power Output 2",
                        new RustComponentDefinition{
                            slotNumber = 1,
                            offset = new Vector3(0f, -.77f, -.42f)
                        }
                    },
                    {
                        "Power Output 3",
                        new RustComponentDefinition{
                            slotNumber = 2,
                            offset = new Vector3(0f, -.77f, .42f)
                        } 
                    }
                }
            },
            {Gates.NOT, new Dictionary<string, RustComponentDefinition>
                {
                    {
                        "Input A",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(.03f, -.8f, 0f)
                        }
                    },
                    {
                        "Power Out",
                        new RustComponentDefinition{
                            slotNumber = 1,
                            offset = new Vector3(0f, -1.28f, 0f)
                        }
                    }
                }
            },
            {Gates.AND, new Dictionary<string, RustComponentDefinition>
                {
                    {
                        "Input A",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(.03f, -.8f, 0f)
                        }
                    },
                    {
                        "Input B",
                        new RustComponentDefinition{
                            slotNumber = 1,
                            offset = new Vector3(-.03f, -.8f, 0f)
                        }
                    },
                    {
                        "Power Out",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(0f, -1.28f, 0f)
                        }
                    }
                }
            },
            {Gates.XOR, new Dictionary<string, RustComponentDefinition>
                {
                    {
                        "Input A",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(.03f, -.8f, 0f)
                        }
                    },
                    {
                        "Input B",
                        new RustComponentDefinition{
                            slotNumber = 1,
                            offset = new Vector3(-.03f, -.8f, 0f)
                        }
                    },
                    {
                        "Power Out",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(0f, -1.28f, 0f)
                        }
                    }
                }
            },
            {Gates.OR, new Dictionary<string, RustComponentDefinition>
                {
                    {
                        "Input A",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(.03f, -.8f, 0f)
                        }
                    },
                    {
                        "Input B",
                        new RustComponentDefinition{
                            slotNumber = 1,
                            offset = new Vector3(-.03f, -.8f, 0f)
                        }
                    },
                    {
                        "Power Out",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(0f, -1.28f, 0f)
                        }
                    }
                }
            },
            {Gates.MEMORY_CELL, new Dictionary<string, RustComponentDefinition>
                {
                    {
                        "Power In",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(0f, .15f, 0f)
                        }
                    },
                    {
                        "SET",
                        new RustComponentDefinition{
                            slotNumber = 1,
                            offset = new Vector3(.08f, -.06f, 0f)
                        }
                    },
                    {
                        "RESET",
                        new RustComponentDefinition{
                            slotNumber = 2,
                            offset = new Vector3(.08f, .02f, 0f)
                        }
                    },
                    {
                        "TOGGLE",
                        new RustComponentDefinition{
                            slotNumber = 3,
                            offset = new Vector3(.08f, .08f, 0f)
                        }
                    },
                    {
                        "Out",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(.04f, -.144f, 0f)
                        }
                    },
                    {
                        "Inverted_Out",
                        new RustComponentDefinition{
                            slotNumber = 1,
                            offset = new Vector3(-.04f, -.144f, 0f)
                        }
                    }
                }
            },
            {Gates.BLOCKER, new Dictionary<string, RustComponentDefinition>
                {
                    {
                        "Power In",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(0f, .15f, 0f)
                        }
                    },
                    {
                        "Block Pass",
                        new RustComponentDefinition{
                            slotNumber = 1,
                            offset = new Vector3(-.05f, 0f, 0f)
                        }
                    },
                    {
                        "Power Out",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(0f, -.15f, 0f)
                        }
                    }
                }
            },
            {Gates.SPLITTER, new Dictionary<string, RustComponentDefinition>
                {
                    {
                        "Power In",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(0f, -1.4f, 0f)
                        }
                    },
                    {
                        "Power Out 1",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(-.13f, -.82f, 0f)
                        }
                    },
                    {
                        "Power Out 2",
                        new RustComponentDefinition{
                            slotNumber = 1,
                            offset = new Vector3(0f, -.82f, 0f)
                        }
                    },
                    {
                        "Power Out 3",
                        new RustComponentDefinition{
                            slotNumber = 2,
                            offset = new Vector3(.13f, -.82f, 0f)
                        }
                    }
                }
            },
            {Gates.E_BRANCH, new Dictionary<string, RustComponentDefinition>
                {
                    {
                        "Power Out",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(.05f, -.15f, 0f)
                        }
                    },
                    {
                        "Branch Out",
                        new RustComponentDefinition{
                            slotNumber = 1,
                            offset = new Vector3(-.05f, -.15f, 0f)
                        }
                    },
                    {
                        "Power In",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(0f, .15f, 0f)
                        }
                    }
                }
            },
            {Gates.SIMPLE_SWITCH, new Dictionary<string, RustComponentDefinition>
                {
                    {
                        "Electric Input",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(0f, -.8f, 0f)
                        }
                    }, 
                    {
                        "Output",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(0f, -1.18f, 0f)
                        }
                    }
                }
            },
            {Gates.SMART_SWITCH, new Dictionary<string, RustComponentDefinition>
                {
                    {
                        "Electric Input",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(0f, -.8f, 0f)
                        }
                    }, 
                    {
                        "Output",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(0f, -1.18f, 0f)
                        }
                    }
                }
            },
            {Gates.GREEN_LIGHT, new Dictionary<string, RustComponentDefinition>
                {
                    {
                        "Power In",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(.2f, 0, 0)
                        }
                    }, 
                    {
                        "Passthrough",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(-.2f, 0, 0)
                        }
                    }
                }
            },
            {Gates.RED_LIGHT, new Dictionary<string, RustComponentDefinition>
                {
                    {
                        "Power In",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(.2f, 0, 0)
                        }
                    }, 
                    {
                        "Passthrough",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(-.2f, 0, 0)
                        }
                    }
                }
            },
            {Gates.WHITE_LIGHT, new Dictionary<string, RustComponentDefinition>
                {
                    {
                        "Power In",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(.2f, 0, 0)
                        }
                    }, 
                    {
                        "Passthrough",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(-.2f, 0, 0)
                        }
                    }
                }
            },
            {Gates.TIMER, new Dictionary<string, RustComponentDefinition>
                {
                    {
                        "Electric Input",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(0f, -.8f, 0f)
                        }
                    }, 
                    {
                        "Toggle On",
                        new RustComponentDefinition{
                            slotNumber = 1,
                            offset = new Vector3(.15f, -1.05f, 0f)
                        }
                    }, 
                    {
                        "Output",
                        new RustComponentDefinition{
                            slotNumber = 0,
                            offset = new Vector3(0f, -1.28f, 0f)
                        }
                    }
                }
            }
        };

        public void ClearCommonEntities()
        {
            foreach (KeyValuePair<Gates, string> entry in prefab_gate_bindings) {
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), $"del {entry.Value}");
            }

            foreach (string prefab in Picasso.SignBindings) {
                ConsoleSystem.Run(ConsoleSystem.Option.Server.Quiet(), $"del {prefab}");
            }
        }

        /**
         * Spawns an Entity in the World
         *
         * @param string prefab
         * @param Vector3 pos
         * @param Quaternion tor
         */
        public BaseEntity SpawnEntity(string prefab, Vector3 pos, Quaternion rot)
        {
            var entity = GameManager.server.CreateEntity(prefab, pos, rot);

            var transform = entity.transform;
            transform.position = pos;
            transform.rotation = rot;

            var eBranch = entity as ElectricalBranch;
            if (eBranch != null)
                eBranch.branchAmount = 100000;

            var testGen = entity as ElectricGenerator;
            if (testGen != null)
                testGen.electricAmount = 2140000000;

            var timerSwitch = entity as TimerSwitch;
            if (timerSwitch != null)
            {
                timerSwitch.timerLength = 2;
            }

            entity.Spawn();

            return entity;
        }

        /*
         * Wires two Entities together
         * returns the IOSlots that were wired
         *
         * @param IOEntity source
         * @param Gates source_binding
         * @param string source_slot
         * @param IOEntity target
         * @param Gates target_binding
         * @param string target_slot
         * @return IOEntity.IOSlot[]
        */
        public IOEntity.IOSlot[] WireEntites(
            IPlayer player,
            IOEntity source,
            Gates source_binding,
            string source_slot, 
            IOEntity target,
            Gates target_binding,
            string target_slot,
            WireTool.WireColour wire_color = WireTool.WireColour.Default
        ) {
            // Get Indexes for IO Slots
            #if DEBUG_PINS
            player.Reply($"Source: {source_binding} | {source_slot}");
            player.Reply($"Target: {target_binding} | {target_slot}");
            #endif

            RustComponentDefinition sourceDefinition = io_definitions[source_binding == Gates.NOT ? Gates.XOR : source_binding][source_slot];
            RustComponentDefinition targetDefinition = io_definitions[target_binding == Gates.NOT ? Gates.XOR : target_binding][target_slot];

            byte sourceIndex = sourceDefinition.slotNumber;
            byte targetIndex = targetDefinition.slotNumber;

            // Define Input and Output Slots
            var inputSlot = target.inputs[targetIndex] ;
            var outputSlot = source.outputs[sourceIndex];

            // Setup Output Slot
            if (inputSlot.connectedTo == null)
                outputSlot.connectedTo = new IOEntity.IORef();
            
            inputSlot.connectedTo.Set(source);
            inputSlot.connectedToSlot = sourceIndex;
            inputSlot.connectedTo.Init();

            // Setup Output Slot
            if (outputSlot.connectedTo == null)
                outputSlot.connectedTo = new IOEntity.IORef();
            
            outputSlot.connectedTo.Set(target);
            outputSlot.connectedToSlot = targetIndex;
            outputSlot.connectedTo.Init(); 

            // Setup Wire - TODO FIXED THIS BROKEN SHIT (COULD BE FIXED SHIT NOW)
            outputSlot.wireColour = wire_color;
            outputSlot.type = IOEntity.IOType.Electric;

            var sourcePosition = source.transform.position;
            var targetPosition = target.transform.position;

            var lineList = new List<Vector3>(){
                Vector3.zero - sourceDefinition.offset,
                (targetPosition - sourcePosition) - targetDefinition.offset
            };
            outputSlot.linePoints = lineList.ToArray();

            // Update source and Target
            source.MarkDirtyForceUpdateOutputs();
            source.SendNetworkUpdate();
            target.MarkDirtyForceUpdateOutputs();
            target.SendNetworkUpdate();

            return new IOEntity.IOSlot[2]{inputSlot, outputSlot};
        }
    }

    // BEGIND DATATYPES FOR DE-SERIALIZATION
    class Chip : ParentHelpers {
        public string Name;
        public long ID;
        public HashSet<SubChip> SubChips = new HashSet<SubChip>();
        public HashSet<Pin> InputPins = new HashSet<Pin>();
        public HashSet<Pin> OutputPins = new HashSet<Pin>();
        public HashSet<Connection> Connections = new HashSet<Connection>();

        public Dictionary<long, Pin> DicInputPins = new Dictionary<long, Pin>();
        public Dictionary<long, Pin> DicOutputPins = new Dictionary<long, Pin>();

        public void Init(long newId)
        {
            ID = newId;

            foreach (Connection connection in Connections)
            {
                if (connection.Source.SubChipID == 0)
                    connection.Source.SubChipID = ID;
                if (connection.Target.SubChipID == 0)
                    connection.Target.SubChipID = ID;
            }

            foreach (Pin pin in InputPins)
            {
                DicInputPins.Add(pin.ID, pin);
            }
            foreach (Pin pin in OutputPins)
            {
                DicOutputPins.Add(pin.ID, pin);
            }
        }
    
        private IOEntity LocateConnectionEntity(
            IPlayer player,
            ConnectionIO connection, 
            Dictionary<string, IOEntity> pins,
            Dictionary<string, IOEntity> electricalComponents
        ) {
            #if DEBUG_PINS
            player.Reply($"PIN Identifier: {connection.SubChipID}_{connection.PinID}");
            if(pins.ContainsKey($"{connection.SubChipID}_{connection.PinID}"))
                player.Reply($"Found Pin {connection.PinID} in Pins");
            else if (electricalComponents.ContainsKey(connection.SubChipID.ToString()))
                player.Reply($"Found Pin {connection.PinID} in Electrical Components");
            else
                player.Reply($"Unable to find Pin {connection.PinID} in Pins or Electrical Components");
            #endif
            
            return pins.ContainsKey($"{connection.SubChipID}_{connection.PinID}")
                    ? pins[$"{connection.SubChipID}_{connection.PinID}"]
                    : electricalComponents[connection.SubChipID.ToString()];
        }

        private void Wire(
            JData thisInstance,
            IPlayer player,
            Project project_reference,
            Dictionary<string, Chip> chipDefinitions,
            Dictionary<string, IOEntity> pins,
            Dictionary<string, IOEntity> electricalComponents,
            int totalEntityCount
        )
        {
            #if DEBUG_PINS
            player.Reply($"PIN STACK: {String.Join(" | ", pins.Keys)}");
            #endif

            // Our list of connections to wire up, this takes into account duplicate pins, as well as their positions.
            HashSet<object[]> pinsToWire = new HashSet<object[]>();
            Dictionary<string, Vector3> duplicatePinPositions = new Dictionary<string, Vector3>();
            Dictionary<string, int> duplicatePins = new Dictionary<string, int>();

            // Iterate over this chips connections
            foreach (Connection connection in Connections) {
                // If we somehow wire something to itself, skip it    
                if (connection.Source.SubChipID == connection.Target.SubChipID)
                    continue;

                // Grab our entities for wiring
                var sourceEntity = LocateConnectionEntity(player, connection.Source, pins, electricalComponents);
                var targetEntity = LocateConnectionEntity(player, connection.Target, pins, electricalComponents);
                
                //Check if the source entity has connections to multiple components
                //We create an int and init it to 0 then add up for additional times we
                //See a source connected
                var duplicatePinId = $"{connection.Source.SubChipID.ToString()}_{connection.Source.PinID.ToString()}";
                if (!duplicatePins.ContainsKey(duplicatePinId)) {
                    duplicatePinPositions.Add(duplicatePinId, sourceEntity.transform.position);
                    duplicatePins.Add(duplicatePinId, 0);
                } else {
                    duplicatePins[duplicatePinId]++;
                }
        
                // BEGIN Locating Gate Types
                var sourceChipId = connection.Source.SubChipID.ToString();
                var sourcePinId  = connection.Source.PinID;
                var targetChipId = connection.Target.SubChipID.ToString();
                var targetPinId  = connection.Target.PinID;

                var sourceGate = connection.Source.SubChipID == ID
                                    ? Gates.OR
                                    : string_to_gates.ContainsKey(chipDefinitions[sourceChipId].Name)
                                        ? string_to_gates[chipDefinitions[sourceChipId].Name]
                                        : Gates.E_BRANCH;

                var targetGate = connection.Target.SubChipID == ID
                                    ? Gates.E_BRANCH
                                    : string_to_gates.ContainsKey(chipDefinitions[targetChipId].Name)
                                        ? string_to_gates[chipDefinitions[targetChipId].Name]
                                        : Gates.OR;
                // END Locating Gate Types

                // BEGIN Locating Gate Slots
                var sourceSlot = "";
                if (sourceGate == Gates.NOT || sourceGate == Gates.AND || (sourceGate == Gates.OR && chipDefinitions[sourceChipId].Name != "OR"))
                {
                    sourceSlot = "Power Out";
                }
                else if (sourceGate == Gates.E_BRANCH && chipDefinitions[sourceChipId].Name != "E_BRANCH")
                {
                    sourceSlot = "Power Out";
                }
                else
                {
                    sourceSlot = chipDefinitions[sourceChipId].DicOutputPins[sourcePinId].Name;
                }

                #if DEBUG_PINS
                player.Reply(chipDefinitions[targetChipId].Name);
                player.Reply(sourcePinId.ToString());
                player.Reply(prefab_gate_bindings[targetGate]);
                #endif

                var targetSlot = "";
                if (targetGate == Gates.NOT)
                {
                    targetSlot = "Input A";
                }
                else if (targetGate == Gates.AND)
                {
                    targetSlot = targetPinId == 0
                                    ? "Input A"
                                    : "Input B";
                }
                else if (targetGate == Gates.OR && chipDefinitions[targetChipId].Name != "OR")
                {
                    targetSlot = "Input A";
                }
                else if (targetGate == Gates.E_BRANCH && chipDefinitions[targetChipId].Name != "E_BRANCH")
                {
                    targetSlot = "Power In";
                }
                else
                {
                    targetSlot = chipDefinitions[targetChipId].DicInputPins[targetPinId].Name;
                }
                //END Locating Gate Slots

                // If a switch add a sign showing its output connection
                if (sourceGate == Gates.SIMPLE_SWITCH) {
                    var sourcePosition = sourceEntity.transform.position;
                    thisInstance.BindSaveSign(
                        new Vector3(sourcePosition.x - .7f, sourcePosition.y + .75f, sourcePosition.z),
                        new Quaternion(0, 0, 0, 0), //new Quaternion(0, 1, 0, 0),
                        Picasso.Signs.WoodenSmall,
                        128,
                        64,
                        17,
                        Picasso.FontSize.Small,
                        new Dictionary<string, Brush> {
                            {chipDefinitions[targetChipId].Name, Brushes.Black},
                            {chipDefinitions[targetChipId].DicInputPins[connection.Target.PinID].Name, Brushes.Orange}
                        },
                        System.Drawing.Color.Green
                    );
                }

                // Add definition be iterated on later
                pinsToWire.Add(
                    new object[8] {duplicatePinId, sourceEntity, sourceGate, sourceSlot, targetEntity, targetGate, targetSlot, connection.GetWireColor()}
                );
            }

            // Next spawn Splitters for duplicate pin connections.
            Dictionary<string, List<IOEntity>> duplicatePinsConnector = new Dictionary<string, List<IOEntity>>();
            foreach (KeyValuePair<string, int> pin in duplicatePins) {
                // If this pin had a single connection, skip it
                if (pin.Value == 0)
                    continue;

                // Position the Splitter next to our component
                Vector3 startPosition = new Vector3(duplicatePinPositions[pin.Key].x, duplicatePinPositions[pin.Key].y, duplicatePinPositions[pin.Key].z + 1);

                // If we have 2 connections, just spawn a single splitter
                if (pin.Value == 1) {
                    duplicatePinsConnector.Add(pin.Key, new List<IOEntity> {SpawnEntity(
                        prefab_gate_bindings[Gates.SPLITTER],
                        startPosition,
                        new Quaternion(0, 0, 0, 0) //new Quaternion(1, 1, 0, 0)
                    )as IOEntity});
                    totalEntityCount++;
                    
                    continue;
                }


                // If we get this far then we can assume we have more than 2 connections
                // We need to spawn a splitter for every 2 connections
                // We also need to wire the splitters together
                List<IOEntity> splitters = new List<IOEntity>();

                Vector3 multiSplitterPos =  new Vector3(startPosition.x, startPosition.y + 1, startPosition.z);
                // player.Reply($"Pin Count: {pin.Value}");
                // player.Reply($"double val: {(double) pin.Value / 2}");
                // player.Reply($"loop counter: { Math.Ceiling((double) pin.Value / 2)}");
                for (int i = 0; i <= (int) Math.Ceiling((double) pin.Value / 2); i++) {
                    multiSplitterPos = new Vector3(multiSplitterPos.x, multiSplitterPos.y, multiSplitterPos.z - 1);
                    splitters.Add(SpawnEntity(
                        prefab_gate_bindings[Gates.SPLITTER],
                        multiSplitterPos,
                        new Quaternion(0, 0, 0, 0) //new Quaternion(1, 1, 0, 0)
                    ) as IOEntity);

                    totalEntityCount++;

                    // Only self wire if on the second or greater iteration
                    if (i == 0)
                        continue;

                    WireEntites(
                        player,
                        splitters[i - 1],
                        Gates.SPLITTER,
                        "Power Out 3",
                        splitters[i],
                        Gates.SPLITTER,
                        "Power In"
                    );
                }

                duplicatePinsConnector.Add(pin.Key, splitters);
            }

            Dictionary<string, int> timesConnected = new Dictionary<string, int>();
            foreach (object[] wireInstructions in pinsToWire)
            {
                var duplicatePinId = wireInstructions[0] as string;

                if (duplicatePins.ContainsKey(duplicatePinId) && duplicatePins[duplicatePinId] > 0) {
                    WireEntites(
                        player,
                        wireInstructions[1] as IOEntity,
                        (Gates) wireInstructions[2],
                        wireInstructions[3] as string,
                        duplicatePinsConnector[duplicatePinId][0],
                        Gates.SPLITTER,
                        "Power In",
                        (WireTool.WireColour) wireInstructions[7]
                    );

                    if (! timesConnected.ContainsKey(duplicatePinId))
                        timesConnected.Add(duplicatePinId, 0);
                    else
                        timesConnected[duplicatePinId]++;

                    // Which Splitter in our Splitter Stack do we want to select for connection
                    int duplicatePinsConnectorIndex = 0;
                    if (timesConnected[duplicatePinId] >= 2) {
                        duplicatePinsConnectorIndex = (int) Math.Floor((double) timesConnected[duplicatePinId] / 2);
                    }

                    // Which pin on the splitter do we want to connect to
                    var splitterConnection = $"Power Out {timesConnected[duplicatePinId] + 1}";
                    if (timesConnected[duplicatePinId] >= 2) {
                        splitterConnection = timesConnected[duplicatePinId] % 2 == 0 ? "Power Out 1" : "Power Out 2";
                    }

                    // player.Reply($"Times Connected {timesConnected[duplicatePinId]}");
                    // player.Reply($"PinID {duplicatePinId}");
                    // player.Reply($"DupeCount {duplicatePinsConnector[duplicatePinId].Count}");
                    // player.Reply($"Current Index {duplicatePinsConnectorIndex}");

                    WireEntites(
                        player,
                        duplicatePinsConnector[duplicatePinId][duplicatePinsConnectorIndex],
                        Gates.SPLITTER,
                        splitterConnection,
                        wireInstructions[4] as IOEntity,
                        (Gates) wireInstructions[5],
                        wireInstructions[6] as string,
                        (WireTool.WireColour) wireInstructions[7]
                    );

                    continue;
                }

                WireEntites(
                    player,
                    wireInstructions[1] as IOEntity,
                    (Gates) wireInstructions[2],
                    wireInstructions[3] as string,
                    wireInstructions[4] as IOEntity,
                    (Gates) wireInstructions[5],
                    wireInstructions[6] as string,
                    (WireTool.WireColour) wireInstructions[7]
                );
            }
        }

        public object[] Build(IPlayer player, JData thisInstance, Project project_reference, Vector3 startPosition, Quaternion startRotation, bool depthShouldBeZOrY = true)
        {
            var customChipCount = 1;
            var chipDefinitions = new Dictionary<string, Chip>();
            var pins = BuildPins(startPosition, startRotation); // Our Pins
            int totalEntityCount = 0 + pins.Count;
            var electricalComponents = new Dictionary<string, IOEntity>(); // Every E Component we spawned for vanilla items
            chipDefinitions.Add(ID.ToString(), this);

            #if DEBUG
            player.Reply($"Building {Name} ({ID})");
            #endif

            thisInstance.BindSaveSign(
                new Vector3(startPosition.x + 2, startPosition.y + 5.5f, startPosition.z),
                new Quaternion(0, 0, 0, 0), //new Quaternion(0, 1, 0, 0),
                Picasso.Signs.WoodenSmall,
                128,
                64,
                17,
                Picasso.FontSize.Small,
                new Dictionary<string, Brush> {
                    {Name, Brushes.Yellow},
                    {ID.ToString(), Brushes.Orange}
                },
                System.Drawing.Color.Blue
            );

            // Build our SubChips for this Chip
            foreach (SubChip chip in SubChips) {
                // Add Chip Definition to our Dictionary
                if (! chipDefinitions.ContainsKey(chip.ID.ToString())) {
                    Chip chipDefinition = project_reference.ReadChip(chip.Name);
                    if (chipDefinition != null) {
                        chipDefinition.Init(chip.ID);
                        chipDefinitions.Add(chip.ID.ToString(), chipDefinition);
                    }
                }

                // Convert Chip Point into Vector3
                var chipPoint = new Dictionary<string, float>();
                foreach (Dictionary<string, float> point in chip.Points) {
                    chipPoint.Add("X", point["X"]);
                    chipPoint.Add("Y", point["Y"]);
                }
                
                // Adjust our Chip Position
                var localChipAdjustedPosition = new Vector3(startPosition.x - chipPoint["X"], startPosition.y + chipPoint["Y"], startPosition.z);

                // Is Custom Chip
                if (!string_to_gates.ContainsKey(chip.Name))
                {
                    thisInstance.BindSaveSign(
                        localChipAdjustedPosition,
                        new Quaternion(0, 0, 0, 0), //new Quaternion(0, 1, 0, 0),
                        Picasso.Signs.WoodenSmall,
                        128,
                        64,
                        17,
                        Picasso.FontSize.Small,
                        new Dictionary<string, Brush> {
                            {chip.Name, Brushes.Yellow},
                            {chip.ID.ToString(), Brushes.Orange}
                        },
                        System.Drawing.Color.Black
                    );

                    Vector3 nextSpot = new Vector3(startPosition.x, startPosition.y, startPosition.z - (15 * customChipCount));
                    if (!depthShouldBeZOrY) {
                        nextSpot = new Vector3(startPosition.x, startPosition.y + (15 * customChipCount), startPosition.z);
                    }
                    var subChipDefinition = chipDefinitions[chip.ID.ToString()];
                    var buildObjects = subChipDefinition.Build(player, thisInstance, project_reference, nextSpot, startRotation, !depthShouldBeZOrY);
                    totalEntityCount += (int) buildObjects[2];

                    // Add our SubChip Pins to our Pins List
                    foreach (KeyValuePair<string, IOEntity> entry in buildObjects[0] as Dictionary<string, IOEntity>) {
                        if (!pins.ContainsKey(entry.Key))
                            pins.Add(entry.Key, entry.Value);
                    }

                    Chip subChipInnerDefintion = buildObjects[1] as Chip;
                    if (!chipDefinitions.ContainsKey(subChipInnerDefintion.ID.ToString()))
                        chipDefinitions.Add(subChipInnerDefintion.ID.ToString(), subChipInnerDefintion);

                    customChipCount++;
                }
                // If NOT || AND (DSL reserved) we spawn them as vanilla items
                else if (string_to_gates[chip.Name] == Gates.AND || string_to_gates[chip.Name] == Gates.NOT) {
                    var chipDefinition = new Chip{
                        Name = chip.Name,
                        InputPins = new HashSet<Pin>(),
                        OutputPins = new HashSet<Pin>(),
                        Connections = new HashSet<Connection>()
                    };
                    chipDefinition.Init(chip.ID);
                    chipDefinitions.Add(chip.ID.ToString(), chipDefinition);
                    

                    if (string_to_gates[chip.Name] == Gates.NOT) {
                        // Spawn our Pin
                        electricalComponents.Add(chip.ID.ToString(), BuildNot(player, localChipAdjustedPosition, startRotation));
                        totalEntityCount += 2;
                    } else {
                        // Spawn our Pin
                        electricalComponents.Add(chip.ID.ToString(), SpawnEntity(
                            prefab_gate_bindings[Gates.AND],
                            localChipAdjustedPosition,
                            startRotation
                        ) as IOEntity);
                        totalEntityCount ++;
                    
                    }
                }
                // Normal Electrical Components
                else if (string_to_gates.ContainsKey(chip.Name)) {
                    electricalComponents.Add(chip.ID.ToString(), SpawnEntity(
                        prefab_gate_bindings[string_to_gates[chip.Name]],
                        localChipAdjustedPosition,
                        startRotation
                    ) as IOEntity);
                    totalEntityCount++;
                }
                // Build Custom Chips Recursively
                else {
                    throw new Exception($"Unable to handle chip {chip.Name} ({chip.ID})");
                }
            }

            Wire(thisInstance, player, project_reference, chipDefinitions, pins, electricalComponents, totalEntityCount);

            return new object[]{
                pins,
                chipDefinitions[ID.ToString()],
                totalEntityCount
            };
        }

        private IOEntity BuildNot(IPlayer player, Vector3 localChipAdjustedPosition, Quaternion startRotation)
        {
            var xor_switch = SpawnEntity(
                prefab_gate_bindings[Gates.XOR],
                localChipAdjustedPosition,
                startRotation
            ) as IOEntity;

            var test_generator = SpawnEntity(
                prefab_gate_bindings[Gates.TEST_GENERATOR],
                new Vector3(localChipAdjustedPosition.x, localChipAdjustedPosition.y, localChipAdjustedPosition.z - 3),
                startRotation
            ) as IOEntity;

            WireEntites(
                player,
                test_generator,
                Gates.TEST_GENERATOR,
                "Power Output 1",
                xor_switch,
                Gates.XOR,
                "Input B"
            );
    
            return xor_switch;
        }

        /**
         * Spawns Our Pins in the World
         *
         * @param string prefab
         * @param Vector3 pos
         * @param Quaternion tor
         * @return Dictionary<string, IOEntity>
         */
        public Dictionary<string, IOEntity> BuildPins(Vector3 startPosition, Quaternion startRotation) {

            var pinEntities = new Dictionary<string, IOEntity>();

            // Iterate over our Pin List
            foreach (Pin pin in OutputPins) {
                // Spawn our Pin
                pinEntities.Add($"{ID}_{pin.ID}", SpawnEntity(
                    prefab_gate_bindings[Gates.E_BRANCH],
                    new Vector3(startPosition.x - 8, startPosition.y + pin.PositionY, startPosition.z),
                    startRotation
                ) as IOEntity);
            }

            foreach (Pin pin in InputPins) {
                // Spawn our Pin
                pinEntities.Add($"{ID}_{pin.ID}", SpawnEntity(
                    prefab_gate_bindings[Gates.OR],
                    new Vector3(startPosition.x + 10, startPosition.y + pin.PositionY, startPosition.z),
                    startRotation
                ) as IOEntity);
            }

            return pinEntities;
        }

        public string ToString()
        {
            string message = $"{Name} \n SubChips:\n";
            foreach (SubChip chip in SubChips) {
                message += $"{chip.ToString()}\n";
            }

            message += "Connections\n";
            foreach (Connection connection in Connections) {
                message += $"{connection.ToString()}\n";
            }

            return message;
        }
    }

    class Pin {
        public string Name;
        public long ID;

        public float PositionY;

        public string ToString()
        {
            return $"Pin ({ID}) - {Name}";
        }
    }

    class ConnectionIO {
        public long PinID;
        public long PinType;
        public long SubChipID;

        public string ToString()
        {
            return $"Chip: {SubChipID} \n Pin: {PinID}";
        }
    }

    class Connection {
        public ConnectionIO Source;
        public ConnectionIO Target;
        public string ColourThemeName;

        public string ToString()
        {
            return $"Source\n {Source.ToString()} \n Target\n {Target.ToString()}";
        }

        public WireTool.WireColour GetWireColor()
        {
            switch (ColourThemeName)
            {
                case "Red":
                    return WireTool.WireColour.Red;
                case "Yellow":
                    return WireTool.WireColour.Yellow;
                case "Green":
                    return WireTool.WireColour.Green;
                case "Blue":
                    return WireTool.WireColour.Blue;
                case "Indigo":
                    return WireTool.WireColour.Purple;
                default:
                    return WireTool.WireColour.Default;
            }
        }
    }

    class SubChip {
        public string Name;

        public long ID;

        public HashSet<Dictionary<string, float>> Points;

        public string ToString()
        {
            return $"{Name} ({ID})";
        }

        public Gates GetGate()
        {
            var dic = new Dictionary<string, Gates> {
                { "SPLITTER", Gates.SPLITTER },
                { "BLOCKER", Gates.BLOCKER },
                { "MEMORY_CELL", Gates.MEMORY_CELL },
                { "E_BRANCH", Gates.E_BRANCH },
                { "AND", Gates.AND },
                { "OR", Gates.OR },
                { "XOR", Gates.XOR },
                { "TEST_GENERATOR", Gates.TEST_GENERATOR },
                { "SWITCH", Gates.SIMPLE_SWITCH },
                { "SMART_SWITCH", Gates.SMART_SWITCH },
                { "G_LIGHT", Gates.GREEN_LIGHT },
                { "R_LIGHT", Gates.RED_LIGHT },
                { "W_LIGHT", Gates.WHITE_LIGHT }
            };

            return dic[Name];
        }
    }

    class Project : ParentHelpers {
        public string ProjectName;
        public HashSet<string> AllCreatedChips = new HashSet<string>();

        public DataFileSystem dataFileSystem = new DataFileSystem($"{Interface.Oxide.DataDirectory}\\rust_circuit_boss");

        public Chip ReadChip(string chip_name)
        {
            return dataFileSystem.ExistsDatafile($"Chips/{chip_name}")
                    ? dataFileSystem.ReadObject<Chip>($"Chips/{chip_name}")
                    : null;
        }
    }
    // END DATATYPES FOR DE-SERIALIZATION
}