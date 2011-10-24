using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace RayTracerTypeLibrary
{
    public class RayTracerTexture
    {
        int[] colorData;

        public int[] ColorData
        {
            get { return this.colorData; }
            set { this.colorData = value; }
        }

        public RayTracerTexture()
        {
        }

        public RayTracerTexture(string path)
        {
            using (Bitmap bitmap = new Bitmap(path))
            {
                BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb);

                this.colorData = new int[bitmapData.Width * bitmapData.Height];
                System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, this.colorData, 0, bitmapData.Width * bitmapData.Height);
            }
        }


    }
}
