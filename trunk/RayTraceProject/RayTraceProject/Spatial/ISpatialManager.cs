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

        // Tveksamt om dessa metoder ska finnas i detta interface. Ignorerar det så länge.
        List<ISpatialBody> GetPossibleIntersections(Vector3 point);
        uint GetCubeoidId(Vector3 point);
        List<ISpatialBody> GetContainedBodies(uint cubeoidId);
        bool TranslateRayToScene(ref Ray ray);

        List<ISpatialBody> GetIntersectedBodies(ref Ray ray);
        bool GetRayIntersection(ref Ray ray, out Triangle triangle, out float? u, out float? v, Triangle origin);
    }
}
