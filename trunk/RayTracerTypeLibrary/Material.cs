using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Xna.Framework;

namespace RayTracerTypeLibrary
{
    public unsafe class Material
    {
        private bool useTexture;
        private Bitmap bitmap;
        private BitmapData textureData;
        private float reflect;
        private string textureFilePath;
        object lockObject = new object();

        public Material()
        { 
        }

        public Material(float reflectiveness, bool useTexture)
            : this(reflectiveness, useTexture, null)
        {
        }

        public Material(float reflectiveness, bool useTexture, string textureFilePath)
        {
            this.reflect = reflectiveness;
            this.textureFilePath = textureFilePath;
            this.useTexture = useTexture;
        }

        public void Init()
        {
            if(this.useTexture)
            {
                this.bitmap = (Bitmap)Bitmap.FromFile(textureFilePath);

                this.textureData = this.bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            }
        }

        public void LookupUV(Vector2 uv, out Vector3 color)
        {
            if (uv.X > 1.0f)
                uv.X = uv.X % 1.0f;
            if (uv.Y > 1.0f)
                uv.Y = uv.Y % 1.0f;

            if (uv.X < 0.0f)
                uv.X = 1.0f + (uv.X % 1.0f);
            if (uv.Y < 0.0f)
                uv.Y = 1.0f + (uv.Y % 1.0f);

            int x = (int)(uv.X * (this.textureData.Width-1));
            int y = (int)(uv.Y * (this.textureData.Height-1));

            uint argb;
            //lock (this.lockObject)
            //{
                uint* dataptr = (uint*)this.textureData.Scan0;
                argb = *(dataptr + (this.textureData.Width * y) + x);
            //}
            color = new Vector3(
                ((argb >> 16) & 0x000000FF) / 255.0f,
                ((argb >> 8) & 0x000000FF) / 255.0f,
                (argb & 0x000000FF) / 255.0f);
        }

        public bool UseTexture
        {
            get { return this.useTexture; }
            set { this.useTexture = value; }
        }

        public float Reflectiveness
        {
            get { return this.reflect; }
            set { this.reflect = value; }
        }

        public string TextureFilePath
        {
            get { return this.textureFilePath; }
            set { this.textureFilePath = value; }
        }
    }
}
