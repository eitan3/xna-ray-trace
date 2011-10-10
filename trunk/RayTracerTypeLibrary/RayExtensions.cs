﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RayTracerTypeLibrary
{
    public static class RayExtensions
    {
        public static bool IntersectsTriangle(this Ray ray, Triangle triangle, out float u, out float v, out float distance)
        {
            u = v = distance = 0;
            Vector3 D = ray.Direction;
            Vector3 T = ray.Position - triangle.v1;

            Vector3 Edge1 = triangle.v2 - triangle.v1;
            Vector3 Edge2 = triangle.v3 - triangle.v1;

            Vector3 P, Q;
            Vector3.Cross(ref D, ref Edge2, out P);
            Vector3.Cross(ref T, ref Edge1, out Q);

            float row1, row2, row3;
            Vector3.Dot(ref Q, ref Edge2, out row1);
            Vector3.Dot(ref P, ref T, out row2);
            Vector3.Dot(ref Q, ref D, out row3);

            var result = (1.0f / Vector3.Dot(P, Edge1)) * new Vector3(row1, row2, row3);
            distance = result.X;
            u = result.Y;
            v = result.Z;

            return (u >= 0 &&
                v >= 0 &&
                distance >= 0 &&
                u + v <= 1);
        }

        public static void Draw(this BoundingBox box, GraphicsDevice device)
        {
            VertexDeclaration vdec = VertexPositionColor.VertexDeclaration;
            VertexPositionColor[] verts = new VertexPositionColor[]
            {
                new VertexPositionColor(box.Min, Color.Red),
                new VertexPositionColor(new Vector3(box.Max.X, box.Min.Y, box.Min.Z), Color.Red),
                new VertexPositionColor(new Vector3(box.Max.X, box.Max.Y, box.Min.Z), Color.Red),
                new VertexPositionColor(new Vector3(box.Min.X, box.Max.Y, box.Min.Z), Color.Red),
                new VertexPositionColor(box.Max, Color.Red),
                new VertexPositionColor(new Vector3(box.Max.X, box.Min.Y, box.Max.Z), Color.Red),
                new VertexPositionColor(new Vector3(box.Min.X, box.Min.Y, box.Max.Z), Color.Red),
                new VertexPositionColor(new Vector3(box.Min.X, box.Max.Y, box.Max.Z), Color.Red),
            };

            Int16[] indices = new Int16[]
            {
                3, 0, 1,
                2, 3, 1,
                7, 4, 6,
                4, 5, 6
            };

            device.DrawUserIndexedPrimitives<VertexPositionColor>(PrimitiveType.TriangleList, verts, 0, verts.Length, indices, 0, 4, vdec);
        }
    }
}