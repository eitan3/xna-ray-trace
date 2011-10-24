using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace RayTraceProject
{
    class DirectionalLight : ILight
    {
        public Vector3 Direction { get; set; }
        public Vector3 Color { get; set; }
        public Vector3 Position { get { return Vector3.Zero; } }
        public float Intensity { get; set; }

        public bool IsPositionable { get { return false; } }

        public DirectionalLight()
        {
            this.Intensity = 1.0f;
        }

        public Vector3 GetLightForFragment(Vector3 position, Vector3 normal)
        {
            float surfaceDot = Vector3.Dot(this.Direction, normal);
            if (surfaceDot < 0.0f)
                surfaceDot = 0.0f;

            return this.Color * surfaceDot * this.Intensity;
        }
    }
}
