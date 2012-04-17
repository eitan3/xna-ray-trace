using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace RayTracerTypeLibrary
{
    public class Mesh
    {
        public MeshOctree Octree { get; private set; }
        public Triangle[] Triangles { get; set; }
        public Material MeshMaterial { get; set; }
        public BoundingBox MeshBoundingBox { get; set; }

        public Mesh()
        {
        }

        public Mesh(Triangle[] triangles, Material material, BoundingBox boundingBox)
        {
            this.Triangles = triangles;
            this.MeshMaterial = material;
            this.MeshBoundingBox = boundingBox;
        }

        public void Init()
        {
            this.Octree = new MeshOctree();
            this.Octree.Bodies.AddRange(this.Triangles);
            this.Octree.Build();
        }

        public bool RayIntersects(ref Ray ray)
        {
            float? distance;
            this.MeshBoundingBox.Intersects(ref ray, out distance);
            return distance.HasValue;
        }
    }
}
