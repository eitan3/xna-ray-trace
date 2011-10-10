using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using RayTracerTypeLibrary;
using Microsoft.Xna.Framework.Graphics;

namespace RayTraceProject.Spatial
{
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
        }
        private CubeNode root;
        private int itemTreshold = 2;
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
            BoundingBox objectBox;

            this.root = new CubeNode();
            root.id = 0;
            root.containingObjects = new List<ISpatialBody>(this.objects);
            root.parent = null;
            root.children = null;
            for (int i = 0; i < this.objects.Count; i++)
            {
                objectBox = this.objects[i].BoundingBox;

                BoundingBox.CreateMerged(ref box, ref objectBox, out box);
            }

            root.bounds = box;
            this.BuildTree(ref root);
        }

        private void BuildTree(ref CubeNode parent)
        {
            if (parent.containingObjects.Count > this.itemTreshold)
            {
                this.depth++;
                BoundingBox[] childrenBounds = new BoundingBox[8];
                parent.children = new CubeNode[8];
                this.SplitCubeoid(ref parent.bounds, ref childrenBounds);
                for (uint i = 0; i < 8; i++)
                {
                    parent.children[i] = new CubeNode();
                    parent.children[i].parent = parent;
                    parent.children[i].id = parent.id + i + 1;
                    parent.children[i].bounds = childrenBounds[i];
                    parent.children[i].depth = this.depth;
                    parent.children[i].containingObjects = new List<ISpatialBody>();
                }

                CubeNode minParent, maxParent;
                Vector3 minPosition, maxPosition;

                for (int i = parent.containingObjects.Count - 1; i >= 0; i--)
                {
                    minPosition = parent.containingObjects[i].BoundingBox.Min;
                    maxPosition = parent.containingObjects[i].BoundingBox.Max;
                    minParent = this.InsertLeaf(parent, ref minPosition);
                    maxParent = this.InsertLeaf(parent, ref maxPosition);

                    // If the two parents are identical, the entire bounding box is enclosed in the node.
                    if (minParent.id == maxParent.id)
                    {
                        minParent.containingObjects.Add(parent.containingObjects[i]);
                        parent.containingObjects.RemoveAt(i);
                    }
                    else
                    {
                        CubeNode commonParent = FindCommonParent(minParent, minParent, maxParent);
                        if (commonParent == null)
                            throw new InvalidOperationException("Could not find common parent!");

                        commonParent.containingObjects.Add(parent.containingObjects[i]);
                        parent.containingObjects.RemoveAt(i);
                    }
                }
                for (int i = 0; i < 8; i++)
                {
                    BuildTree(ref parent.children[i]);
                }
                //for (int i = 0; i < 8; i++)
                //{
                //    parent.children[i].bounds = childrenBounds[i];
                //    parent.children[i].containingObjects = new List<ISpatialBody>();
                //   for (int j = 0; j < parent.containingObjects.Count; j++)
                //    {
                //        parent.containingObjects[i].BoundingBox.Intersects(ref parent.children[i].bounds, out intersects);
                //        if (intersects)
                //        {
                //            parent.children[i].containingObjects.Add(parent.containingObjects[i]);
                //        }
                //    }
                //    this.BuildTree(ref parent.children[i]);
                //}
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

        public uint GetCubeoidId(Vector3 point)
        {
            CubeNode node = FindLeaf(this.root, ref point);
            if (node == null)
                return uint.MaxValue;
            else
                return node.id;
        }

        public List<ISpatialBody> GetBodies(Ray ray)
        {
            List<ISpatialBody> bodies = new List<ISpatialBody>();
            this.GetBodies(ray, this.root, bodies);

            return bodies;
        }

        private void GetBodies(Ray ray, CubeNode node, List<ISpatialBody> bodies)
        {

        }


        public List<ISpatialBody> GetContainedBodies(uint cubeoidId)
        {
            CubeNode node = this.GetCubeNodeById(cubeoidId, this.root);
            return node.containingObjects;
        }

        public bool TranslateRayToScene(ref Ray ray)
        {
            float? result;
            this.root.bounds.Intersects(ref ray, out result);
            if (result.HasValue)
            {
                ray.Position += ray.Direction * (result.Value + 0.001f);
                return true;
            }
            else
            {
                return false;
            }

        }

        CubeNode GetCubeNodeById(uint id, CubeNode node)
        {
            if (node.id == id)
            {
                return node;
            }
            else if(node.children != null)
            {
                for (int i = 0; i < node.children.Length; i++)
                {
                    CubeNode result = GetCubeNodeById(id, node.children[i]);
                    if (result != null)
                        return result;
                }
                return null;
            }
            else
            {
                return null;
            }
        }

        public List<ISpatialBody> GetPossibleIntersections(Vector3 point)
        {
            CubeNode node = FindLeaf(this.root, ref point);
            return node.containingObjects;
        }

        CubeNode FindLeaf(CubeNode node, ref Vector3 point)
        {
            if (node.children == null)
            {

                if (node.bounds.Contains(point) != ContainmentType.Disjoint)
                    return node;
                else
                    return null;
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
                        return FindLeaf(node.children[7], ref point); // 7: Max X, Max Y, Max Z
                    }
                    else
                    {
                        return FindLeaf(node.children[6], ref point); // 6: Max X, Max Y, Min Z
                    }
                }
                else
                {
                    if (greaterZ)
                    {
                        return FindLeaf(node.children[5], ref point); // 5: Max X, Min Y, Max Z
                    }
                    else
                    {
                        return FindLeaf(node.children[4], ref point); // 4: Max X, Min Y, Min Z
                    }
                }
            }
            else
            {
                if (greaterY)
                {
                    if (greaterZ)
                    {
                        return FindLeaf(node.children[3], ref point); // 3: Min X, Max Y, Max Z
                    }
                    else
                    {
                        return FindLeaf(node.children[2], ref point); // 2: Min X, Max Y, Min Z
                    }
                }
                else
                {
                    if (greaterZ)
                    {
                        return FindLeaf(node.children[1], ref point); // 1: Min X, Min Y, Max Z
                    }
                    else
                    {
                        return FindLeaf(node.children[0], ref point); // 0: Min X, Min Y, Min Z
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

        private class RayIntersectResult
        {
            public ISpatialBody body;
            public float result;
        }

        public List<ISpatialBody> GetIntersectedBodies(ref Ray ray)
        {
            List<RayIntersectResult> bodies = new List<RayIntersectResult>();
            
            this.GetIntersectedBodies(ref ray, this.root, bodies);
            if (bodies.Count == 0)
                return null;

            RayIntersectResult best = bodies.OrderBy(x => x.result).First();

            return bodies.Where(x => x.body.BoundingBox.Intersects(best.body.BoundingBox)).OrderBy(x => x.result).Select(x => x.body).ToList();
        }

        private void GetIntersectedBodies(ref Ray ray, CubeNode currentNode, List<RayIntersectResult> bodies)
        {
            float? result;
            currentNode.bounds.Intersects(ref ray, out result);
            if(!result.HasValue)
                return;
            else
            {
                if (currentNode.children == null)
                {
                    for (int i = 0; i < currentNode.containingObjects.Count; i++)
                    {
                        Ray transformedRay;
                        SceneObject obj = (SceneObject)currentNode.containingObjects[i];
                        Matrix inverseWorld = obj.InverseWorld;

                        Vector3.Transform(ref ray.Position, ref inverseWorld, out transformedRay.Position);
                        Vector3.Transform(ref ray.Direction, ref inverseWorld, out transformedRay.Direction);
                        ray.Direction.Normalize();

                        float? objectResult;
                        obj.BoundingBox.Intersects(ref ray, out objectResult);
                        if (objectResult.HasValue)
                            bodies.Add(new RayIntersectResult() { body = currentNode.containingObjects[i], result = objectResult.Value });
                    }
                }
                else
                {
                    for (int i = 0; i < 8; i++)
                    {
                        this.GetIntersectedBodies(ref ray, currentNode.children[i], bodies);
                    }
                }
            }
        }

        private class RayIntersectResultCube
        {
            public CubeNode cubeoid;
            public float result;
        }


        public bool GetRayIntersection(ref Ray ray, out Triangle? triangle, out float? u, out float? v)
        {
            triangle = null;
            u = v = 0;

            SortedDictionary<float, CubeNode> cubeoids = new SortedDictionary<float, CubeNode>();
            this.GetRayCubeNodeIntersections(ref ray, this.root, cubeoids);
            if (cubeoids.Count == 0)
                return false;

            List<CubeNode> intersectedCubeoids = cubeoids.Values.ToList();

            int cubeoidIndex = 0;

            float minDistance = float.MaxValue;
            float intersectionU = 0;
            float intersectionV = 0;
            Triangle? intersectedTriangle = null;
            bool intersectionFound = false;
            while (!intersectionFound && cubeoidIndex < intersectedCubeoids.Count)
            {
                List<ISpatialBody> objects = intersectedCubeoids[cubeoidIndex++].containingObjects;
                for (int i = 0; i < objects.Count; i++)
                {
                    SceneObject sceneObject = (SceneObject)objects[i];
                    Matrix inverseWorld = sceneObject.InverseWorld;
                    Ray transformedRay;
                    Vector3.Transform(ref ray.Position, ref inverseWorld, out transformedRay.Position);
                    Vector3.Transform(ref ray.Direction, ref inverseWorld, out transformedRay.Direction);
                    transformedRay.Direction.Normalize();

                    if (sceneObject.RayIntersects(ray))
                    {
                        List<Triangle> triangles = sceneObject.GetTriangles();
                        for (int j = 0; j < triangles.Count; j++)
                        {
                            float currentU, currentV, distance;
                            if (transformedRay.IntersectsTriangle(triangles[j], out currentU, out currentV, out distance) && 
                                distance < minDistance)
                            {
                                minDistance = distance;
                                intersectionU = currentU;
                                intersectionV = currentV;
                                intersectedTriangle = triangles[j];

                                // Signal that intersection was found. Remaining objects in this cubeoid will be examined, but no more cubeoids.
                                intersectionFound = true;
                            }
                        }
                    }
                }
            }

            if (intersectionFound)
            {
                triangle = intersectedTriangle;
                u = intersectionU;
                v = intersectionV;
            }

            return intersectionFound;
        }

        private void GetRayCubeNodeIntersections(ref Ray ray, CubeNode current, SortedDictionary<float, CubeNode> cubeoids)
        {
            float? result;
            current.bounds.Intersects(ref ray, out result);
            if (result.HasValue)
            {
                if (current.children != null)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        GetRayCubeNodeIntersections(ref ray, current.children[i], cubeoids);
                    }
                }
                else
                {
                    cubeoids.Add(result.Value, current);
                }
            }
        }

        
    }
}
