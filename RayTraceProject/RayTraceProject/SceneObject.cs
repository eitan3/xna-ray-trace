using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RayTracerTypeLibrary;

namespace RayTraceProject
{
    class SceneObject : Spatial.ISpatialBody
    {
        List<Triangle> vertices;
        BoundingBox boundingBox;
        Vector3 position;
        Vector3 rotation;
        Model model;
        bool worldIsDirty = true;
        Matrix world;
        Matrix inverseWorld;

        public BoundingBox BoundingBox
        {
            get { return this.boundingBox; }
            set { this.boundingBox = value; }
        }

        public Vector3 Position
        {
            get { return this.position; }
            set
            {
                if (this.position != value)
                {
                    this.position = value;
                    this.worldIsDirty = true;
                }
            }
        }

        public Vector3 Rotation
        {
            get { return this.rotation; }
            set 
            {
                if (this.rotation != value)
                {
                    this.rotation = value;
                    this.worldIsDirty = true;
                }
            }
        }

        public Matrix World
        {
            get
            {
                if (this.worldIsDirty)
                    this.BuildWorld();
                return this.world;
            }
        }

        public Matrix InverseWorld
        {
            get
            {
                if (this.worldIsDirty)
                    this.BuildWorld();
                return this.inverseWorld;
            }
        }

        public SceneObject(Model model, Vector3 pos, Vector3 rot)
        {
            this.model = model;
            this.position = pos;
            this.rotation = rot;

            if (model.Tag == null || !(model.Tag is Dictionary<string, object>))
                throw new ArgumentException("Model must be built using the TracerModelProcessor");

            Dictionary<string, object> modelData = (Dictionary<string, object>)model.Tag;
            this.vertices = (List<Triangle>)modelData["vertices"];
            this.boundingBox = (BoundingBox)modelData["box"];

        }

        private void BuildWorld()
        {
            this.worldIsDirty = false;

            Matrix scaleMatrix = Matrix.Identity;
            Matrix rotationMatrix = Matrix.CreateRotationX(this.rotation.X) * 
                Matrix.CreateRotationY(this.rotation.Y) * 
                Matrix.CreateRotationZ(this.rotation.Z);
            Matrix translationMatrix = Matrix.CreateTranslation(this.position);

            this.world = scaleMatrix * rotationMatrix * translationMatrix;
            //Vector3.Transform(ref this.boundingBox.Max, ref this.world, out this.boundingBox.Max);
            //Vector3.Transform(ref this.boundingBox.Min, ref this.world, out this.boundingBox.Min);

            Matrix.Invert(ref this.world, out this.inverseWorld);
        }

        public void Draw(ref Matrix view, ref Matrix proj, GraphicsDevice device, GameTime gameTime)
        {
            Matrix[] boneTransforms = new Matrix[this.model.Bones.Count];
            this.model.CopyAbsoluteBoneTransformsTo(boneTransforms);

            for (int i = 0; i < this.model.Meshes.Count; i++)
            {
                for(int j = 0; j < this.model.Meshes[i].MeshParts.Count; j++)
                {
                    BasicEffect effect = this.model.Meshes[i].MeshParts[j].Effect as BasicEffect;
                    effect.EnableDefaultLighting();
                    effect.World =  boneTransforms[this.model.Meshes[i].ParentBone.Index] * this.World;
                    effect.Projection = proj;
                    effect.View = view;
                }
                this.model.Meshes[i].Draw();
            }

            VertexPositionNormalTexture[] verts = new VertexPositionNormalTexture[this.vertices.Count * 3];
            for (int i = 0; i < this.vertices.Count; i++)
            {
                verts[i * 3] = new VertexPositionNormalTexture(this.vertices[i].v1, Vector3.Up, Vector2.Zero);
                verts[(i * 3) + 1] = new VertexPositionNormalTexture(this.vertices[i].v2, Vector3.Up, Vector2.UnitX);
                verts[(i * 3) + 2] = new VertexPositionNormalTexture(this.vertices[i].v3, Vector3.Up, Vector2.One);
            }

            if(eff == null)
                eff = new BasicEffect(device);
            eff.World = this.world;
            eff.Projection = proj;
            eff.View = view;
            eff.CurrentTechnique.Passes[0].Apply();
            //device.DrawUserPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList, verts, 0, vertices.Count, VertexPositionNormalTexture.VertexDeclaration);
        }
        BasicEffect eff = null;

        public List<Triangle> GetTriangles()
        {
            return this.vertices;
        }

        public bool RayIntersects(Ray ray)
        {
            return this.BoundingBox.Intersects(ray).HasValue;
        }

        public bool GetIntersectingFaceNormal(Ray ray, out Vector3 normal, out float? distance)
        {
            bool intersects = false;
            normal = Vector3.Zero;
            distance = null;

            ray.Position = Vector3.Transform(ray.Position, this.inverseWorld);
            ray.Direction = Vector3.Transform(ray.Direction, this.inverseWorld);

            if (this.boundingBox.Intersects(ray) == null)
            {
                intersects = false;
            }
            else
            {
                float? currentDistance = float.NaN;
                float minDistance = float.MaxValue;
                Vector3 minVector1, minVector2, minVector3;
                Vector3 vector1, vector2, vector3;
                vector1 = vector2 = vector3 = Vector3.Zero;

                for (int i = 0; i < this.vertices.Count; i++)
                {
                    Triangle triangle = this.vertices[i];

                    //RayTriangleIntesercts(ray, triangle);
                    //RayIntersectsTriangle(ref ray, ref vector1, ref vector2, ref vector3, out currentDistance);
                    if (distance != null && minDistance > currentDistance.Value)
                    {
                        intersects = true;
                        minDistance = currentDistance.Value;

                        minVector1 = vector1;
                        minVector2 = vector2;
                        minVector3 = vector3;
                    }

                }

                if (intersects)
                {
                    normal = Vector3.Cross(vector2 - vector1, vector3 - vector1);
                    distance = minDistance;
                }
            }

            return intersects;
        }

        //public static bool RayTriangleIntesercts(Ray ray, Triangle triangle, out float u, out float v, out float distance)
        //{
        //    u = v = distance = 0;
        //    float t;
        //    Vector3 D = ray.Direction;
        //    Vector3 T = ray.Position - triangle.v1;

        //    Vector3 Edge1 = triangle.v2 - triangle.v1;
        //    Vector3 Edge2 = triangle.v3 - triangle.v1;

        //    Vector3 P, Q;
        //    Vector3.Cross(ref D, ref Edge2, out P);
        //    Vector3.Cross(ref T, ref Edge1, out Q);

        //    float row1, row2, row3;
        //    Vector3.Dot(ref Q, ref Edge2, out row1);
        //    Vector3.Dot(ref P, ref T, out row2);
        //    Vector3.Dot(ref Q, ref D, out row3);

        //    var result = (1.0f / Vector3.Dot(P, Edge1)) * new Vector3(row1, row2, row3);
        //    distance = result.X;
        //    u = result.Y;
        //    v = result.Z;

        //    return (u >= 0 &&
        //        v >= 0 &&
        //        distance >= 0 &&
        //        u + v <= 1);
        //}
    }
}
