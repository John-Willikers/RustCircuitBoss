using System.Collections.Generic;
using System;
using System.Drawing;
using System.IO;
using static System.Random;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Picasso", "JohnWillikers", "0.1.0")]
    [Description("Used to draw things on signs")]
    class Picasso : RustPlugin {
        public enum FontSize {
            Small = 12,
            Medium = 24,
            Large = 36
        }

        public enum Signs: int {
            WoodenSmall = 0,
        }

        private string[] SignBindings = new string[] {
            "assets/prefabs/deployable/signs/sign.small.wood.prefab"
        };

        public void SpawnSign(Vector3 position, Quaternion rotation, Signs sign_type, int width, int height, int yOffset, FontSize fontSize, Dictionary<string, Brush> lines)
        {
            // Create a Sign
            var sign = GameManager.server.CreateEntity(SignBindings[(int) sign_type], position, rotation) as Signage;
            sign.Spawn();
            sign.SetFlag(BaseEntity.Flags.Locked, true);
            sign.SendNetworkUpdateImmediate();

            // Write Bitmap to Server and assign to Sign
            var image = DrawTextSign(width, height, yOffset, (int) fontSize, lines);
            sign.textureIDs[0] = FileStorage.server.Store(image, FileStorage.Type.png, sign.net.ID);
            sign.SendNetworkUpdateImmediate();
        }

        public static byte[] DrawTextSign(int width, int height, int yOffset, int fontSize, Dictionary<string, Brush> lines)
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
}