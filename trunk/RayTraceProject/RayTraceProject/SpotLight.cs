using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RayTraceProject
{
    class SpotLight : ILight
    {
        private float angleCosine;
        private float spotAngle;
        public Vector3 Position { get; set; }
        public Vector3 Direction { get; set; }
        public Vector3 Color { get; set; }
        public float DecayExponent { get; set; }
        public float Intensity { get; set; }
        public float SpotAngle
        {
            get { return this.spotAngle; }
            set
            {
                this.spotAngle = value;
                this.angleCosine = (float)Math.Cos(this.spotAngle * 0.5f);
            }
        }

        public bool IsPositionable { get { return true; } }

        public SpotLight()
        {
            this.DecayExponent = 1.3f;
            this.Intensity = 1.0f;
        }

        public Vector3 GetLightForFragment(Vector3 position, Vector3 normal)
        {
            Vector3 dirToLight = this.Position - position;

            dirToLight.Normalize();

            float surfaceDot;
            Vector3.Dot(ref dirToLight, ref normal, out surfaceDot);
            if (surfaceDot < 0.0f) // Fragment is facing away from the light.
            {
                return Vector3.Zero;
            }
            
            float lightDot = Vector3.Dot(-dirToLight, this.Direction);

            if (lightDot > this.angleCosine)
            {
                float spotIntensity = this.Intensity * (float)((lightDot - this.angleCosine) / Math.Pow((1 - this.angleCosine), this.DecayExponent));
                return this.Color * spotIntensity * surfaceDot;
            }
            else
            {
                return Vector3.Zero;
            }
            
        }
    }
}
