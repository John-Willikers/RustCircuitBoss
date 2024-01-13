using System;
using System.Collections.Generic;
using static System.Random;
using Oxide.Core;
using Oxide.Core.Plugins;
namespace Oxide.Plugins {

    [Info("JData", "JohnWillikers", "0.1.0")]
    [Description("Data Stuff")]
    class JData : RustPlugin {

        private DataFileSystem data_dir = new DataFileSystem($"{Interface.Oxide.DataDirectory}");
        private Dictionary<string, Project> user_loaded_projects = new Dictionary<string, Project>(); 

        [HookMethod("SetNewDataDir")]
        public void SetNewDataDir(string name)
        {
            user_loaded_projects.Clear();
            data_dir = new DataFileSystem($"{Interface.Oxide.DataDirectory}\\{name}");
        }

        [HookMethod("AssignProjectToUser")]
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

        [HookMethod("UnloadUserProject")]
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

        [HookMethod("GetUserProject")]
        public Project GetUserProject(string uId)
        {
            return (user_loaded_projects.ContainsKey(uId))
                    ? user_loaded_projects[uId]
                    : null;
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
    class ParentHelpers {
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