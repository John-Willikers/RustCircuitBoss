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
            chip.Build(player, this, loaded_project, new Vector3(position.X, position.Y, position.Z), new Quaternion(1, 1, 0, 0));
        }

        [Command("c_clear")]
         private void ProjectClearCommand(IPlayer player, string command, string[] args)
        {   
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
            { "SIMPLE_SWITCH", Gates.SIMPLE_SWITCH },
            { "SMART_SWITCH", Gates.SMART_SWITCH },
            { "GREEN_LIGHT", Gates.GREEN_LIGHT },
            { "RED_LIGHT", Gates.RED_LIGHT },
            { "WHITE_LIGHT", Gates.WHITE_LIGHT },
            { "NOT", Gates.NOT },
            { "TIMER", Gates.TIMER }
        };
        // IO Definitions
        public Dictionary<Gates, Dictionary<string,int>> io_definitions = new Dictionary<Gates, Dictionary<string, int>>
        {
            {Gates.TEST_GENERATOR, new Dictionary<string, int>
                {
                    {"Power Output 1", 0 },
                    {"Power Output 2", 1 },
                    {"Power Output 3", 2 }
                }
            },
            {Gates.AND, new Dictionary<string, int>
                {
                    {"Input A", 0 },
                    {"Input B", 1 },
                    {"Power Out", 0 }
                }
            },
            {Gates.XOR, new Dictionary<string, int>
                {
                    {"Input A", 0 },
                    {"Input B", 1 },
                    {"Power Out", 0 }
                }
            },
            {Gates.OR, new Dictionary<string, int>
                {
                    {"Input A", 0 },
                    {"Input B", 1 },
                    {"Power Out", 0 }
                }
            },
            {Gates.MEMORY_CELL, new Dictionary<string, int>
                {
                    {"Power In", 0 },
                    {"SET", 1 },
                    {"RESET", 2 },
                    {"TOGGLE", 3 },
                    {"Out", 0 },
                    {"Inverted_Out", 1 }
                }
            },
            {Gates.BLOCKER, new Dictionary<string, int>
                {
                    {"Power In", 0 },
                    {"Block Pass", 1 },
                    {"Power Out", 0 }
                }
            },
            {Gates.SPLITTER, new Dictionary<string, int>
                {
                    {"Power In", 0 },
                    {"Power Out 1", 0 },
                    {"Power Out 2", 1 },
                    {"Power Out 3", 2 }
                }
            },
            {Gates.E_BRANCH, new Dictionary<string, int>
                {
                    {"Power Out", 0 },
                    {"Branch Out", 1 },
                    {"Power In", 0 }
                }
            },
            {Gates.SIMPLE_SWITCH, new Dictionary<string, int>
                {
                    {"Electric Input", 0 },
                    {"Output", 0 }
                }
            },
            {Gates.SMART_SWITCH, new Dictionary<string, int>
                {
                    {"Electric Input", 0 },
                    {"Output", 0 }
                }
            },
            {Gates.GREEN_LIGHT, new Dictionary<string, int>
                {
                    {"Power In", 0 },
                    {"Passthrough", 0 }
                }
            },
            {Gates.RED_LIGHT, new Dictionary<string, int>
                {
                    {"Power In", 0 },
                    {"Passthrough", 0 }
                }
            },
            {Gates.WHITE_LIGHT, new Dictionary<string, int>
                {
                    {"Power In", 0 },
                    {"Passthrough", 0 }
                }
            },
            {Gates.TIMER, new Dictionary<string, int>
                {
                    {"Electric Input", 0 },
                    {"Toggle On", 1 },
                    {"Output", 0}
                }
            },
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
                eBranch.branchAmount = 10000;

            var testGen = entity as ElectricGenerator;
            if (testGen != null)
                testGen.electricAmount = 1000000000;

            var timerSwitch = entity as TimerSwitch;
            if (timerSwitch != null)
            {
                timerSwitch.timerLength = 2;
            }

            entity.Spawn();

            return entity;
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
            Dictionary<string, IOEntity> electricalComponents
        )
        {
            #if DEBUG_PINS
            player.Reply($"PIN STACK: {String.Join(" | ", pins.Keys)}");
            #endif

            HashSet<object[]> pinsToWire = new HashSet<object[]>();
            Dictionary<string, Vector3> duplicatePinPositions = new Dictionary<string, Vector3>();
            Dictionary<string, int> duplicatePins = new Dictionary<string, int>();

            foreach (Connection connection in Connections) {    
                var sourceEntity = LocateConnectionEntity(player, connection.Source, pins, electricalComponents);
                var targetEntity = LocateConnectionEntity(player, connection.Target, pins, electricalComponents);
                var duplicatePinId = $"{connection.Source.SubChipID.ToString()}_{connection.Source.PinID.ToString()}";

                if (!duplicatePins.ContainsKey(duplicatePinId)) {
                    duplicatePinPositions.Add(duplicatePinId, sourceEntity.transform.position);
                    duplicatePins.Add(duplicatePinId, 0);
                } else {
                    duplicatePins[duplicatePinId]++;
                }
        
                var sourceChipId = connection.Source.SubChipID.ToString();
                var sourcePinId  = connection.Source.PinID;
                var targetChipId = connection.Target.SubChipID.ToString();
                var targetPinId  = connection.Target.PinID;

                var sourceGate = connection.Source.SubChipID == ID
                                    ? Gates.OR
                                    : string_to_gates.ContainsKey(chipDefinitions[sourceChipId].Name)
                                        ? string_to_gates[chipDefinitions[sourceChipId].Name]
                                        : Gates.SPLITTER;

                var targetGate = connection.Target.SubChipID == ID
                                    ? Gates.SPLITTER
                                    : string_to_gates.ContainsKey(chipDefinitions[targetChipId].Name)
                                        ? string_to_gates[chipDefinitions[targetChipId].Name]
                                        : Gates.OR;

                var sourceSlot = "";
                if (sourceGate == Gates.NOT || sourceGate == Gates.AND || (sourceGate == Gates.OR && chipDefinitions[sourceChipId].Name != "OR"))
                {
                    sourceSlot = "Power Out";
                }
                else if (sourceGate == Gates.SPLITTER && chipDefinitions[sourceChipId].Name != "SPLITTER")
                {
                    sourceSlot = "Power Out 1";
                }
                else
                {
                    sourceSlot = chipDefinitions[sourceChipId].DicOutputPins[sourcePinId].Name;
                }

                if (sourceGate == Gates.SIMPLE_SWITCH) {
                    var sourcePosition = sourceEntity.transform.position;
                    thisInstance.BindSaveSign(
                        new Vector3(sourcePosition.x + 2, sourcePosition.y, sourcePosition.z),
                        new Quaternion(0, 1, 0, 0),
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
                else if (targetGate == Gates.SPLITTER && chipDefinitions[targetChipId].Name != "SPLITTER")
                {
                    targetSlot = "Power In";
                }
                else
                {
                    targetSlot = chipDefinitions[targetChipId].DicInputPins[targetPinId].Name;
                }

                pinsToWire.Add(
                    new object[7] {duplicatePinId, sourceEntity, sourceGate, sourceSlot, targetEntity, targetGate, targetSlot}
                );
            }

            Dictionary<string, List<IOEntity>> duplicatePinsConnector = new Dictionary<string, List<IOEntity>>();

            foreach (KeyValuePair<string, int> pin in duplicatePins) {
                    if (pin.Value == 0)
                        continue;

                    Vector3 startPosition = new Vector3(duplicatePinPositions[pin.Key].x + 1, duplicatePinPositions[pin.Key].y, duplicatePinPositions[pin.Key].z);

                    if (pin.Value == 1) {
                        duplicatePinsConnector.Add(pin.Key, new List<IOEntity> {SpawnEntity(
                            prefab_gate_bindings[Gates.SPLITTER],
                            startPosition,
                            new Quaternion(1, 1, 0, 0)
                        
                        )as IOEntity});
                        
                        continue;
                    }

                    List<IOEntity> splitters = new List<IOEntity>();

                    Vector3 multiSplitterPos =  new Vector3(startPosition.x, startPosition.y + 1, startPosition.z);
                    for (int i = 1; i <= Math.Ceiling((double) pin.Value / 2); i++) {
                        multiSplitterPos = new Vector3(multiSplitterPos.x, multiSplitterPos.y - 1, multiSplitterPos.z);
                        splitters.Add(SpawnEntity(
                            prefab_gate_bindings[Gates.SPLITTER],
                            multiSplitterPos,
                            new Quaternion(1, 1, 0, 0)
                        ) as IOEntity);

                        if (i > 1) {
                            WireEntites(
                                player,
                                splitters[i - 2],
                                Gates.SPLITTER,
                                "Power Out 3",
                                splitters[i - 1],
                                Gates.SPLITTER,
                                "Power In"
                            );
                        }
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
                        "Power In"
                    );

                    if (! timesConnected.ContainsKey(duplicatePinId))
                        timesConnected.Add(duplicatePinId, 2);
                    else
                        timesConnected[duplicatePinId]++;

                    int duplicatePinsConnectorIndex = timesConnected[duplicatePinId] / 2  == 1 
                                                        ? 0
                                                        : (int) (Math.Floor((float) (timesConnected[duplicatePinId] / 2)) - 1);

                    if (duplicatePinsConnector[duplicatePinId].Count == 1)
                        duplicatePinsConnectorIndex = 0;

                    WireEntites(
                        player,
                        duplicatePinsConnector[duplicatePinId][duplicatePinsConnectorIndex],
                        Gates.SPLITTER,
                        timesConnected[duplicatePinId] % 2 == 0 ? "Power Out 1" : "Power Out 2",
                        wireInstructions[4] as IOEntity,
                        (Gates) wireInstructions[5],
                        wireInstructions[6] as string
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
                    wireInstructions[6] as string
                );
            }
        }

        public object[] Build(IPlayer player, JData thisInstance, Project project_reference, Vector3 startPosition, Quaternion startRotation, bool depthShouldBeZOrY = true)
        {
            var customChipCount = 1;
            var chipDefinitions = new Dictionary<string, Chip>();
            var pins = BuildPins(startPosition, startRotation); // Our Pins
            var electricalComponents = new Dictionary<string, IOEntity>(); // Every E Component we spawned for vanilla items
            chipDefinitions.Add(ID.ToString(), this);

            #if DEBUG
            player.Reply($"Building {Name} ({ID})");
            #endif

            thisInstance.BindSaveSign(
                new Vector3(startPosition.x + 2, startPosition.y + 5.5f, startPosition.z),
                new Quaternion(0, 1, 0, 0),
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
                var localChipAdjustedPosition = new Vector3(startPosition.x + chipPoint["X"], startPosition.y + chipPoint["Y"], startPosition.z);

                // Is Custom Chip
                if (!string_to_gates.ContainsKey(chip.Name))
                {
                    thisInstance.BindSaveSign(
                        localChipAdjustedPosition,
                        new Quaternion(0, 1, 0, 0),
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

                    Vector3 nextSpot = new Vector3(startPosition.x, startPosition.y, startPosition.z + (15 * customChipCount));
                    if (!depthShouldBeZOrY) {
                        nextSpot = new Vector3(startPosition.x, startPosition.y + (15 * customChipCount), startPosition.z);
                    }
                    var subChipDefinition = chipDefinitions[chip.ID.ToString()];
                    var buildObjects = subChipDefinition.Build(player, thisInstance, project_reference, nextSpot, startRotation, !depthShouldBeZOrY);

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
                    } else {
                        // Spawn our Pin
                        electricalComponents.Add(chip.ID.ToString(), SpawnEntity(
                            prefab_gate_bindings[Gates.AND],
                            localChipAdjustedPosition,
                            startRotation
                        ) as IOEntity);
                    
                    }
                }
                // Normal Electrical Components
                else if (string_to_gates.ContainsKey(chip.Name)) {
                    electricalComponents.Add(chip.ID.ToString(), SpawnEntity(
                        prefab_gate_bindings[string_to_gates[chip.Name]],
                        localChipAdjustedPosition,
                        startRotation
                    ) as IOEntity);
                }
                // Build Custom Chips Recursively
                else {
                    throw new Exception($"Unable to handle chip {chip.Name} ({chip.ID})");
                }
            }

            Wire(thisInstance, player, project_reference, chipDefinitions, pins, electricalComponents);

            return new object[]{
                pins,
                chipDefinitions[ID.ToString()]
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
                new Vector3(localChipAdjustedPosition.x, localChipAdjustedPosition.y, localChipAdjustedPosition.z + 3),
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
                    prefab_gate_bindings[Gates.SPLITTER],
                    new Vector3(startPosition.x + 8, startPosition.y + pin.PositionY, startPosition.z),
                    startRotation
                ) as IOEntity);
            }

            foreach (Pin pin in InputPins) {
                // Spawn our Pin
                pinEntities.Add($"{ID}_{pin.ID}", SpawnEntity(
                    prefab_gate_bindings[Gates.OR],
                    new Vector3(startPosition.x - 10, startPosition.y + pin.PositionY, startPosition.z),
                    startRotation
                ) as IOEntity);
            }

            return pinEntities;
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
            string target_slot
        ) {
            // Get Indexes for IO Slots
            #if DEBUG_PINS
            player.Reply($"Source: {source_binding} | {source_slot}");
            player.Reply($"Target: {target_binding} | {target_slot}");
            #endif

            int sourceIndex = io_definitions[source_binding == Gates.NOT ? Gates.XOR : source_binding][source_slot];
            int targetIndex = io_definitions[target_binding == Gates.NOT ? Gates.XOR : target_binding][target_slot];

            // Define Input and Output Slots
            var inputSlot = target.inputs[targetIndex] ;
            var outputSlot = source.outputs[sourceIndex];

            // Setup Input Slot
            if (inputSlot.connectedTo == null)
                inputSlot.connectedTo = new IOEntity.IORef();
            
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
            outputSlot.wireColour = WireTool.WireColour.Default;
            outputSlot.type = IOEntity.IOType.Electric;

            var lineList = new List<Vector3>(){
                source.transform.position,
                target.transform.position
            };
            outputSlot.linePoints = lineList.ToArray();

            // Update source and Target
            source.MarkDirtyForceUpdateOutputs();
            source.SendNetworkUpdate();
            // target.SendNetworkUpdate(); - Might be redundant

            return new IOEntity.IOSlot[2]{inputSlot, outputSlot};
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

        public string ToString()
        {
            return $"Source\n {Source.ToString()} \n Target\n {Target.ToString()}";
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
                { "SIMPLE_SWITCH", Gates.SIMPLE_SWITCH },
                { "SMART_SWITCH", Gates.SMART_SWITCH },
                { "GREEN_LIGHT", Gates.GREEN_LIGHT },
                { "RED_LIGHT", Gates.RED_LIGHT },
                { "WHITE_LIGHT", Gates.WHITE_LIGHT }
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