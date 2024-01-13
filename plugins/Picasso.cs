using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Picasso", "JohnWillikers", "0.1.0")]
    [Description("Used to draw things on signs")]
    class Picasso : RustPlugin {
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
}