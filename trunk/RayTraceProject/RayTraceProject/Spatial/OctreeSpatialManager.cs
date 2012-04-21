using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using RayTracerTypeLibrary;
using Microsoft.Xna.Framework.Graphics;

namespace RayTraceProject.Spatial
{
    public struct IntersectionResult
    {
        public Mesh mesh;
        public Triangle triangle;
        public float u, v, d;
        public Vector3 worldPosition;

        public IntersectionResult(
            Mesh mesh,
            Triangle triangle,
            float u,
            float v,
            float d,
            Vector3 worldPosition)
        {
            this.mesh = mesh;
            this.triangle = triangle;
            this.u = u;
            this.v = v;
            this.d = d;
            this.worldPosition = worldPosition;
        }
    }

    class OctreeSpatialManager : ISpatialManager
    {
        private class CubeNode
        {
            public uint id;
            public uint depth;
            public BoundingBox bounds;
            public CubeNode parent;
            public CubeNode[] children;
            public List<ISpatialBody> containingObjects;
#if DEBUG
            public DrawableBox box;
#endif
        }
        private CubeNode root;
        private int itemTreshold = 20;
        private List<ISpatialBody> objects;
        private uint depth;

        public List<ISpatialBody> Bodies
        {
            get { return this.objects; }
        }

        public OctreeSpatialManager()
        {
            this.objects = new List<ISpatialBody>();
        }

        public void Build()
        {
            this.depth = 0;
            BoundingBox box = new BoundingBox(Vector3.Zero, Vector3.Zero);


            this.root = new CubeNode();
            root.id = 0;
            root.containingObjects = new List<ISpatialBody>(this.objects);
            root.parent = null;
            root.children = null;
            for (int i = 0; i < this.objects.Count; i++)
            {
                //BoundingBox worldBox = this.objects[i].WorldBoundingBox;
                BoundingBox transformedBox = this.objects[i].BoundingBox;
                Matrix world = (this.objects[i] as SceneObject).World;
                Vector3.Transform(ref transformedBox.Min, ref world, out transformedBox.Min);
                Vector3.Transform(ref transformedBox.Max, ref world, out transformedBox.Max);

                BoundingBox objectBox = new BoundingBox(
                    Vector3.Min(transformedBox.Min, transformedBox.Max),
                    Vector3.Max(transformedBox.Min, transformedBox.Max));

                if (i == 0)
                {
                    box = objectBox;
                }
                else
                {
                    BoundingBox.CreateMerged(ref box, ref objectBox, out box);
                }
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

        private void AddBody(ISpatialBody body, CubeNode node)
        {
            if (node.bounds.Intersects(body.WorldBoundingBox))
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
                        child.containingObjects = new List<ISpatialBody>();
                        parent.children[index++] = child;

                        for (int objectIndex = 0; objectIndex < parent.containingObjects.Count; objectIndex++)
                        {
                            if (parent.containingObjects[objectIndex].WorldBoundingBox.Intersects(child.bounds))
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

        public void Draw(Camera camera, ref Matrix view, ref Matrix proj, GraphicsDevice device, GameTime gameTime)
        {
            this.DrawNode(camera, ref view, ref proj, this.root, device, gameTime);
        }


        private void DrawNode(Camera camera, ref Matrix view, ref Matrix proj, CubeNode node, GraphicsDevice device, GameTime gameTime)
        {
#if DEBUG
            if (node.box == null)
            {
                node.box = new DrawableBox(device, node.bounds);
            }
            Matrix identity = Matrix.Identity;
            node.box.Draw(device, ref view, ref proj, ref identity);
#endif
            //this.objects.Sort(new Comparison<ISpatialBody>(fo));
            //for (int i = 0; i < this.objects.Count; i++)
            //   (this.objects[i] as GameObject3D).Draw(ref view, ref proj, gameTime);
            //return;
            if (camera.BoundingFrustum.Intersects(node.bounds))
            {
                for (int i = 0; i < node.containingObjects.Count; i++)
                {
                    if (camera.BoundingFrustum.Intersects(node.containingObjects[i].BoundingBox))
                    {
                        (node.containingObjects[i] as SceneObject).Draw(ref view, ref proj, device, gameTime);
                        //node.containingObjects[i].BoundingBox.Draw(device);
                    }
                }

                if (node.children != null)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        DrawNode(camera, ref view, ref proj, node.children[i], device, gameTime);
                    }
                }
            }
        }

        public bool GetRayIntersection(ref Ray ray, out IntersectionResult? result, Triangle ignoreTriangle, Mesh ignoreObject)
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
            bool intersectionFound = false;
            Mesh intersectedMesh = null;
            SceneObject intersectedSceneObject = null;
            RayTracerTypeLibrary.MeshOctree.TriangleIntersectionResult? intersectedTriangleResult = null;
            RayTracerTypeLibrary.MeshOctree.TriangleIntersectionResult? triangleResult;

            Vector3 v1, v2, rayDirPosition;
            while (!intersectionFound && cubeoidIndex < cubeoids.Count)
            {
                List<CubeNode> cuboidGroup = intersectedCubeoids[cubeoidIndex++];
                for (int k = 0; k < cuboidGroup.Count; k++)
                {
                    List<ISpatialBody> objects = cuboidGroup[k].containingObjects;

                    for (int i = 0; i < objects.Count; i++)
                    {
                        if (ignoreObject == null || ignoreObject != objects[i])
                        {


                            SceneObject sceneObject = (SceneObject)objects[i];

                            Matrix inverseWorld = sceneObject.InverseWorld;
                            // -- While the below LOOKS like it should work, it does not. Correct solution below!
                            //Ray transformedRay;
                            //Vector3.Transform(ref ray.Position, ref inverseWorld, out transformedRay.Position);
                            //Vector3.Transform(ref ray.Direction, ref inverseWorld, out transformedRay.Direction);
                            //transformedRay.Direction.Normalize();

                            //Vector3 v1 = Vector3.Transform(ray.Position, inverseWorld);
                            //Vector3 v2 = Vector3.Transform(ray.Position + ray.Direction, inverseWorld);
                            Vector3.Add(ref ray.Position, ref ray.Direction, out rayDirPosition);

                            Vector3.Transform(ref ray.Position, ref inverseWorld, out v1);
                            Vector3.Transform(ref rayDirPosition, ref inverseWorld, out v2);
                            Vector3.Subtract(ref v2, ref v1, out rayDirPosition);
                            Ray transformedRay = new Ray(v1, rayDirPosition);
                            transformedRay.Direction.Normalize();

                            for (int meshIndex = 0; meshIndex < sceneObject.Meshes.Count; meshIndex++)
                            {
                                if (sceneObject.Meshes[meshIndex].RayIntersects(ref transformedRay))
                                {
                                    if (sceneObject.Meshes[meshIndex].Octree.GetRayIntersection(ref transformedRay, out triangleResult, ignoreTriangle) &&
                                        triangleResult.Value.d < minDistance)
                                    {
                                        minDistance = triangleResult.Value.d;
                                        intersectedTriangleResult = triangleResult;
                                        intersectedMesh = sceneObject.Meshes[meshIndex];
                                        intersectedSceneObject = sceneObject;
                                        intersectionFound = true;
                                    }
                                    
                                    //Triangle[] triangles = sceneObject.Meshes[meshIndex].Triangles;
                                    //// Backface culling IF first triangle (and thus the rest) is transparent. Rewrite this in the future.
                                    //if (sceneObject.Meshes[meshIndex].MeshMaterial.Transparent)
                                    //{
                                    //    for (int j = 0; j < triangles.Length; j++)
                                    //    {
                                    //        if (ignoreTriangle == null || ignoreTriangle != triangles[j])
                                    //        {
                                    //            float currentU, currentV, distance;
                                    //            if (transformedRay.IntersectsTriangle(triangles[j], out currentU, out currentV, out distance) &&
                                    //                distance < minDistance)
                                    //            {
                                    //                minDistance = distance;
                                    //                intersectionU = currentU;
                                    //                intersectionV = currentV;
                                    //                intersectedTriangle = triangles[j];
                                    //                intersectedMesh = sceneObject.Meshes[meshIndex];
                                    //                intersectedSceneObject = sceneObject;
                                    //                // Signal that intersection was found. Remaining objects in this cubeoid will be examined, but no more cubeoids.
                                    //                intersectionFound = true;
                                    //            }
                                    //        }
                                    //    }
                                    //}
                                    //else
                                    //{
                                    //    for (int j = 0; j < triangles.Length; j++)
                                    //    {
                                    //        if (ignoreTriangle == null || ignoreTriangle != triangles[j])
                                    //        {
                                    //            float currentU, currentV, distance;
                                    //            if (transformedRay.IntersectsTriangleBackfaceCulling(triangles[j], out currentU, out currentV, out distance) &&
                                    //                distance < minDistance)
                                    //            {
                                    //                minDistance = distance;
                                    //                intersectionU = currentU;
                                    //                intersectionV = currentV;
                                    //                intersectedTriangle = triangles[j];
                                    //                intersectedMesh = sceneObject.Meshes[meshIndex];
                                    //                intersectedSceneObject = sceneObject;
                                    //                // Signal that intersection was found. Remaining objects in this cubeoid will be examined, but no more cubeoids.
                                    //                intersectionFound = true;
                                    //            }
                                    //        }
                                    //    }
                                    //}

                                }
                            }


                        }
                    }
                }


            }

            if (intersectionFound)
            {
                Vector3 interpolatedPosition;
                Matrix world = intersectedSceneObject.World;
                Vector3 objectSpacePosition = intersectedTriangleResult.Value.objectSpacePosition;
                Vector3.Transform(ref objectSpacePosition, ref world, out interpolatedPosition);

                result = new IntersectionResult(
                    intersectedMesh,
                    intersectedTriangleResult.Value.triangle,
                    intersectedTriangleResult.Value.u,
                    intersectedTriangleResult.Value.v,
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
