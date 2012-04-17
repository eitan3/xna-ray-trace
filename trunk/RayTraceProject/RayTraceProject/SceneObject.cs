using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RayTracerTypeLibrary;
using System.ComponentModel;

namespace RayTraceProject
{
    public class SceneObject : Spatial.ISpatialBody
    {
        List<Mesh> meshes;
        BoundingBox boundingBox;
        BoundingBox worldBoundingBox;
        Vector3 position;
        Vector3 rotation;
        Vector3 scale = Vector3.One;
        Model model;
        bool worldIsDirty = true;
        Matrix world;
        Matrix inverseWorld;

        DrawableBox box;

#if DEBUG
        public static BasicEffect boundingEffect = null;
        static RasterizerState boundingRasterState;
        VertexPositionColor[] boundingVertices;
        int[] boundingIndices;
#endif

        public List<Mesh> Meshes { get { return this.meshes; } }

        public BoundingBox WorldBoundingBox
        {
            get
            {
                if (this.worldIsDirty)
                    this.BuildWorld();
                return this.worldBoundingBox;
            }
        }

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

        public Vector3 Scale
        {
            get { return this.scale; }
            set
            {
                if (this.scale != value)
                {
                    this.scale = value;
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

        public string Name
        {
            get;
            set;
        }

        public SceneObject(GraphicsDevice device, Model model, Vector3 pos, Vector3 rot)
        {
            this.model = model;
            this.position = pos;
            this.rotation = rot;

            if (model.Tag == null || !(model.Tag is Dictionary<string, object>))
                throw new ArgumentException("Model must be built using the TracerModelProcessor");

            Dictionary<string, object> modelData = (Dictionary<string, object>)model.Tag;
            this.meshes = (List<Mesh>)modelData["Meshes"];

            for (int i = 0; i < this.meshes.Count; i++)
            {
                this.boundingBox = BoundingBox.CreateMerged(this.boundingBox, this.meshes[i].MeshBoundingBox);
                this.meshes[i].MeshMaterial.Init();
            }

            this.box = new DrawableBox(device, this.boundingBox);
            //var foo = this.triangles.Where(x => x.convexGeometry);

#if DEBUG
            if(SceneObject.boundingEffect == null)
                SceneObject.boundingEffect = new BasicEffect(device);
            if (SceneObject.boundingRasterState == null)
            {
                SceneObject.boundingRasterState = new RasterizerState();
                SceneObject.boundingRasterState.CullMode = CullMode.CullCounterClockwiseFace;
                SceneObject.boundingRasterState.FillMode = FillMode.WireFrame;
            }
            this.boundingVertices = new VertexPositionColor[]
                {
                    new VertexPositionColor(new Vector3(this.boundingBox.Min.X, this.boundingBox.Min.Y, this.boundingBox.Min.Z), Color.White),
                    new VertexPositionColor(new Vector3(this.boundingBox.Max.X, this.boundingBox.Min.Y, this.boundingBox.Min.Z), Color.White),
                    new VertexPositionColor(new Vector3(this.boundingBox.Max.X, this.boundingBox.Max.Y, this.boundingBox.Min.Z), Color.White),
                    new VertexPositionColor(new Vector3(this.boundingBox.Min.X, this.boundingBox.Max.Y, this.boundingBox.Min.Z), Color.White),
                    new VertexPositionColor(new Vector3(this.boundingBox.Min.X, this.boundingBox.Min.Y, this.boundingBox.Max.Z), Color.White),
                    new VertexPositionColor(new Vector3(this.boundingBox.Max.X, this.boundingBox.Min.Y, this.boundingBox.Max.Z), Color.White),
                    new VertexPositionColor(new Vector3(this.boundingBox.Max.X, this.boundingBox.Max.Y, this.boundingBox.Max.Z), Color.White),
                    new VertexPositionColor(new Vector3(this.boundingBox.Min.X, this.boundingBox.Max.Y, this.boundingBox.Max.Z), Color.White)
                };
            this.boundingIndices = new int[]
                {
                    // Back face
                    2, 3, 1,
                    3, 0, 1,
                    // Right face
                    6, 2, 5,
                    2, 1, 5,
                    // Front face
                    7, 6, 4,
                    6, 5, 4,
                    // Left face
                    3, 7, 0,
                    7, 4, 0,
                    // Top face
                    3, 2, 7,
                    2, 6, 7,
                    // Bottom face
                    4, 5, 0,
                    5, 1, 0
                };
#endif
        }

        private void BuildWorld()
        {
            this.worldIsDirty = false;

            Matrix scaleMatrix = Matrix.CreateScale(this.scale);
            Matrix rotationMatrix = Matrix.CreateRotationX(this.rotation.X) * 
                Matrix.CreateRotationY(this.rotation.Y) * 
                Matrix.CreateRotationZ(this.rotation.Z);
            Matrix translationMatrix = Matrix.CreateTranslation(this.position);

            this.world = scaleMatrix * rotationMatrix * translationMatrix;

            Vector3.Transform(ref this.boundingBox.Max, ref this.world, out this.worldBoundingBox.Max);
            Vector3.Transform(ref this.boundingBox.Min, ref this.world, out this.worldBoundingBox.Min);

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
                    effect.PreferPerPixelLighting = true;
                    effect.World =  boneTransforms[this.model.Meshes[i].ParentBone.Index] * this.World;
                    effect.Projection = proj;
                    effect.View = view;
                }
                this.model.Meshes[i].Draw();
            }

            //VertexPositionNormalTexture[] verts = new VertexPositionNormalTexture[this.triangles.Count * 3];
            //for (int i = 0; i < this.triangles.Count; i++)
            //{
            //    verts[i * 3] = new VertexPositionNormalTexture(this.triangles[i].v1, Vector3.Up, Vector2.Zero);
            //    verts[(i * 3) + 1] = new VertexPositionNormalTexture(this.triangles[i].v2, Vector3.Up, Vector2.UnitX);
            //    verts[(i * 3) + 2] = new VertexPositionNormalTexture(this.triangles[i].v3, Vector3.Up, Vector2.One);
            //}

#if DEBUG
            this.box.Draw(device, ref view, ref proj, ref this.world);
            //RasterizerState oldRasterState = device.RasterizerState;
            //device.RasterizerState = SceneObject.boundingRasterState;
            //SceneObject.boundingEffect.World = this.world;
            //SceneObject.boundingEffect.View = view;
            //SceneObject.boundingEffect.Projection = proj;
            //SceneObject.boundingEffect.DiffuseColor = Vector3.One;
            //boundingEffect.VertexColorEnabled = true;
            //SceneObject.boundingEffect.CurrentTechnique.Passes[0].Apply();
            
            //device.DrawUserIndexedPrimitives<VertexPositionColor>(PrimitiveType.TriangleList, this.boundingVertices, 0, this.boundingVertices.Length, this.boundingIndices, 0, 12);
            //device.RasterizerState = oldRasterState;
#endif
            //eff.EnableDefaultLighting();
            //eff.CurrentTechnique.Passes[0].Apply();
            ////device.DrawUserPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList, verts, 0, vertices.Count, VertexPositionNormalTexture.VertexDeclaration);
            //this.boundingBox.Draw(device);
        }

        public List<Triangle> GetTriangles()
        {
            return null; // this.triangles;
        }

        public bool RayIntersects(ref Ray ray)
        {
            float? distance;
            BoundingBox.Intersects(ref ray, out distance);
            return distance.HasValue;
        }

        //public bool GetIntersectingFaceNormal(Ray ray, out Vector3 normal, out float? distance)
        //{
        //    bool intersects = false;
        //    normal = Vector3.Zero;
        //    distance = null;

        //    ray.Position = Vector3.Transform(ray.Position, this.inverseWorld);
        //    ray.Direction = Vector3.Transform(ray.Direction, this.inverseWorld);

        //    if (this.boundingBox.Intersects(ray) == null)
        //    {
        //        intersects = false;
        //    }
        //    else
        //    {
        //        float? currentDistance = float.NaN;
        //        float minDistance = float.MaxValue;
        //        Vector3 minVector1, minVector2, minVector3;
        //        Vector3 vector1, vector2, vector3;
        //        vector1 = vector2 = vector3 = Vector3.Zero;

        //        for (int i = 0; i < this.triangles.Count; i++)
        //        {
        //            Triangle triangle = this.triangles[i];

        //            //RayTriangleIntesercts(ray, triangle);
        //            //RayIntersectsTriangle(ref ray, ref vector1, ref vector2, ref vector3, out currentDistance);
        //            if (distance != null && minDistance > currentDistance.Value)
        //            {
        //                intersects = true;
        //                minDistance = currentDistance.Value;

        //                minVector1 = vector1;
        //                minVector2 = vector2;
        //                minVector3 = vector3;
        //            }

        //        }

        //        if (intersects)
        //        {
        //            normal = Vector3.Cross(vector2 - vector1, vector3 - vector1);
        //            distance = minDistance;
        //        }
        //    }

        //    return intersects;
        //}

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
