using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace RayTracerTypeLibrary
{
    public class MeshOctree
    {
        public struct TriangleIntersectionResult
        {
            public Triangle triangle;
            public float u, v, d;
            public Vector3 objectSpacePosition;

            public TriangleIntersectionResult(
                Triangle triangle,
                float u,
                float v,
                float d,
                Vector3 objectSpacePosition)
            {
                this.triangle = triangle;
                this.u = u;
                this.v = v;
                this.d = d;
                this.objectSpacePosition = objectSpacePosition;
            }
        }

        private class CubeNode
        {
            public uint id;
            public uint depth;
            public BoundingBox bounds;
            public CubeNode parent;
            public CubeNode[] children;
            public List<Triangle> containingObjects;
        }
        private CubeNode root;
        private int itemTreshold = 50;
        private List<Triangle> objects;
        private uint depth;

        public List<Triangle> Bodies
        {
            get { return this.objects; }
        }

        public MeshOctree()
        {
            this.objects = new List<Triangle>();
        }

        public void Build()
        {
            this.depth = 0;
            BoundingBox box = new BoundingBox(Vector3.Zero, Vector3.Zero);


            this.root = new CubeNode();
            root.id = 0;
            root.containingObjects = new List<Triangle>(this.objects);
            root.parent = null;
            root.children = null;
            for (int i = 0; i < this.objects.Count; i++)
            {
                // Kanske en metod i objekten som tar in en boundingb box och utökar den för att säkerställa att allt får plats.
                // "ExpandBoundingBox" "EnsureSpace"
                Vector3.Min(ref box.Min, ref this.objects[i].v1, out box.Min);
                Vector3.Min(ref box.Min, ref this.objects[i].v2, out box.Min);
                Vector3.Min(ref box.Min, ref this.objects[i].v3, out box.Min);

                Vector3.Max(ref box.Max, ref this.objects[i].v1, out box.Max);
                Vector3.Max(ref box.Max, ref this.objects[i].v2, out box.Max);
                Vector3.Max(ref box.Max, ref this.objects[i].v3, out box.Max);
            }

            root.bounds = box;
            this.BuildTree(ref root);
        }

        private void BuildTree(ref CubeNode parent)
        {
            if (parent.containingObjects.Count > this.itemTreshold)
            {
                this.depth++;
                this.SplitCuboid(parent);

                for (int i = 0; i < 8; i++)
                {
                    BuildTree(ref parent.children[i]);
                }
            }
        }

        private void AddBody(Triangle body, CubeNode node)
        {
            // En metod för där en "body" rapporterar om den är innuti en cuboid eller ej.
            if (node.bounds.Contains(body.v1) != ContainmentType.Disjoint ||
                node.bounds.Contains(body.v2) != ContainmentType.Disjoint ||
                node.bounds.Contains(body.v3) != ContainmentType.Disjoint)
            {
                node.containingObjects.Add(body);

                if (node.children != null)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        AddBody(body, node.children[i]);
                    }
                }
            }
        }

        private CubeNode FindCommonParent(CubeNode current, CubeNode nodeA, CubeNode nodeB)
        {
            if (current == null)
                return null;



            while (nodeA.depth > nodeB.depth)
            {
                nodeA = nodeA.parent;
            }

            while (nodeB.depth > nodeA.depth)
            {
                nodeB = nodeB.parent;
            }

            while (nodeA.id != nodeB.id)
            {
                nodeA = nodeA.parent;
                nodeB = nodeB.parent;
            }

            return nodeA;
        }

        private CubeNode InsertLeaf(CubeNode node, ref Vector3 point)
        {
            if (node.children == null)
            {
                return node;
            }
            bool greaterX = point.X > (node.bounds.Min.X + node.bounds.Max.X) / 2f;
            bool greaterY = point.Y > (node.bounds.Min.Y + node.bounds.Max.Y) / 2f;
            bool greaterZ = point.Z > (node.bounds.Min.Z + node.bounds.Max.Z) / 2f;
            if (greaterX)
            {
                if (greaterY)
                {
                    if (greaterZ)
                    {
                        return InsertLeaf(node.children[7], ref point); // 7: Max X, Max Y, Max Z
                    }
                    else
                    {
                        return InsertLeaf(node.children[6], ref point); // 6: Max X, Max Y, Min Z
                    }
                }
                else
                {
                    if (greaterZ)
                    {
                        return InsertLeaf(node.children[5], ref point); // 5: Max X, Min Y, Max Z
                    }
                    else
                    {
                        return InsertLeaf(node.children[4], ref point); // 4: Max X, Min Y, Min Z
                    }
                }
            }
            else
            {
                if (greaterY)
                {
                    if (greaterZ)
                    {
                        return InsertLeaf(node.children[3], ref point); // 3: Min X, Max Y, Max Z
                    }
                    else
                    {
                        return InsertLeaf(node.children[2], ref point); // 2: Min X, Max Y, Min Z
                    }
                }
                else
                {
                    if (greaterZ)
                    {
                        return InsertLeaf(node.children[1], ref point); // 1: Min X, Min Y, Max Z
                    }
                    else
                    {
                        return InsertLeaf(node.children[0], ref point); // 0: Min X, Min Y, Min Z
                    }
                }
            }
        }

        private void SplitCuboid(CubeNode parent)
        {
            Vector3 cubePosition;
            Vector3 cubeSize = (parent.bounds.Max - parent.bounds.Min) / 2f;
            parent.children = new CubeNode[8];
            uint index = 0;
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        CubeNode child = new CubeNode();
                        cubePosition = parent.bounds.Min + new Vector3(cubeSize.X * i, cubeSize.Y * j, cubeSize.Z * k);
                        child.bounds = new BoundingBox(cubePosition, cubePosition + cubeSize);
                        child.parent = parent;
                        child.id = parent.id + index + 2;
                        child.containingObjects = new List<Triangle>();
                        parent.children[index++] = child;

                        for (int objectIndex = 0; objectIndex < parent.containingObjects.Count; objectIndex++)
                        {
                            if(child.bounds.Contains(parent.containingObjects[objectIndex].v1) != ContainmentType.Disjoint ||
                            child.bounds.Contains(parent.containingObjects[objectIndex].v2) != ContainmentType.Disjoint ||
                            child.bounds.Contains(parent.containingObjects[objectIndex].v3) != ContainmentType.Disjoint)
                            {
                                child.containingObjects.Add(parent.containingObjects[objectIndex]);
                            }
                        }
                    }
                }
            }
        }

        private void SplitCubeoid
            (ref BoundingBox parent,
            ref BoundingBox[] cubes)
        {
            Vector3 cubePosition;
            Vector3 cubeSize = (parent.Max - parent.Min) / 2f;
            // float cubeSize = (parent.Max.X - parent.Min.X) / 2f;
            int index = 0;
            for (int i = 0; i < 2; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    for (int k = 0; k < 2; k++)
                    {
                        cubePosition = parent.Min + new Vector3(cubeSize.X * i, cubeSize.Y * j, cubeSize.Z * k);
                        cubes[index++] = new BoundingBox(cubePosition, cubePosition + cubeSize);
                    }
                }
            }
        }

        public bool GetRayIntersection(ref Ray ray, out TriangleIntersectionResult? result, Triangle ignoreTriangle)
        {
            result = null;

            SortedList<float, List<CubeNode>> cubeoids = new SortedList<float, List<CubeNode>>();
            //SortedDictionary<float, CubeNode> cubeoids = new SortedDictionary<float, CubeNode>();
            this.GetRayCubeNodeIntersections(ref ray, this.root, cubeoids);
            if (cubeoids.Count == 0)
                return false;

            List<List<CubeNode>> intersectedCubeoids = cubeoids.Values.ToList();

            int cubeoidIndex = 0;

            float minDistance = float.MaxValue;
            float intersectionU = 0;
            float intersectionV = 0;
            Triangle intersectedTriangle = null;
            bool intersectionFound = false;
            Mesh intersectedMesh = null;

            Vector3 v1, v2, rayDirPosition;
            while (!intersectionFound && cubeoidIndex < cubeoids.Count)
            {
                List<CubeNode> cuboidGroup = intersectedCubeoids[cubeoidIndex++];
                for (int k = 0; k < cuboidGroup.Count; k++)
                {
                    List<Triangle> triangles = cuboidGroup[k].containingObjects;

                    for (int i = 0; i < triangles.Count; i++)
                    {
                        if (ignoreTriangle == null || ignoreTriangle != triangles[i])
                        {
                            float currentU, currentV, distance;
                            if (ray.IntersectsTriangleBackfaceCulling(triangles[i], out currentU, out currentV, out distance) &&
                                distance < minDistance)
                            {
                                minDistance = distance;
                                intersectionU = currentU;
                                intersectionV = currentV;
                                intersectedTriangle = triangles[i];
                                // Signal that intersection was found. Remaining objects in this cubeoid will be examined, but no more cubeoids.
                                intersectionFound = true;
                            }
                        }
                    }
                }
            }

            if (intersectionFound)
            {
                Vector3 p1 = intersectedTriangle.v2 - intersectedTriangle.v1;
                Vector3 p2 = intersectedTriangle.v3 - intersectedTriangle.v1;
                Vector3 interpolatedPosition = intersectedTriangle.v1 + (p1 * intersectionU) + (p2 * intersectionV);

                //Matrix world = intersectedSceneObject.World;
                //Vector3.Transform(ref interpolatedPosition, ref world, out interpolatedPosition);

                result = new TriangleIntersectionResult(
                    intersectedTriangle,
                    intersectionU,
                    intersectionV,
                    minDistance,
                    interpolatedPosition);
            }

            return intersectionFound;
        }

        private void GetRayCubeNodeIntersections(ref Ray ray, CubeNode current, SortedList<float, List<CubeNode>> cuboids) //SortedDictionary<float, CubeNode>
        {
            float? result;
            current.bounds.Intersects(ref ray, out result);
            if (result.HasValue)
            {
                if (current.children != null)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        GetRayCubeNodeIntersections(ref ray, current.children[i], cuboids);
                    }
                }
                else
                {
                    if (cuboids.ContainsKey(result.Value))
                    {
                        cuboids[result.Value].Add(current);
                    }
                    else
                    {
                        cuboids.Add(result.Value, new List<CubeNode>() { current });
                    }
                }
            }
        }
    }
}
