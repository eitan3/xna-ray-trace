using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Xna.Framework;
using System.Diagnostics;

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

        public enum TextureFiltering
        {
            Point,
            Bilinear,
        }

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

        public void LookupUV(Vector2 uv, TextureFiltering filtering, out Vector3 color)
        {
            if (uv.X > 1.0f)
                uv.X = uv.X % 1.0f;
            if (uv.Y > 1.0f)
                uv.Y = uv.Y % 1.0f;

            if (uv.X < 0.0f)
                uv.X = 1.0f + (uv.X % 1.0f);
            if (uv.Y < 0.0f)
                uv.Y = 1.0f + (uv.Y % 1.0f);

            switch (filtering)
            {
                case TextureFiltering.Point:
                    this.GetColorPoint(ref uv, out color);
                    break;
                case TextureFiltering.Bilinear:
                    this.GetColorBilinear(ref uv, out color);
                    break;
                default:
                    throw new ArgumentException("Value does not fall within the expected range", "filtering");

            }
        }

        private void GetColorPoint(ref Vector2 uv, out Vector3 color)
        {
            int x = (int)(uv.X * (this.textureData.Width - 1));
            int y = (int)(uv.Y * (this.textureData.Height - 1));

            uint* dataptr = (uint*)this.textureData.Scan0;
            uint baseArgb;
            baseArgb = *(dataptr + (this.textureData.Width * y) + x);
            
            color = new Vector3(
                ((baseArgb >> 16) & 0x000000FF) / 255.0f,
                ((baseArgb >> 8) & 0x000000FF) / 255.0f,
                (baseArgb & 0x000000FF) / 255.0f);
        }

        private void GetColorBilinear(ref Vector2 uv, out Vector3 color)
        {
            // How many texels do we have per pixel?
            float recipWidth = 1.0f / (this.textureData.Width);
            float recipHeight = 1.0f / (this.textureData.Height);

            // remainderX and remainderY tells us how much of the neighbouring pixels we are "touching".
            double remainderX = Math.IEEERemainder(uv.X, recipWidth);
            double remainderY = Math.IEEERemainder(uv.Y, recipHeight);

            // Subtract with the remainders, so the UV coordinates are positioned ON a texel.
            uv.X -= (float)remainderX;
            uv.Y -= (float)remainderY;

            // Get the pixel xy coordinates for our texel.
            int x = (int)(uv.X * (this.textureData.Width - 1));
            int y = (int)(uv.Y * (this.textureData.Height - 1));

            // Get the pixel xy coordinates for the surrounding pixels.
            int x2 = (int)((uv.X + recipWidth) * (this.textureData.Width - 1));
            int y2 = (int)((uv.Y + recipHeight) * (this.textureData.Height - 1));

            // Extract all four colors; our "base" color, the one below, the one to the side, and the one diagonally down.
            uint baseArgb, blendXargb, blendYargb, blendXYargb;

            uint* dataptr = (uint*)this.textureData.Scan0;
            baseArgb = *(dataptr + (this.textureData.Width * y) + x);
            blendXargb = *(dataptr + (this.textureData.Width * y) + x2);
            blendYargb = *(dataptr + (this.textureData.Width * y2) + x);
            blendXYargb = *(dataptr + (this.textureData.Width * y2) + x2);
            //}
            Vector3 baseColor = new Vector3(
                ((baseArgb >> 16) & 0x000000FF) / 255.0f,
                ((baseArgb >> 8) & 0x000000FF) / 255.0f,
                (baseArgb & 0x000000FF) / 255.0f);
            Vector3 blendXColor = new Vector3(
                ((blendXargb >> 16) & 0x000000FF) / 255.0f,
                ((blendXargb >> 8) & 0x000000FF) / 255.0f,
                (blendXargb & 0x000000FF) / 255.0f);
            Vector3 blendYColor = new Vector3(
                ((blendYargb >> 16) & 0x000000FF) / 255.0f,
                ((blendYargb >> 8) & 0x000000FF) / 255.0f,
                (blendYargb & 0x000000FF) / 255.0f);
            Vector3 blendXYColor = new Vector3(
                ((blendXYargb >> 16) & 0x000000FF) / 255.0f,
                ((blendXYargb >> 8) & 0x000000FF) / 255.0f,
                (blendXYargb & 0x000000FF) / 255.0f);

            // Interpolate between the four colors.
            Vector3 colorXdifference = blendXColor - baseColor;
            Vector3 colorYdifference = blendYColor - baseColor;
            Vector3 colorXYdifference = blendXYColor - baseColor;

            float dx = (float)(remainderX / recipWidth) + 0.5f;
            float dy = (float)(remainderY / recipHeight) + 0.5f;
            float recipDx = 1.0f - dx;
            float recipDy = 1.0f - dy;

            color = (baseColor * recipDx * recipDy) +
                (blendYColor * recipDx * (dy)) +
                (blendXColor * dx * recipDy) +
                (blendXYColor * dx * dy); ;
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
