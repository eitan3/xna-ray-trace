using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using RayTracerTypeLibrary;

namespace RayTraceProject.Spatial
{
    interface ISpatialManager
    {
        List<ISpatialBody> Bodies { get; }
        void Build();

        bool GetRayIntersection(ref Ray ray, out IntersectionResult? result, Triangle ignoreTriangle, SceneObject ignoreObject);
    }
}
