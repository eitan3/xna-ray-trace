using System;
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

            //float dot;
            //Vector3.Dot(ref triangle.surfaceNormal, ref D, out dot);
            //if (dot > 0)
            //    return false;

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
        //const float EPSILON = 0.0000001f;
        //public static bool IntersectsTriangle(this Ray ray, Triangle triangle, out float u, out float v, out float distance)
        //{
        //    u = v = distance = 0;

        //    Vector3 Edge1 = triangle.v2 - triangle.v1;
        //    Vector3 Edge2 = triangle.v3 - triangle.v1;

        //    Vector3 Q;
        //    Vector3.Cross(ref ray.Direction, ref Edge2, out Q);

        //    float a;
        //    Vector3.Dot(ref Edge1, ref Q, out a);
        //    if (a > -EPSILON && a < EPSILON)
        //        return false;

        //    float f = 1.0f / a;

        //    Vector3 s;
        //    Vector3.Subtract(ref ray.Position, ref triangle.v1, out s);

        //    Vector3.Dot(ref s, ref Q, out u);
        //    u *= f;
        //    if (u < 0.0f)
        //        return false;

        //    Vector3 r;
        //    Vector3.Cross(ref s, ref Edge1, out r);

        //    Vector3.Dot(ref ray.Direction, ref r, out v);
        //    v *= f;
        //    if (v < 0.0f || u + v > 1.0f)
        //        return false;

        //    Vector3.Dot(ref Edge2, ref r, out distance);
        //    distance *= f;
        //    return true;
        //}

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
