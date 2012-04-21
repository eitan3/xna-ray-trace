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
using System.ComponentModel;

namespace RayTracePipeline
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to apply custom processing to content data, converting an object of
    /// type TInput to TOutput. The input and output types may be the same if
    /// the processor wishes to alter data without changing its type.
    ///
    /// This should be part of a Content Pipeline Extension Library project.
    /// </summary>
    [ContentProcessor(DisplayName = "TracerModelProcessor")]
    public class TracerModelProcessor : ModelProcessor
    {
        private Material material;
        private bool interpolateNormals = true;
        List<Mesh> meshes;
        [DisplayName("Diffuse color")]
        [Description("The diffuse color of the material, if no texture is used.")]
        [DefaultValue("0;255;0;255")]
        public Color DiffuseColor
        {
            get;
            set;
        }

        [DisplayName("Texture Filepath")]
        [Description("Filepath to the texture to be used as a diffuse map")]
        public string TextureFilePath
        {
            get;
            set;
        }

        [DisplayName("Use Texture")]
        [Description("Determines if a texture should be used when rendering this model")]
        [DefaultValue(false)]
        public bool UseTexture
        {
            get;
            set;
        }

        [DisplayName("Reflectiveness")]
        [Description("A value between 0 to 1 denoting how reflective the material is")]
        [DefaultValue(0.5f)]
        public float Reflectiveness
        {
            get;
            set;
        }

        [DisplayName("Interpolate normals")]
        [Description("Determines if surface normals should be interpolated or not.")]
        [DefaultValue(true)]
        public bool InterpolateNormals
        {
            get { return interpolateNormals; }
            set { this.interpolateNormals = value; }
        }

        [DisplayName("Refraction Index")]
        [Description("Specifies the refraction index relative of a material relative to that of vacuum.")]
        [DefaultValue(1.33f)] // Water
        public float RefractionIndex
        {
            get;
            set;
        }

        [DisplayName("Transparent")]
        [Description("Determines if the material is transparent or not.")]
        [DefaultValue(false)]
        public bool Transparent
        {
            get;
            set;
        }

        [DisplayName("Use Vertex Colors")]
        [Description("Specifies if colors from the vertex color channel should be used, if it exists.")]
        [DefaultValue(false)]
        public bool UseVertexColors
        {
            get;
            set;
        }

        

        public override ModelContent Process(NodeContent input, ContentProcessorContext context)
        {
            ModelContent content = base.Process(input, context);
            this.CreateMaterial();

            this.meshes = new List<Mesh>();
            this.FindVertices(input);

            Dictionary<string, object> contentData = new Dictionary<string, object>();

            contentData.Add("Meshes", this.meshes);

            content.Tag = contentData;
            return content;
        }

        private void CreateMaterial()
        {
            
            this.material = new Material(this.Reflectiveness, this.UseTexture, this.Transparent, this.RefractionIndex, this.TextureFilePath);
            this.material.InterpolateNormals = this.InterpolateNormals;
            if (this.UseTexture)
            {
                RayTracerTexture texture = new RayTracerTexture(this.TextureFilePath);
                this.material.Texture = texture;
            }
        }

        private void FindVertices(NodeContent node)
        {
            MeshContent mesh = node as MeshContent;
            if (mesh != null)
            {
                List<Triangle> triangles = new List<Triangle>();

                Matrix absoluteTransform = mesh.AbsoluteTransform;
                Matrix absoluteTransformInvertTranspose = Matrix.Transpose(Matrix.Invert(absoluteTransform));
                foreach (GeometryContent geometry in mesh.Geometry)
                {
                    VertexChannel texCoords = null;
                    if (geometry.Vertices.Channels.Contains(VertexChannelNames.TextureCoordinate(0)))
                    {
                        texCoords = geometry.Vertices.Channels[VertexChannelNames.TextureCoordinate(0)];
                    }
                    else if (this.UseTexture)
                    {
                        throw new InvalidContentException("Model is built with UseTexure but does not contain any TextureCoordinates.");
                    }

                    VertexChannel normals = null;
                    if(geometry.Vertices.Channels.Contains(VertexChannelNames.Normal(0)))
                    {
                        normals = geometry.Vertices.Channels[VertexChannelNames.Normal(0)];
                    }
                    
                    VertexChannel colors = null;
                    if (geometry.Vertices.Channels.Contains(VertexChannelNames.Color(0)))
                    {
                        colors = geometry.Vertices.Channels[VertexChannelNames.Color(0)];
                    }

                    BasicMaterialContent basicMaterial = geometry.Material as BasicMaterialContent;
                    int triangleIndex = 0;
                    for (int i = 0; i < geometry.Indices.Count; i += 3)
                    {
                        Vector3 v1 = geometry.Vertices.Positions[geometry.Indices[i]];
                        Vector3 v2 = geometry.Vertices.Positions[geometry.Indices[i + 1]];
                        Vector3 v3 = geometry.Vertices.Positions[geometry.Indices[i + 2]];

                        Vector3 n1 = (Vector3)normals[geometry.Indices[i]];
                        Vector3 n2 = (Vector3)normals[geometry.Indices[i + 1]];
                        Vector3 n3 = (Vector3)normals[geometry.Indices[i + 2]];

                        Vector3.Transform(ref v1, ref absoluteTransform, out v1);
                        Vector3.Transform(ref v2, ref absoluteTransform, out v2);
                        Vector3.Transform(ref v3, ref absoluteTransform, out v3);

                        // I have no idea why I have to do this. If I do not, the Z and Y components of all normals are "switched" around.
                        //Vector3.Transform(ref n1, ref normalTransform, out n1);
                        //Vector3.Transform(ref n2, ref normalTransform, out n2);
                        //Vector3.Transform(ref n3, ref normalTransform, out n3);

                        // I am suspecting that what I suspected above is wrong. I mustve confused myself with weird models.
                        // It seems like the normals should be transformed by the absoluteTransfom;
                        //---
                        // SCRATCH THAT. The normals should be transformed by the transpose of the inverse of the absolute transform :D
                        Vector3.Transform(ref n1, ref absoluteTransformInvertTranspose, out n1);
                        Vector3.Transform(ref n2, ref absoluteTransformInvertTranspose, out n2);
                        Vector3.Transform(ref n3, ref absoluteTransformInvertTranspose, out n3);

                        n1.Normalize();
                        n2.Normalize();
                        n3.Normalize();

                        Vector3 edge1, edge2, surfaceNormal;
                        Vector3.Subtract(ref v2, ref v1, out edge1);
                        Vector3.Subtract(ref v3, ref v1, out edge2);
                        Vector3.Cross(ref edge2, ref edge1, out surfaceNormal);
                        surfaceNormal.Normalize();

                        Triangle triangle = new Triangle();
                        triangle.v1 = v1;
                        triangle.v2 = v2;
                        triangle.v3 = v3;
                        if (texCoords != null)
                        {
                            triangle.uv1 = (Vector2)texCoords[geometry.Indices[i]];
                            triangle.uv2 = (Vector2)texCoords[geometry.Indices[i + 1]];
                            triangle.uv3 = (Vector2)texCoords[geometry.Indices[i + 2]];
                        }
                        triangle.n1 = n1;
                        triangle.n2 = n2;
                        triangle.n3 = n3;
                        triangle.i1 = geometry.Indices[i];
                        triangle.i2 = geometry.Indices[i + 1];
                        triangle.i3 = geometry.Indices[i + 2];
                        triangle.id = triangleIndex++;
                        triangle.surfaceNormal = surfaceNormal;

                        if (this.UseVertexColors && colors != null)
                            triangle.color = ((Color)colors[geometry.Indices[i]]).ToVector4();
                        else
                            triangle.color = this.DiffuseColor.ToVector4();
                        triangles.Add(triangle);
                    }
                    
                }

                BoundingBox box = this.CreateBoundingBox(triangles);
                Mesh tracerMesh = new Mesh(triangles.ToArray(), this.material, box);
                this.meshes.Add(tracerMesh);
            }

            foreach (NodeContent child in node.Children)
            {
                this.FindVertices(child);
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