using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace RayTracerTypeLibrary
{
    public struct Triangle
    {
        public int id;
        public Vector3 v1, v2, v3;
        public Vector2 uv1, uv2, uv3;
        public Vector3 n1, n2, n3;
    }
}
