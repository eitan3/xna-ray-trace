using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace RayTraceProject.Spatial
{
    interface ISpatialManager
    {
        List<ISpatialBody> Bodies { get; }
        void Build();

        // Tveksamt om dessa metoder ska finnas i detta interface. Ignorerar det så länge.
        List<ISpatialBody> GetPossibleIntersections(Vector3 point);
        uint GetCubeoidId(Vector3 point);
        List<ISpatialBody> GetContainedBodies(uint cubeoidId);
        bool TranslateRayToScene(ref Ray ray);
    }
}
