// Requires: Picasso
// Requires: JData
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
        // Begin Plugin References
        [PluginReference] 
        private Picasso Picasso;
        [PluginReference]
        private JData JData;
        // End Plugin References

        private void Init()
        {
            Puts("A baby plugin is born!");
        }

        private void Loaded()
        {
            JData.SetNewDataDir("rust_circuit_boss");
            #if DEBUG
                Puts("DEBUG ENABLED - loading test project to Ten'a Account");
                JData.AssignProjectToUser("76561198064426107", "Test");
            #endif
        }

        #if DEBUG
        // BEGIN TEST FUNCS 
        
        /**
        * Test Func to spawn a sign and draw on it
        * 
        * @param IPlayer player
        */
        public void BobRoss(IPlayer player)
        {
            GenericPosition position = player.Position();

            var lines = new Dictionary<string, Brush>
            {
                {"Hello World", Brushes.Yellow },
                {"Oh No", Brushes.Orange}
            };

            Picasso.SpawnSign(
                new Vector3(position.X, position.Y, position.Z + 1),
                new Quaternion(0, 0, 0, 0),
                Picasso.Signs.WoodenSmall,
                128,
                64,
                17,
                Picasso.FontSize.Small,
                lines
            );
        }
        // END TEST FUNCS
        #endif

        // BEGIN PLAYER COMMANDS
        [Command("c_build")]
        private void ProjectBuildCommand(IPlayer player, string command, string[] args)
        {
            JData.BuildChip(args[0], player);
        }

        [Command("c_load")]
        private void ProjectLoadCommand(IPlayer player, string command, string[] args)
        {
            var loaded_project = JData.AssignProjectToUser(player.Id, args[0]);
            if (loaded_project == null)
                player.Reply($"Failed to load project: {args[0]}");
            else
                player.Reply($"Loaded: {loaded_project.ProjectName}");
        }

        [Command("c_info")]
        private void ProjectInfoCommand(IPlayer player, string command, string[] args)
        {
            var loaded_project = JData.GetUserProject(player.Id);
            if (loaded_project == null)
            {
                player.Reply($"No project loaded");
                return;
            }
              
            player.Reply($"Project: {loaded_project.ProjectName}");
            player.Reply("Created Chips: ");
            foreach (string chipName in loaded_project.AllCreatedChips)
            {
                player.Reply(chipName);
            }
        }

        [Command("c_unload")]
        private void ProjectUnloadCommand(IPlayer player, string command, string[] args)
        {
            var result = JData.UnloadUserProject(player.Id);
            if (result)
                player.Reply($"Unloaded project");
            else
                player.Reply($"No project to unload");
        }

        #if DEBUG
            [Command("c_test")]
            // END PLAYER COMMANDS
            private void CTestCommand(IPlayer player, string command, string[] args)
            {
                    player.Reply($"Beginning Test");
                    // GetSlots(player);
                    BobRoss(player);
                    player.Reply($"Test Complete");
                
            }
        #endif

        // BEGIN Hooks
        void OnUserDisconnected(IPlayer player)
        {
            #if !DEBUG
                // Remove player from projects dictionary;
                JData.UnloadUserProject(player.Id);
                Puts($"{player.Name} ({player.Id}) project unloaded");
            #endif
        }
        // END Hooks
    }
}