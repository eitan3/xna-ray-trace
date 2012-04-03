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
    ///
    /// TODO: change the ContentProcessor attribute to specify the correct
    /// display name for this processor.
    /// </summary>
    [ContentProcessor(DisplayName = "TracerModelProcessor")]
    public class TracerModelProcessor : ModelProcessor
    {
        const float EPSILON = 0.001f;
        private bool interpolateNormals = true;
        List<Mesh> meshes;
        //List<Triangle> triangles;
        List<Triangle> cyclicPreventor;
        Color c;
        static int fleh = 0;
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

        private Material material;

        public override ModelContent Process(NodeContent input, ContentProcessorContext context)
        {
            c = Color.Red;
            
            ModelContent content = base.Process(input, context);
            //System.Diagnostics.Debugger.Launch();
            this.CreateMaterial();

            this.meshes = new List<Mesh>();
            this.FindVertices(input);

            //System.Diagnostics.Debugger.Launch();

            this.cyclicPreventor = new List<Triangle>();

            Dictionary<string, object> contentData = new Dictionary<string, object>();

            //contentData.Add("vertices", triangles);
            //contentData.Add("box", boundingBox);
            //contentData.Add("material", this.material);
            contentData.Add("Meshes", this.meshes);

            content.Tag = contentData;
            return content;
        }

        //private void DetermineConvexGeometry()
        //{
        //    Plane trianglePlane1, trianglePlane2, trianglePlane3;
        //    List<Triangle> trianglesToProcess = new List<Triangle>(this.triangles);
        //    Triangle currentTriangle, compareTriangle;

        //    while (trianglesToProcess.Count > 0)
        //    {
        //        int index = trianglesToProcess.Count - 1;
        //        currentTriangle = trianglesToProcess[index];

        //        this.GetPlaneForVector(currentTriangle.v1, currentTriangle.n1, out trianglePlane1);
        //        this.GetPlaneForVector(currentTriangle.v2, currentTriangle.n2, out trianglePlane2);
        //        this.GetPlaneForVector(currentTriangle.v3, currentTriangle.n3, out trianglePlane3);
                
        //        currentTriangle.convexGeometry = true;
        //        for (int j = index - 1; j >= 0; j--)
        //        {
        //            compareTriangle = trianglesToProcess[j];
        //            if ((this.PlanePointDot(trianglePlane1, currentTriangle.v1, compareTriangle.v1, compareTriangle.n1) - EPSILON > 0.0f) ||
        //                (this.PlanePointDot(trianglePlane1, currentTriangle.v1, compareTriangle.v2, compareTriangle.n2) - EPSILON > 0.0f) ||
        //                (this.PlanePointDot(trianglePlane1, currentTriangle.v1, compareTriangle.v3, compareTriangle.n3) - EPSILON > 0.0f) ||
        //                (this.PlanePointDot(trianglePlane2, currentTriangle.v2, compareTriangle.v1, compareTriangle.n1) - EPSILON > 0.0f) ||
        //                (this.PlanePointDot(trianglePlane2, currentTriangle.v2, compareTriangle.v2, compareTriangle.n2) - EPSILON > 0.0f) ||
        //                (this.PlanePointDot(trianglePlane2, currentTriangle.v2, compareTriangle.v3, compareTriangle.n3) - EPSILON > 0.0f) ||
        //                (this.PlanePointDot(trianglePlane3, currentTriangle.v3, compareTriangle.v1, compareTriangle.n1) - EPSILON > 0.0f) ||
        //                (this.PlanePointDot(trianglePlane3, currentTriangle.v3, compareTriangle.v2, compareTriangle.n2) - EPSILON > 0.0f) ||
        //                (this.PlanePointDot(trianglePlane3, currentTriangle.v3, compareTriangle.v3, compareTriangle.n3) - EPSILON > 0.0f))
        //            {
                        
        //                currentTriangle.convexGeometry = false;
        //                trianglesToProcess.RemoveAt(j);
        //                //compareTriangle.convexGeometry = false;
                        
        //                // Theres no need to process triangle at 'j' later on so remove it from trianglesToProcess.
        //                // Triangle at 'index' should be removed too, but it cant be removed just yet.
        //                //trianglesToProcess.RemoveAt(j);
        //            }
        //        }

        //        trianglesToProcess.Remove(currentTriangle);
        //    }
        //}

        //private float PlanePointDot(Plane plane, Vector3 pointOnPlane, Vector3 pointInSpace, Vector3 pointInSpaceNormal)
        //{
        //    if (Vector3.Dot(plane.Normal, pointInSpaceNormal) - EPSILON > 0.0f)
        //        return Vector3.Dot(plane.Normal, Vector3.Normalize(pointInSpace - pointOnPlane));
        //    else
        //        return -1;
        //}

        //private void GetPlaneForVector(Vector3 position, Vector3 normal, out Plane plane)
        //{
        //    // Plane equation: Ax + By + Cz + D = 0
        //    // We need to solve for D, so the equation turns into:
        //    // Ax + By + Cz = -D

        //    float D = (normal.X * position.X) + (normal.Y * position.Y) + (normal.Z * position.Z);
        //    plane = new Plane(normal, D);

        //    //Vector3 dirToVector = Vector3.Normalize(position);
        //    //Vector3 dirToPlane = Vector3.Normalize(normal);
        //    //float hypotenuse1 = position.Length();

        //    //float dot = Vector3.Dot(dirToVector, dirToPlane);
        //    //float angle = (1 - dot) * MathHelper.PiOver2;

        //    //float length = (float)Math.Cos(angle) * hypotenuse1;

        //    //plane.Normal = normal;
        //    //plane.D = length;
        //}

        //private bool PointAbovePlane(Plane plane, Vector3 point)
        //{
        //    Vector3 planeCenter = -plane.D * plane.Normal;
        //    float dot = Vector3.Dot(Vector3.Normalize(point - planeCenter), plane.Normal);
        //    System.Diagnostics.Debug.WriteLine(dot);
        //    return (dot >= 0.0f);
        //}

        //private bool ExamineTriangleConvex(Triangle triangle)
        //{
        //    for (int i = 0; i < this.triangles.Count; i++ )
        //    {
        //        if (this.triangles[i].id != triangle.id &&
        //            (this.triangles[i].i1 == triangle.i1 ||
        //            this.triangles[i].i2 == triangle.i2 ||
        //            this.triangles[i].i3 == triangle.i3))
        //        {
        //            this.cyclicPreventor.Add(triangle);

        //            float dot;
        //            Vector3.Dot(ref this.triangles[i].surfaceNormal, ref triangle.surfaceNormal, out dot);
        //            if (dot < 0)
        //            {
        //                return false;
        //            }

        //            ExamineTriangleConvex(this.triangles[i]);
        //        }
        //    }
        //    return true;
        //}

        private void CreateMaterial()
        {
            
            this.material = new Material(this.Reflectiveness, this.UseTexture, this.Transparent, this.RefractionIndex, this.TextureFilePath);
            this.material.InterpolateNormals = this.InterpolateNormals;
            //System.Diagnostics.Debugger.Launch();
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

                //System.Diagnostics.Debugger.Launch();
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
                    //Matrix normalTransform = Matrix.CreateRotationX(MathHelper.PiOver2) *Matrix.CreateRotationZ(MathHelper.Pi);
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
                        //triangle.material = this.material;
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