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
    public enum TextureFiltering
    {
        Point,
        Bilinear,
    }

    public enum UVAddressMode
    {
        Clamp,
        Wrap,
        Mirror
    }

    public unsafe class Material
    {
        private const float BYTE_RECIPROCAL = 1.0f / 255.0f;
        private bool useTexture;
        private bool interpolateNormals;
        private Bitmap bitmap;
        private BitmapData textureData;
        private float reflect;
        private string textureFilePath;
        private Vector2 texelDensity;
        object lockObject = new object();

        public RayTracerTexture Texture { get; set; }

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
                
                this.texelDensity = new Vector2(1.0f / (this.textureData.Width),  1.0f / (this.textureData.Height));
            }
        }

        public void LookupUV(Vector2 uv, UVAddressMode addressMode, TextureFiltering filtering, out Vector3 color)
        {
            switch (addressMode)
            {
                case UVAddressMode.Clamp:
                    this.ClampUV(ref uv);
                    break;
                case UVAddressMode.Wrap:
                    this.WrapUV(ref uv);
                    break;
                case UVAddressMode.Mirror:
                    this.MirrorUV(ref uv);
                    break;
                default:
                    throw new ArgumentException("Value does not fall within the expected range", "addressMode");
            }

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

        private void MirrorUV(ref Vector2 uv)
        {
            Vector2 originalUV = uv;
            if (uv.X > 1.0f)
                uv.X = (uv.X % 1.0f);
            if (uv.Y > 1.0f)
                uv.Y = (uv.Y % 1.0f);

            if (uv.X < 0.0f)
                uv.X = 1 + (uv.X % 1.0f);
            if (uv.Y < 0.0f)
                uv.Y = 1 + (uv.Y % 1.0f);

            if ((int)(originalUV.X - uv.X) % 2 == 0)
            {
                uv.X = 1.0f - uv.X;
            }
            if ((int)(originalUV.Y - uv.Y) % 2 == 0)
            {
                uv.Y = 1.0f - uv.Y;
            }
        }

        private void WrapUV(ref Vector2 uv)
        {
            if (uv.X > 1.0f)
                uv.X = uv.X % 1.0f;
            if (uv.Y > 1.0f)
                uv.Y = uv.Y % 1.0f;

            if (uv.X < 0.0f)
                uv.X = 1.0f + (uv.X % 1.0f);
            if (uv.Y < 0.0f)
                uv.Y = 1.0f + (uv.Y % 1.0f);
        }

        private void ClampUV(ref Vector2 uv)
        {
            Vector2 zero = Vector2.Zero;
            Vector2 one = Vector2.One;
            Vector2.Clamp(ref uv, ref zero, ref one, out uv);
        }

        private void GetColorPoint(ref Vector2 uv, out Vector3 color)
        {
            int x = (int)(uv.X * (this.textureData.Width - 1));
            int y = (int)(uv.Y * (this.textureData.Height - 1));

            uint* dataptr = (uint*)this.textureData.Scan0;
            uint baseArgb;
            baseArgb = *(dataptr + (this.textureData.Width * y) + x);

            // The final color is multiplied with BYTE_RECIPROCAL (1 / 255) instead of being divided by 255. It has the same effect but is apparently
            // slightly more efficient.
            color = new Vector3(
                ((baseArgb >> 16) & 0x000000FF) * BYTE_RECIPROCAL,
                ((baseArgb >> 8) & 0x000000FF) * BYTE_RECIPROCAL,
                (baseArgb & 0x000000FF) * BYTE_RECIPROCAL);
        }

        private void GetColorBilinear(ref Vector2 uv, out Vector3 color)
        {
            // How many texels do we have per pixel?


            // remainderX and remainderY tells us how much of the neighbouring pixels we are "touching".
            double remainderX = Math.IEEERemainder(uv.X, this.texelDensity.X);
            double remainderY = Math.IEEERemainder(uv.Y, this.texelDensity.Y);

            // Subtract with the remainders, so the UV coordinates are positioned ON a texel.
            uv.X -= (float)remainderX;
            uv.Y -= (float)remainderY;

            // Get the pixel xy coordinates for our texel.
            int x = (int)(uv.X * (this.textureData.Width - 1));
            int y = (int)(uv.Y * (this.textureData.Height - 1));

            // Get the pixel xy coordinates for the surrounding pixels.
            int x2 = (int)((uv.X + this.texelDensity.X) * (this.textureData.Width - 1));
            int y2 = (int)((uv.Y + this.texelDensity.Y) * (this.textureData.Height - 1));

            // Extract all four colors; our "base" color, the one below, the one to the side, and the one diagonally down.
            //uint baseArgb, blendXargb, blendYargb, blendXYargb;
            int baseArgb, blendXargb, blendYargb, blendXYargb;

            uint* dataptr = (uint*)this.textureData.Scan0;
            //baseArgb = *(dataptr + (this.textureData.Width * y) + x);
            //blendXargb = *(dataptr + (this.textureData.Width * y) + x2);
            //blendYargb = *(dataptr + (this.textureData.Width * y2) + x);
            //blendXYargb = *(dataptr + (this.textureData.Width * y2) + x2);
            baseArgb = this.Texture.ColorData[(this.textureData.Width * y) + x];
            blendXargb = this.Texture.ColorData[(this.textureData.Width * y) + x2];
            blendYargb = this.Texture.ColorData[(this.textureData.Width * y2) + x];
            blendXYargb = this.Texture.ColorData[(this.textureData.Width * y2) + x2];


            
            Vector3 baseColor = new Vector3(
                ((baseArgb >> 16) & 0x000000FF),
                ((baseArgb >> 8) & 0x000000FF),
                (baseArgb & 0x000000FF) );
            Vector3 blendXColor = new Vector3(
                ((blendXargb >> 16) & 0x000000FF),
                ((blendXargb >> 8) & 0x000000FF),
                (blendXargb & 0x000000FF));
            Vector3 blendYColor = new Vector3(
                ((blendYargb >> 16) & 0x000000FF),
                ((blendYargb >> 8) & 0x000000FF),
                (blendYargb & 0x000000FF));
            Vector3 blendXYColor = new Vector3(
                ((blendXYargb >> 16) & 0x000000FF),
                ((blendXYargb >> 8) & 0x000000FF),
                (blendXYargb & 0x000000FF));

            // Interpolate between the four colors.
            Vector3 colorXdifference = blendXColor - baseColor;
            Vector3 colorYdifference = blendYColor - baseColor;
            Vector3 colorXYdifference = blendXYColor - baseColor;

            float dx = (float)(remainderX * this.textureData.Width) + 0.5f;
            float dy = (float)(remainderY * this.textureData.Height) + 0.5f;
            float invertDx = 1.0f - dx;
            float invertDy = 1.0f - dy;

            // The final color is multiplied with BYTE_RECIPROCAL (1 / 255) instead of being divided by 255. It has the same effect but is apparently
            // slightly more efficient.
            color = ((baseColor * invertDx * invertDy) +
                (blendYColor * invertDx * dy) +
                (blendXColor * dx * invertDy) +
                (blendXYColor * dx * dy)) * BYTE_RECIPROCAL;
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

        public bool InterpolateNormals
        {
            get { return this.interpolateNormals; }
            set { this.interpolateNormals = value; }
        }
    }
}
