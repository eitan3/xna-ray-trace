using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace RayTraceProject
{
    interface ILight
    {
        bool IsPositionable { get; }
        Vector3 Position { get; }
        Vector3 Direction { get; }
        Vector3 GetLightForFragment(Vector3 position, Vector3 normal);
    }
}
