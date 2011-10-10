using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

// TODO: replace these with the processor input and output types.
using TInput = System.String;
using TOutput = System.String;
using RayTracerTypeLibrary;

namespace RayTracePipeline
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to apply custom processing to content data, converting an object of
    /// type TInput to TOutput. The input and output types may be the same if
    /// the processor wishes to alter data without changing its type.
    ///
    /// This should be part of a Content Pipeline Extension Library project.
    ///
    /// TODO: change the ContentProcessor attribute to specify the correct
    /// display name for this processor.
    /// </summary>
    [ContentProcessor(DisplayName = "TracerModelProcessor")]
    public class TracerModelProcessor : ModelProcessor
    {
        static int counter = 0;
        Color c;
        public override ModelContent Process(NodeContent input, ContentProcessorContext context)
        {
            Random rand = new Random();
            c = new Color(0, rand.Next(0, 256), 0);
            //System.Diagnostics.Debugger.Launch();
            ModelContent content = base.Process(input, context);
            

            List<Triangle> triangles = new List<Triangle>();
            this.FindVertices(input, triangles);

            BoundingBox boundingBox = this.CreateBoundingBox(triangles);

            Dictionary<string, object> contentData = new Dictionary<string, object>();

            contentData.Add("vertices", triangles);
            contentData.Add("box", boundingBox);

            content.Tag = contentData;
            counter++;
            return content;
        }

        private void FindVertices(NodeContent node, List<Triangle> triangles)
        {
           
            MeshContent mesh = node as MeshContent;
            if (mesh != null)
            {
                //System.Diagnostics.Debugger.Launch();
                Matrix absoluteTransform = mesh.AbsoluteTransform;
                foreach (GeometryContent geometry in mesh.Geometry)
                {
                    List<Vector2> texCoords = new List<Vector2>();
                    if (geometry.Vertices.Channels.Contains(VertexChannelNames.TextureCoordinate(0)))
                    {
                        VertexChannel channel = geometry.Vertices.Channels[VertexChannelNames.TextureCoordinate(0)];
                        foreach (Vector2 v in channel)
                        {
                            texCoords.Add(new Vector2(v.X, v.Y));
                        }
                    }

                    List<Vector3> colors = new List<Vector3>();
                    if (geometry.Vertices.Channels.Contains(VertexChannelNames.Color(0)))
                    {
                        VertexChannel channel = geometry.Vertices.Channels[VertexChannelNames.Color(0)];
                        foreach(Vector3 color in channel)
                        {
                            colors.Add(color);
                        }
                    }

                    List<Vector3> normals = new List<Vector3>();
                    if(geometry.Vertices.Channels.Contains(VertexChannelNames.Normal(0)))
                    {
                        VertexChannel channel = geometry.Vertices.Channels[VertexChannelNames.Normal(0)];
                        foreach(Vector3 normal in channel)
                        {
                            normals.Add(normal);
                        }
                    }

                    BasicMaterialContent basicMaterial = geometry.Material as BasicMaterialContent;
                    Matrix normalTransform = Matrix.CreateRotationX(MathHelper.PiOver2) * Matrix.CreateRotationZ(MathHelper.Pi);
                    for (int i = 0; i < geometry.Indices.Count; i += 3)
                    {
                        Vector3 v1 = geometry.Vertices.Positions[geometry.Indices[i]];
                        Vector3 v2 = geometry.Vertices.Positions[geometry.Indices[i + 1]];
                        Vector3 v3 = geometry.Vertices.Positions[geometry.Indices[i + 2]];

                        Vector3 n1 = normals[geometry.Indices[i]];
                        Vector3 n2 = normals[geometry.Indices[i + 1]];
                        Vector3 n3 = normals[geometry.Indices[i + 2]];

                        Vector3.Transform(ref v1, ref absoluteTransform, out v1);
                        Vector3.Transform(ref v2, ref absoluteTransform, out v2);
                        Vector3.Transform(ref v3, ref absoluteTransform, out v3);

                        // I have no idea why I have to do this. If I do not, the Z and Y components of all normals are "switched" around.
                        Vector3.Transform(ref n1, ref normalTransform, out n1);
                        Vector3.Transform(ref n2, ref normalTransform, out n2);
                        Vector3.Transform(ref n3, ref normalTransform, out n3);

                        n1.Normalize();
                        n2.Normalize();
                        n3.Normalize();

                        Triangle triangle = new Triangle();
                        triangle.v1 = v1;
                        triangle.v2 = v2;
                        triangle.v3 = v3;
                        triangle.uv1 = texCoords[geometry.Indices[i]];
                        triangle.uv2 = texCoords[geometry.Indices[i + 1]];
                        triangle.uv2 = texCoords[geometry.Indices[i + 2]];
                        triangle.n1 = n1;
                        triangle.n2 = n2;
                        triangle.n3 = n3;
                        triangle.id = counter;
                        triangles.Add(triangle);
                    }
                    
                }
            }

            foreach (NodeContent child in node.Children)
            {
                this.FindVertices(child, triangles);
            }
        }

        private BoundingBox CreateBoundingBox(List<Triangle> triangles)
        {
            BoundingBox box = new BoundingBox();
            for (int i = 0; i < triangles.Count; i++)
            {
                if (triangles[i].v1.X < box.Min.X)
                    box.Min.X = triangles[i].v1.X;

                if (triangles[i].v1.Y < box.Min.Y)
                    box.Min.Y = triangles[i].v1.Y;

                if (triangles[i].v1.Z < box.Min.Z)
                    box.Min.Z = triangles[i].v1.Z;

                if (triangles[i].v1.X > box.Max.X)
                    box.Max.X = triangles[i].v1.X;

                if (triangles[i].v1.Y > box.Max.Y)
                    box.Max.Y = triangles[i].v1.Y;

                if (triangles[i].v1.Z > box.Max.Z)
                    box.Max.Z = triangles[i].v1.Z;


                if (triangles[i].v2.X < box.Min.X)
                    box.Min.X = triangles[i].v2.X;

                if (triangles[i].v2.Y < box.Min.Y)
                    box.Min.Y = triangles[i].v2.Y;

                if (triangles[i].v2.Z < box.Min.Z)
                    box.Min.Z = triangles[i].v2.Z;

                if (triangles[i].v2.X > box.Max.X)
                    box.Max.X = triangles[i].v2.X;

                if (triangles[i].v2.Y > box.Max.Y)
                    box.Max.Y = triangles[i].v2.Y;

                if (triangles[i].v2.Z > box.Max.Z)
                    box.Max.Z = triangles[i].v2.Z;


                if (triangles[i].v3.X < box.Min.X)
                    box.Min.X = triangles[i].v3.X;

                if (triangles[i].v3.Y < box.Min.Y)
                    box.Min.Y = triangles[i].v3.Y;

                if (triangles[i].v3.Z < box.Min.Z)
                    box.Min.Z = triangles[i].v3.Z;

                if (triangles[i].v3.X > box.Max.X)
                    box.Max.X = triangles[i].v3.X;

                if (triangles[i].v3.Y > box.Max.Y)
                    box.Max.Y = triangles[i].v3.Y;

                if (triangles[i].v3.Z > box.Max.Z)
                    box.Max.Z = triangles[i].v3.Z;
            }

            return box;
        }
    }


}