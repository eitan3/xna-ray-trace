﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using RayTracerTypeLibrary;

namespace RayTraceProject.Spatial
{
    public interface ISpatialBody
    {
        Vector3 Position
        {
            get;
        }
        BoundingBox BoundingBox
        {
            get;
            set;
        }
        BoundingBox WorldBoundingBox
        {
            get;
        }

        //bool GetIntersectingFaceNormal(Ray ray, out Vector3 normal, out float? distance);

        List<Triangle> GetTriangles();

        bool RayIntersects(ref Ray ray);
    }
}
