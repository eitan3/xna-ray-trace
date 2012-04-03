using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RayTraceProject
{
    class DrawableBox
    {
        public static BasicEffect effect = null;
        static RasterizerState rasterState;
        VertexPositionColor[] vertices;
        int[] indices;
        private BoundingBox box;
        public BoundingBox Box
        {
            get { return this.box;}
            set
            {
                this.box = value;
                this.updateVerticeData();
            }

        }


        public DrawableBox(GraphicsDevice device, BoundingBox box)
        {
            this.vertices = new VertexPositionColor[8];
            this.indices = new int[]
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

            this.Box = box;

            if (DrawableBox.effect == null)
            {
                DrawableBox.effect = new BasicEffect(device);
            }

            if (DrawableBox.rasterState == null)
            {
                DrawableBox.rasterState = new RasterizerState();
                DrawableBox.rasterState.CullMode = CullMode.CullCounterClockwiseFace;
                DrawableBox.rasterState.FillMode = FillMode.WireFrame;
            }
        }

        public void UpdateFromBoundingBox(BoundingBox box)
        {

        }

        private void updateVerticeData()
        {
            this.vertices[0].Position = new Vector3(this.Box.Min.X, this.Box.Min.Y, this.Box.Min.Z);
            this.vertices[1].Position = new Vector3(this.Box.Max.X, this.Box.Min.Y, this.Box.Min.Z);
            this.vertices[2].Position = new Vector3(this.Box.Max.X, this.Box.Max.Y, this.Box.Min.Z);
            this.vertices[3].Position = new Vector3(this.Box.Min.X, this.Box.Max.Y, this.Box.Min.Z);
            this.vertices[4].Position = new Vector3(this.Box.Min.X, this.Box.Min.Y, this.Box.Max.Z);
            this.vertices[5].Position = new Vector3(this.Box.Max.X, this.Box.Min.Y, this.Box.Max.Z);
            this.vertices[6].Position = new Vector3(this.Box.Max.X, this.Box.Max.Y, this.Box.Max.Z);
            this.vertices[7].Position = new Vector3(this.Box.Min.X, this.Box.Max.Y, this.Box.Max.Z);
        }

        public void Draw(GraphicsDevice device, ref Matrix view, ref Matrix proj, ref Matrix world)
        {
            RasterizerState oldRasterState = device.RasterizerState;
            device.RasterizerState = DrawableBox.rasterState;
            SceneObject.boundingEffect.World = world;
            SceneObject.boundingEffect.View = view;
            SceneObject.boundingEffect.Projection = proj;
            SceneObject.boundingEffect.DiffuseColor = Vector3.One;
            SceneObject.boundingEffect.CurrentTechnique.Passes[0].Apply();
            device.DrawUserIndexedPrimitives<VertexPositionColor>(PrimitiveType.TriangleList, this.vertices, 0, this.vertices.Length, this.indices, 0, 12);
            device.RasterizerState = oldRasterState;
        }
    }
}
