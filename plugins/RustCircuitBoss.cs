#define DEBUG
using System.Collections.Generic;
using System;
using System.Drawing;
using System.IO;
using static System.Random;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Facepunch;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Oxide.Game.Rust.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Rust Circuit Boss", "John Willikers", "0.1.0")]
    [Description("Takes Project files from Digital Logic Sim and generates them in game")]
    class RustCircuitBoss : CovalencePlugin
    {
        private ProjectReader project_reader;
        private Dictionary<string, Project> loaded_projects = new Dictionary<string, Project>();
        // private Dictionary<string, Chip>
        private Dictionary<string, string> gate_bindings = new Dictionary<string, string>{
            {"SPLITTER", "assets/prefabs/deployable/playerioents/splitter/splitter.prefab"},
            {"BLOCKER", "assets/prefabs/deployable/playerioents/gates/blocker/electrical.blocker.deployed.prefab"},
            {"MEMORY_CELL", "assets/prefabs/deployable/playerioents/gates/dflipflop/electrical.memorycell.deployed.prefab"},
            {"E_BRANCH", "assets/prefabs/deployable/playerioents/gates/branch/electrical.branch.deployed.prefab"},
            {"AND", "assets/prefabs/deployable/playerioents/gates/andswitch/andswitch.entity.prefab"},
            {"OR", "assets/prefabs/deployable/playerioents/gates/orswitch/orswitch.entity.prefab"},
            {"XOR", "assets/prefabs/deployable/playerioents/gates/xorswitch/xorswitch.entity.prefab"},
            {"TEST_GENERATOR", "assets/prefabs/deployable/playerioents/generators/generator.small.prefab"},
            {"SIMPLE_SWITCH", "assets/prefabs/deployable/playerioents/simpleswitch/switch.prefab"},
            {"SMART_SWITCH", "assets/prefabs/deployable/playerioents/app/smartswitch/smartswitch.prefab"},
            {"GREEN_LIGHT", "assets/prefabs/misc/permstore/industriallight/industrial.wall.lamp.green.deployed.prefab"},
            {"RED_LIGHT", "assets/prefabs/misc/permstore/industriallight/industrial.wall.lamp.red.deployed.prefab"},
            {"WHITE_LIGHT", "assets/prefabs/misc/permstore/industriallight/industrial.wall.lamp.deployed.prefab"}
        };

        private Dictionary<string, string> sign_bindings  = new Dictionary<string, string>{
            {"SMALL_WOOD_SIGN", "assets/prefabs/deployable/signs/sign.small.wood.prefab"}
        };

        private void Init()
        {
            Puts("A baby plugin is born!");
            project_reader = new ProjectReader(new DataFileSystem($"{Interface.Oxide.DataDirectory}\\rust_circuit_boss"));
            #if DEBUG
                Puts("DEBUG ENABLED - loading test project to Ten'a Account");
                loaded_projects.Add("76561198064426107", project_reader.ReadProject("Test"));
            #endif
        }

        #if DEBUG
        // BEGIN TEST FUNCS 
        public void BobRoss(IPlayer player)
        {
            GenericPosition position = player.Position();

            Signage my_sign = SpawnEntity(
                sign_bindings["SMALL_WOOD_SIGN"],
                new Vector3(position.X, position.Y, position.Z+1),
                new Quaternion(1, 1, 1, 1)
            ) as Signage;

            var lines = new Dictionary<string, Brush>
            {
                {"Hello World", Brushes.Yellow },
                {"Oh No", Brushes.Orange}
            };

            var imageBytes = Picasso.DrawSign(128, 64, 30, 10, lines);

            player.Reply($"Image Bytes: {imageBytes.Length} - {imageBytes.ToString()}");

            my_sign.textureIDs[0] = FileStorage.server.Store(imageBytes, FileStorage.Type.png, my_sign.net.ID);
            my_sign.SendNetworkUpdate();
        }

        public void GetSlots(IPlayer player)
        {
            GenericPosition position = player.Position();

            var splitter = SpawnEntity(
                gate_bindings["SPLITTER"],
                new Vector3(position.X, position.Y, position.Z),
                new Quaternion(1, 1, 1, 1)
            ) as IOEntity;

            var e_branch = SpawnEntity(
                gate_bindings["E_BRANCH"],
                new Vector3(position.X+1, position.Y+1, position.Z+1),
                new Quaternion(1, 1, 1, 1)
            ) as IOEntity;

            // Get Indexes for IO Slots
            int sourceIndex = IODefinitions.slots["SPLITTER"]["Power Out 1"];
            int targetIndex = IODefinitions.slots["E_BRANCH"]["Power In"];

            // Define Input and Output Slots
            var inputSlot = e_branch.inputs[targetIndex];
            var outputSlot =  splitter.outputs[sourceIndex];

            // Setup Input Slot
            if (inputSlot.connectedTo == null) 
                inputSlot.connectedTo = new IOEntity.IORef();

            inputSlot.connectedTo.Set(splitter);
            inputSlot.connectedToSlot = sourceIndex;
            inputSlot.connectedTo.Init();

            // Setup Output Slot
            if (outputSlot.connectedTo == null)
                outputSlot.connectedTo = new IOEntity.IORef();

            outputSlot.connectedTo.Set(e_branch);
            outputSlot.connectedToSlot = targetIndex;
            outputSlot.connectedTo.Init(); 

            // Setup Wire - TODO FIXED THIS BROKEN SHIT
            outputSlot.wireColour = (WireTool.WireColour)3;
            outputSlot.type = (IOEntity.IOType)0;
            var lineList = new List<Vector3>();
            lineList.Add(splitter.transform.position);
            lineList.Add(e_branch.transform.position);
            splitter.outputs[sourceIndex].linePoints = lineList.ToArray();

            // Update source and Target
            splitter.MarkDirtyForceUpdateOutputs();
            splitter.SendNetworkUpdate();
            e_branch.SendNetworkUpdate();
        }

        public void Compass(IPlayer player)
        {
            GenericPosition position = player.Position();

            SpawnEntity(
                gate_bindings["SPLITTER"],
                new Vector3(position.X, position.Y, position.Z+1),
                new Quaternion(1, 1, 1, 1)
            );
        }
        // END TEST FUNCS
        #endif

        // BEGIN SPAWN LOGIC
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

            entity.Spawn();

            return entity;
        }

        /**
         * Wire two entities together
         * 
         * @param IOEntity source
         * @param string source_binding
         * @param string source_slot
         * @param IOEntity target
         * @param string target_binding
         * @param string target_slot
         */
        public IOEntity.IOSlot[] wireEntities(
            IOEntity source,
            string source_binding,
            string source_slot, 
            IOEntity target,
            string target_binding,
            string target_slot
        ) {
            // Get Indexes for IO Slots
            int sourceIndex = IODefinitions.slots[source_binding][source_slot];
            int targetIndex = IODefinitions.slots[target_binding][target_slot];

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
            // outputSlot.wireColour = (WireTool.WireColour)3;
            // outputSlot.type = (IOEntity.IOType)0;
            // var lineList = new List<Vector3>();
            // lineList.Add(sourceEntity.transform.position);
            // lineList.Add(targetEntity.transform.position);
            // source.outputs[sourceIndex].linePoints = lineList.ToArray();

            // Update source and Target
            source.MarkDirtyForceUpdateOutputs();
            source.SendNetworkUpdate();
            target.SendNetworkUpdate();

            return new IOEntity.IOSlot[2]{inputSlot, outputSlot};
        }

        // Change sig to return HashSet of IO for chip (most of the time splitters in the beginning and end)
        public void BuildChip(
            IPlayer player,
            Project project,
            Chip chip,
            Dictionary<string, Chip> custom_chips,
            Dictionary<string, IOEntity> pin_connections,
            Dictionary<string, Chip> loaded_chips,
            Dictionary<string, Pin> loaded_pins,
            int offset,
            long subChipId = 0
        ) {
            Dictionary<string, IOEntity> e_components = new Dictionary<string, IOEntity>();
            GenericPosition position = player.Position();
            if (!custom_chips.ContainsKey(chip.Name))
                custom_chips.Add(chip.Name, chip);

            player.Reply($"Chip: {chip.Name}");

            // Spawn IO we, logic is the same between I and O besides prefab and location
            // So we just use an int to tell which kind we are iterating over.
            for (int cS = 0; cS < 2; cS++)
            {
                foreach (Pin pin in cS == 0 ? chip.InputPins : chip.OutputPins )
                {
                    #if DEBUG
                        // string pinType = cS == 0 ? "inputPin" : "outputPin";
                        // player.Reply($"Built {pinType}");
                    #endif

                    float posX = cS == 0 ? position.X - 10 : position.X + 10;

                    IOEntity spawned_entity = SpawnEntity(
                        gate_bindings[cS == 0 ? "OR" : "SPLITTER"],
                        new Vector3(posX, position.Y + pin.PositionY * 2, position.Z + offset),
                        new Quaternion(1, 1, 1, 1)
                    ) as IOEntity;

                    if (!pin_connections.ContainsKey($"{subChipId}_{pin.ID.ToString()}"))
                        pin_connections.Add($"{subChipId}_{pin.ID.ToString()}", spawned_entity);
                    if (!loaded_pins.ContainsKey(pin.ID.ToString()))
                        loaded_pins.Add(pin.ID.ToString(), pin);
                }
            }

            // Iterate over internal Chips
            foreach (SubChip subChip in chip.SubChips)
            {
                #if DEBUG
                    // Bycey Boy
                    // player.Reply($"<color=#ff0000ff>subChip: {subChip.Name}</color>");
                    // player.Reply($"subChip: {subChip.Name}");
                #endif
                // Load a Chip into memory as long as its not binded to a elec. item
                if (!loaded_chips.ContainsKey(subChip.ID.ToString())) {
                    if (subChip.Name == "NOT" || subChip.Name == "AND") {
                        loaded_chips.Add(subChip.ID.ToString(), new Chip{
                            Name = subChip.Name,
                            InputPins = new HashSet<Pin>(),
                            OutputPins = new HashSet<Pin>(),
                            Connections = new HashSet<Connection>()
                        });
                        goto SkipNormalChipLoad;
                    }


                    Chip chip_to_load = project_reader.ReadChip(project, subChip.Name);
                    if (chip_to_load == null)
                        goto SkipNormalChipLoad;

                    foreach (Pin pin in chip_to_load.InputPins)
                    {
                        if (loaded_pins.ContainsKey(pin.ID.ToString()))
                            continue;
                        loaded_pins.Add(pin.ID.ToString(), pin);
                    }

                    foreach (Pin pin in chip_to_load.OutputPins)
                    {
                        if (loaded_pins.ContainsKey(pin.ID.ToString()))
                            continue;
                        loaded_pins.Add(pin.ID.ToString(), pin);
                    }
                    loaded_chips.Add(subChip.ID.ToString(), chip_to_load);
                }

                SkipNormalChipLoad:

                float subChipXOffset = 0f;
                float subChipYOffset = 0f;
                
                foreach (Dictionary<string, float> point in subChip.Points)
                {
                    subChipXOffset = point["X"];
                    subChipYOffset = point["Y"];
                }

                if (subChip.Name == "NOT") {
                    // Bonus Points, figure out how to set Input B as hot regardless of input
                    IOEntity xor_switch = SpawnEntity(
                        gate_bindings["XOR"],
                        new Vector3(position.X + subChipXOffset, position.Y + subChipYOffset, position.Z + offset),
                        new Quaternion(1, 1, 1, 1)
                    ) as IOEntity;

                    IOEntity test_generator = SpawnEntity(
                        gate_bindings["TEST_GENERATOR"],
                        new Vector3(position.X + subChipXOffset, position.Y + subChipYOffset, position.Z + offset),
                        new Quaternion(1, 1, 1, 1)
                    ) as IOEntity;
                    
                    wireEntities(
                        test_generator,
                        "TEST_GENERATOR",
                        "Power Output 1",
                        xor_switch,
                        "XOR",
                        "Input B"
                    );

                    e_components.Add(
                        subChip.ID.ToString(),
                        xor_switch
                    );
                }
                // If Elec. Item spawn as is
                else if (gate_bindings.ContainsKey(subChip.Name)) {
                    e_components.Add(
                        subChip.ID.ToString(),
                        SpawnEntity(
                            gate_bindings[subChip.Name],
                            new Vector3(position.X + subChipXOffset, position.Y + subChipYOffset, position.Z + offset),
                            new Quaternion(1, 1, 1, 1)
                        ) as IOEntity
                    );
                // If a non Elec. Item we probably want recursively build it so we can put Elec. items down
                } else {
                    offset += 5;
                    BuildChip(player, project, loaded_chips[subChip.ID.ToString()], custom_chips, pin_connections, loaded_chips, loaded_pins, offset, subChip.ID);
                }

                // INSERT MAKE SIGN
                Signage switch_sign = SpawnEntity(
                    sign_bindings["SMALL_WOOD_SIGN"],
                    new Vector3(position.X, position.Y, position.Z + offset),
                    new Quaternion(1, 1, 1, 1)
                ) as Signage;

                var lines = new Dictionary<string, Brush>
                {
                    {chip.Name, Brushes.Yellow},
                };

                var imageData = Picasso.DrawSign(128, 64, 17, 16, lines);

                switch_sign.textureIDs[0] = FileStorage.server.Store(imageData, FileStorage.Type.png, switch_sign.net.ID);
                switch_sign.SendNetworkUpdate();
            }

            foreach(Connection connection in chip.Connections)
            {
                Puts($"Source ChipID: {connection.Source.SubChipID}");
                Puts($"Source ChipName: {loaded_chips[connection.Source.SubChipID.ToString()].Name}");
                Puts($"Source PinID: {connection.Source.PinID}");
                Puts($"Target ChipID: {connection.Target.SubChipID}");
                Puts($"Target ChipName: {loaded_chips[connection.Target.SubChipID.ToString()].Name}");
                Puts($"Target PinID: {connection.Target.PinID}");

                Puts($"Custom Chips: {string.Join(", ", custom_chips.Keys)}");
                Puts($"E Components: {string.Join(", ", e_components.Keys)}");
                Puts($"Pin Connections: {string.Join(", ", pin_connections.Keys)}");
                Puts($"Sub Chips: {string.Join(", ", loaded_chips.Keys)}");
                Puts($"Pins: {string.Join(", ", loaded_pins.Keys)}");

                Puts(loaded_chips[connection.Target.SubChipID.ToString()].Name);

                Puts("HIT BB 0");

                // Get our Connection Source and Target
                IOEntity sourceEntity = (connection.Source.SubChipID > 0 && !custom_chips.ContainsKey(loaded_chips[connection.Source.SubChipID.ToString()].Name))
                                            ? e_components[connection.Source.SubChipID.ToString()]
                                            : pin_connections[$"{connection.Source.SubChipID.ToString()}_{connection.Source.PinID.ToString()}"];

                IOEntity targetEntity = (connection.Target.SubChipID > 0 && !custom_chips.ContainsKey(loaded_chips[connection.Target.SubChipID.ToString()].Name))
                                            ? e_components[connection.Target.SubChipID.ToString()]
                                            : pin_connections[$"{connection.Target.SubChipID.ToString()}_{connection.Target.PinID.ToString()}"];


                Puts("HIT BB");

                string sourceType = "";
                string targetType = "";
                string sourceSlot = "";
                string targetSlot = "";

                // NOT Gates
                if (loaded_chips[connection.Source.SubChipID.ToString()].Name == "NOT")
                {
                    sourceType = "XOR";
                    sourceSlot = "Power Out";
                }
                // And Gates
                else if (loaded_chips[connection.Source.SubChipID.ToString()].Name == "AND")
                {
                    sourceType = "AND";
                    sourceSlot = "Power Out";
                }
                // Other Rust Elec. Items
                else if (connection.Source.SubChipID > 0 && !custom_chips.ContainsKey(loaded_chips[connection.Source.SubChipID.ToString()].Name))
                {
                    sourceType = loaded_chips[connection.Source.SubChipID.ToString()].Name;
                    sourceSlot = loaded_pins[connection.Source.PinID.ToString()].Name;
                }
                // Input Pin
                else
                {
                    sourceType = "OR";
                    sourceSlot = "Power Out";
                }

                // Not Gates
                if (loaded_chips[connection.Target.SubChipID.ToString()].Name == "NOT")
                {
                    targetType = "XOR";
                    targetSlot = "Input A";
                }
                // And Gates
                else if (loaded_chips[connection.Target.SubChipID.ToString()].Name == "AND")
                {
                    targetType = "AND";
                    targetSlot = connection.Target.PinID == 0 ? "Input A" : "Input B";
                }
                // Other Rust Elec. Items
                else if (connection.Target.SubChipID > 0 && !custom_chips.ContainsKey(loaded_chips[connection.Target.SubChipID.ToString()].Name))
                {
                    targetType = loaded_chips[connection.Target.SubChipID.ToString()].Name;
                    targetSlot = loaded_pins[connection.Target.PinID.ToString()].Name;
                }
                // Output Pins
                else
                {
                    targetType = "SPLITTER";
                    targetSlot = "Power In";
                }

                var entitySlots = wireEntities(
                    sourceEntity,
                    sourceType,
                    sourceSlot,
                    targetEntity,
                    targetType,
                    targetSlot
                );

                #if DEBUG
                // Change Names of slots for debugging 
                if (connection.Source.SubChipID > 0 && connection.Source.PinID > 20) {
                    entitySlots[0].niceName = $"{loaded_chips[connection.Source.SubChipID.ToString()].Name} => {loaded_pins[connection.Source.PinID.ToString()].Name}";
                }
                if (connection.Target.SubChipID > 0 && connection.Target.PinID > 20) {
                    entitySlots[1].niceName = $"{loaded_pins[connection.Target.PinID.ToString()].Name} => {loaded_chips[connection.Target.SubChipID.ToString()].Name}";
                }
                #endif

                // Adds Signs to Switches for Context
                if(loaded_chips[connection.Source.SubChipID.ToString()].Name == "SIMPLE_SWITCH" || loaded_chips[connection.Source.SubChipID.ToString()].Name == "SMART_SWITCH") {
                    Vector3 sourcePos = sourceEntity.transform.position;
                    Signage switch_sign = SpawnEntity(
                        sign_bindings["SMALL_WOOD_SIGN"],
                        new Vector3(sourcePos.x, sourcePos.y, sourcePos.z + 3),
                        new Quaternion(1, 1, 1, 1)
                    ) as Signage;

                    var lines = new Dictionary<string, Brush>
                    {
                        {loaded_pins[connection.Target.PinID.ToString()].Name, Brushes.Yellow},
                        {loaded_chips[connection.Target.SubChipID.ToString()].Name, Brushes.Orange}
                    };

                    var imageData = Picasso.DrawSign(128, 64, 17, 16, lines);

                    switch_sign.textureIDs[0] = FileStorage.server.Store(imageData, FileStorage.Type.png, switch_sign.net.ID);
                    switch_sign.SendNetworkUpdate();
                }
            }
        }
        // END SPAWN LOGIC

        // BEGIN PLAYER COMMANDS
        [Command("c_build")]
        private void ProjectBuildCommand(IPlayer player, string command, string[] args)
        {
            string chip_name = args[0];

            if (! loaded_projects.ContainsKey(player.Id))
            {
                player.Reply($"You must load a project first to build a chip!");
                return;
            }

            Dictionary<string, Chip> custom_chips = new Dictionary<string, Chip>();
            Dictionary<string, IOEntity> pin_connections = new Dictionary<string, IOEntity>();
            Dictionary<string, Chip> loaded_chips = new Dictionary<string, Chip>();
            Dictionary<string, Pin> loaded_pins = new Dictionary<string, Pin>();
            int offset = 0;

            Project loaded_project = loaded_projects[player.Id];
            Chip chip = project_reader.ReadChip(loaded_project, chip_name);
            if (chip == null)
                return;
            loaded_chips.Add("0", chip);

            player.Reply($"Spawning {chip.Name}");

            BuildChip(player, loaded_project, chip, custom_chips, pin_connections, loaded_chips, loaded_pins, offset);
        }

        [Command("c_load")]
        private void ProjectLoadCommand(IPlayer player, string command, string[] args)
        {
            string project_name = args[0];
            loaded_projects.Add(player.Id, project_reader.ReadProject(project_name));
            player.Reply($"Loaded: {project_name}");
        }

        [Command("c_info")]
        private void ProjectInfoCommand(IPlayer player, string command, string[] args)
        {
            if (! loaded_projects.ContainsKey(player.Id))
            {
                player.Reply("You do not have a project loaded, please load one");
                return;
            }

            Project proj = loaded_projects[player.Id];
            player.Reply($"Project: {proj.ProjectName}");
            player.Reply("Created Chips: ");
            foreach (string chipName in proj.AllCreatedChips)
            {
                if (gate_bindings.ContainsKey(chipName))
                    continue;
                player.Reply(chipName);
            }
        }

        [Command("c_unload")]
        private void ProjectUnloadCommand(IPlayer player, string command, string[] args)
        {
            if (loaded_projects.ContainsKey(player.Id))
            {
                loaded_projects.Remove(player.Id);
                player.Reply($"Unloaded project");
            }
            else
            {
                player.Reply($"No Project to Unload");
            }
        }

        #if DEBUG
            [Command("c_test")]
            // END PLAYER COMMANDS
            private void CTestCommand(IPlayer player, string command, string[] args)
            {
                    player.Reply($"Beginning Test");
                    GetSlots(player);
                    // BobRoss(player);
                    player.Reply($"Test Complete");
                
            }
        #endif

        // BEGIN Hooks
        void OnUserDisconnected(IPlayer player)
        {
            #if !DEBUG
                // Remove player from projects dictionary;
                loaded_projects.Remove(player.Id);
                Puts($"{player.Name} ({player.Id}) project unloaded");
            #endif
        }
        // END Hooks
    }

    // BEGIN DATATYPES FOR DE-SERIALIZATION 
    class ParentHelpers {
        public ProjectReader project_reader = new ProjectReader(new DataFileSystem($"{Interface.Oxide.DataDirectory}\\rust_circuit_boss"));
        public long LongRandom(long min=100000000000000000, long max=100000000000000050) {
            System.Random rand = new System.Random();
            byte[] buf = new byte[8];
            rand.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);

            return (Math.Abs(longRand % (max - min)) + min);
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

    class Chip : ParentHelpers {
        public string Name;
        public HashSet<SubChip> SubChips = new HashSet<SubChip>();
        public HashSet<Pin> InputPins = new HashSet<Pin>();
        public HashSet<Pin> OutputPins = new HashSet<Pin>();
        public HashSet<Connection> Connections = new HashSet<Connection>();

        public void generateNewIds() 
        {
            // Dictionary<long, Chip> chip_definitions = new Dictionary<string, Chip>();
            // Dictionary<long, long> inputPinIdMaps = new Dictionary<long, long>();
            // Dictionary<long, long> outputPinIdMaps = new Dictionary<long, long>();
            // Dictionary<long, long> subChipIdMaps = new Dictionary<long, long>();

            // // Generate new Pin Ids (Input & Output)
            // for (int i = 0; i < 2; i++)
            // {
            //     var iter = i == 0 ? InputPins : OutputPins;
            //     foreach (Pin pin in iter) {
            //         long newId = LongRandom();
            //         if (i == 0)
            //             inputPinIdMaps.Add(pin.ID, newId);
            //         else
            //             outputPinIdMaps.Add(pin.ID, newId);
            //         pin.ID = newId;
            //     }
            // }

            // foreach (SubChip subChip in SubChips)
            // {
                
            // }

            // foreach (Connection connection in Connections)
            // {
            //     // Generate new Connection Ids
            //     connection.Source.PinID = inputPinIdMaps[connection.Source.PinID];
            //     connection.Target.PinID = outputPinIdMaps[connection.Target.PinID];

            //     // Generate new SubChip Ids
            //     if (connection.Source.SubChipID > 0)
            //         connection.Source.SubChipID = subChipIdMaps[connection.Source.SubChipID];

            //     if (connection.Target.SubChipID > 0)
            //         connection.Target.SubChipID = subChipIdMaps[connection.Target.SubChipID];
            // }
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

    class SubChip {
        public string Name;
        public long ID;

        public HashSet<Dictionary<string, float>> Points;

        public string ToString()
        {
            return $"{Name} ({ID})";
        }
    }

    class Project {
        public string ProjectName;
        public HashSet<string> AllCreatedChips = new HashSet<string>();
    }
    // END DATATYPES FOR DE-SERIALIZATION

    class Picasso {
        public static byte[] DrawSign(int width, int height, int yOffset, int fontSize, Dictionary<string, Brush> lines)
        {
            var image = new Bitmap(width, height);

            var graphics = System.Drawing.Graphics.FromImage(image);
            graphics.Clear(System.Drawing.Color.Black);

            int i = 0;
            foreach (KeyValuePair<string, Brush> line in lines) {
                graphics.DrawString(line.Key, new System.Drawing.Font("Arial", fontSize), line.Value, new RectangleF(0, i * yOffset, width, height));
                yOffset += fontSize;
                i++;
            }
            
            return GetBitmapBytes(image);
        }

        public static byte[] GetBitmapBytes(Bitmap bitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                // Save the bitmap to the MemoryStream as PNG
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

                // Get the byte array from the MemoryStream
                return stream.ToArray();
            }
        }
    }

    class ProjectReader {
        private DataFileSystem project_location = new DataFileSystem($"{Interface.Oxide.DataDirectory}\\rust_circuit_boss");

        public ProjectReader(DataFileSystem project_location)
        {
            this.project_location = project_location;
        }

        public Project ReadProject(string name)
        {
            return project_location.ExistsDatafile($"{name}/ProjectSettings") 
                    ? project_location.ReadObject<Project>($"{name}/ProjectSettings")
                    : null;
        }

        public Chip ReadChip(Project project, string chip_name)
        {
            return project_location.ExistsDatafile($"{project.ProjectName}/Chips/{chip_name}")
                    ? project_location.ReadObject<Chip>($"{project.ProjectName}/Chips/{chip_name}")
                    : null;
        }
    }

    class IODefinitions {
            public static Dictionary<string, Dictionary<string,int>> slots = new Dictionary<string, Dictionary<string, int>>
            {
                {"TEST_GENERATOR", new Dictionary<string, int>
                    {
                        {"Power Output 1", 0 },
                        {"Power Output 2", 1 },
                        {"Power Output 3", 2 }
                    }
                },
                {"AND", new Dictionary<string, int>
                    {
                        {"Input A", 0 },
                        {"Input B", 1 },
                        {"Power Out", 0 }
                    }
                },
                {"XOR", new Dictionary<string, int>
                    {
                        {"Input A", 0 },
                        {"Input B", 1 },
                        {"Power Out", 0 }
                    }
                },
                {"OR", new Dictionary<string, int>
                    {
                        {"Input A", 0 },
                        {"Input B", 1 },
                        {"Power Out", 0 }
                    }
                },
                {"MEMORY_CELL", new Dictionary<string, int>
                    {
                        {"Power In", 0 },
                        {"SET", 1 },
                        {"RESET", 2 },
                        {"TOGGLE", 3 },
                        {"Out", 0 },
                        {"Inverted_Out", 1 }
                    }
                },
                {"BLOCKER", new Dictionary<string, int>
                    {
                        {"Power In", 0 },
                        {"Block Pass", 1 },
                        {"Power Out", 0 }
                    }
                },
                {"SPLITTER", new Dictionary<string, int>
                    {
                        {"Power In", 0 },
                        {"Power Out 1", 0 },
                        {"Power Out 2", 1 },
                        {"Power Out 3", 2 }
                    }
                },
                {"E_BRANCH", new Dictionary<string, int>
                    {
                        {"Power Out", 0 },
                        {"Branch Out", 1 },
                        {"Power In", 0 }
                    }
                },
                {"SIMPLE_SWITCH", new Dictionary<string, int>
                    {
                        {"Electric Input", 0 },
                        {"Output", 0 }
                    }
                },
                {"SMART_SWITCH", new Dictionary<string, int>
                    {
                        {"Electric Input", 0 },
                        {"Output", 0 }
                    }
                },
                {"GREEN_LIGHT", new Dictionary<string, int>
                    {
                        {"Power In", 0 },
                        {"Passthrough", 0 }
                    }
                },
                {"RED_LIGHT", new Dictionary<string, int>
                    {
                        {"Power In", 0 },
                        {"Passthrough", 0 }
                    }
                },
                {"WHITE_LIGHT", new Dictionary<string, int>
                    {
                        {"Power In", 0 },
                        {"Passthrough", 0 }
                    }
                }
            };
        }
}