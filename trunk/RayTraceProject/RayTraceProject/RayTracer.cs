using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using RayTracerTypeLibrary;
using System.Runtime.Remoting.Messaging;
using System.ComponentModel;

namespace RayTraceProject
{
    class RayTracer
    {
        private List<ILight> lights;
        private RenderTarget2D target;
        Color[] renderTargetData;

        public Spatial.ISpatialManager CurrentScene { get; set; }
        public Camera CurrentCamera { get; set; }
        public RenderTarget2D CurrentTarget
        {
            get { return this.target; }
            set
            {
                if (this.IsBusy)
                    throw new InvalidOperationException("Can not change RenderTarget while RayTracer is busy.");
                this.target = value;
                this.renderTargetData = new Color[this.target.Width * this.target.Height];
            }
        }
        public GraphicsDevice GraphicsDevice { get; set; }
        public int MaxReflections { get; set; }
        public bool IsBusy { get; private set; }
        public event EventHandler<AsyncCompletedEventArgs> RenderCompleted;
        public TextureFiltering TextureFiltering { get; set; }
        public UVAddressMode AddressMode { get; set; }
        public List<ILight> Lights { get { return this.lights; } }

        public bool UseMultisampling { get; set; }
        public int MultisampleQuality { get; set; }

        public float Progress
        {
            get { return (float)this.scanline / (float)CurrentTarget.Height; }
        }

        private int scanline = -1;
        public int GetNextScanline()
        {
            return System.Threading.Interlocked.Increment(ref scanline);
        }

        public RayTracer()
        {
            this.lights = new List<ILight>();
        }

        public void RenderAsync()
        {
            this.points.Clear();
            if (this.IsBusy)
                throw new InvalidOperationException("Current render operation not finished.");

#if DEBUG_CONVEXFLAG
            this.MaxReflections = 1;
#endif

            this.IsBusy = true;
            System.Threading.Thread asyncThread;
            if (this.UseMultisampling)
                asyncThread = new System.Threading.Thread(RenderInternalWithMultisampling);
            else
                asyncThread = new System.Threading.Thread(RenderInternal);

            asyncThread.IsBackground = true;
            asyncThread.Name = "RenderDispatcherThread";
            asyncThread.Start();
        }

        private unsafe int GetNumberOfThreads()
        {
            uint affinityMask = (uint)System.Diagnostics.Process.GetCurrentProcess().ProcessorAffinity.ToInt32();

            int processors = 0;
            uint highbit = 0x80000000;
            uint mask = 1;

            // "Loop-and-a-half"
            if ((affinityMask & mask) > 0)
                processors++;
            do
            {
                mask = mask << 1;
                if ((affinityMask & mask) > 0)
                    processors++;
            } while (mask != highbit);

            return processors;
        }

        private void RenderInternal()
        {
            int numberOfThreads = this.GetNumberOfThreads();
            System.Threading.Thread[] threads = new System.Threading.Thread[numberOfThreads];

            for (int i = 0; i < numberOfThreads; i++)
            {
                threads[i] = new System.Threading.Thread(this.Render);
                threads[i].IsBackground = true;
                threads[i].Name = string.Format("RenderThread ({0})", i);
                threads[i].Start(new Rectangle(0, 0, CurrentTarget.Width, CurrentTarget.Height));
            }


            for (int i = 0; i < numberOfThreads; i++)
            {
                threads[i].Join();
            }

            GraphicsDevice.SetRenderTarget(null);
            CurrentTarget.SetData<Color>(this.renderTargetData);

            this.OnRenderCompleted();
        }

        private void RenderInternalWithMultisampling()
        {
            int numberOfThreads = this.GetNumberOfThreads();
            System.Threading.Thread[] threads = new System.Threading.Thread[numberOfThreads];

            // First pass
            this.scanline = -1;
            for (int i = 0; i < numberOfThreads; i++)
            {
                threads[i] = new System.Threading.Thread(this.RenderFirstPass);
                threads[i].IsBackground = true;
                threads[i].Name = string.Format("FirstPassRenderThread ({0})", i);
                threads[i].Start(new Rectangle(0, 0, this.CurrentTarget.Width, this.CurrentTarget.Height));
            }

            for (int i = 0; i < numberOfThreads; i++)
            {
                threads[i].Join();
            }

            //// Second pass
            //this.scanline = -1;
            //for (int i = 0; i < numberOfThreads; i++)
            //{
            //    threads[i] = new System.Threading.Thread(this.RenderSecondPass);
            //    threads[i].IsBackground = true;
            //    threads[i].Name = string.Format("SecondPassRenderThread ({0})", i);
            //    threads[i].Start(new Rectangle(0, 0, this.CurrentTarget.Width, this.CurrentTarget.Height));
            //}

            //for (int i = 0; i < numberOfThreads; i++)
            //{
            //    threads[i].Join();
            //}

            GraphicsDevice.SetRenderTarget(null);
            //CurrentTarget.SetData<Color>(this.renderTargetData);
            CurrentTarget.SetData<Color>(0, new Rectangle(0, 0, CurrentTarget.Width, CurrentTarget.Height), renderTargetData, 0, CurrentTarget.Width * CurrentTarget.Height);

            this.OnRenderCompleted();
        }

        private void RenderFirstPass(object rectangleObject)
        {
            Rectangle viewportRectangle = (Rectangle)rectangleObject;
            bool finished = false;
            Matrix view = CurrentCamera.View;
            Matrix proj = CurrentCamera.Projection;
            Viewport viewport = GraphicsDevice.Viewport;
            Ray ray;

            while (!finished)
            {
                int scanline = this.GetNextScanline();
                // This is very similar to the standard render-routine, apart from that we go one extra scanline AND
                // one extra "increment" on the width.

                if (scanline >= viewportRectangle.Height)
                {
                    finished = true;
                }
                else
                {
                    Vector3 screenSpaceCoord;

                    for (int x = 0; x < viewportRectangle.Width; x++)
                    {
                        Color result;
                        GetColorForQuadrant(x, scanline, 1.0f, 0, out result);

                        //screenSpaceCoord.X = (x - 0.25f);
                        //screenSpaceCoord.Y = (scanline - 0.25f);
                        //screenSpaceCoord.Z = 0;
                        //ray.Position = viewport.Unproject(screenSpaceCoord, proj, view, Matrix.Identity);

                        //screenSpaceCoord.Z = 1;
                        //Vector3 vector2 = viewport.Unproject(screenSpaceCoord, proj, view, Matrix.Identity);
                        //Vector3.Subtract(ref vector2, ref ray.Position, out ray.Direction);
                        //Vector3.Normalize(ref ray.Direction, out ray.Direction);

                        //Color color;
                        //this.CastRay(ref ray, out color, 0, null, null);
                        renderTargetData[((scanline) * viewportRectangle.Width) + x] = result;
                    }
                }
            }
        }

        private void GetColorForQuadrant(float centerX, float centerY, float size, int iteration, out Color result)
        {
            Ray ray;
            float quarterSize = size * 0.25f;
            Color ulColor, urColor, llColor, lrColor;
            Vector3 screenSpaceCoord;
            Vector3 vector2;

            // Cast upper-left ray.
            screenSpaceCoord.X = centerX - quarterSize;
            screenSpaceCoord.Y = centerY - quarterSize;
            screenSpaceCoord.Z = 0;
            ray.Position = this.GraphicsDevice.Viewport.Unproject(screenSpaceCoord, this.CurrentCamera.Projection, this.CurrentCamera.View, Matrix.Identity);

            screenSpaceCoord.Z = 1;
            vector2 = this.GraphicsDevice.Viewport.Unproject(screenSpaceCoord, this.CurrentCamera.Projection, this.CurrentCamera.View, Matrix.Identity);
            Vector3.Subtract(ref vector2, ref ray.Position, out ray.Direction);
            Vector3.Normalize(ref ray.Direction, out ray.Direction);

            this.CastRay(ref ray, out ulColor, 0, null, null, 1.0f);

            // Cast upper-right ray.

            screenSpaceCoord.X = centerX + quarterSize;
            screenSpaceCoord.Y = centerY - quarterSize;
            screenSpaceCoord.Z = 0;
            ray.Position = this.GraphicsDevice.Viewport.Unproject(screenSpaceCoord, this.CurrentCamera.Projection, this.CurrentCamera.View, Matrix.Identity);

            screenSpaceCoord.Z = 1;
            vector2 = this.GraphicsDevice.Viewport.Unproject(screenSpaceCoord, this.CurrentCamera.Projection, this.CurrentCamera.View, Matrix.Identity);
            Vector3.Subtract(ref vector2, ref ray.Position, out ray.Direction);
            Vector3.Normalize(ref ray.Direction, out ray.Direction);

            this.CastRay(ref ray, out urColor, 0, null, null, 1.0f);

            // Cast lower-left ray.

            screenSpaceCoord.X = centerX - quarterSize;
            screenSpaceCoord.Y = centerY + quarterSize;
            screenSpaceCoord.Z = 0;
            ray.Position = this.GraphicsDevice.Viewport.Unproject(screenSpaceCoord, this.CurrentCamera.Projection, this.CurrentCamera.View, Matrix.Identity);

            screenSpaceCoord.Z = 1;
            vector2 = this.GraphicsDevice.Viewport.Unproject(screenSpaceCoord, this.CurrentCamera.Projection, this.CurrentCamera.View, Matrix.Identity);
            Vector3.Subtract(ref vector2, ref ray.Position, out ray.Direction);
            Vector3.Normalize(ref ray.Direction, out ray.Direction);

            this.CastRay(ref ray, out llColor, 0, null, null, 1.0f);

            // Cast lower-right ray.

            screenSpaceCoord.X = centerX + quarterSize;
            screenSpaceCoord.Y = centerY + quarterSize;
            screenSpaceCoord.Z = 0;
            ray.Position = this.GraphicsDevice.Viewport.Unproject(screenSpaceCoord, this.CurrentCamera.Projection, this.CurrentCamera.View, Matrix.Identity);

            screenSpaceCoord.Z = 1;
            vector2 = this.GraphicsDevice.Viewport.Unproject(screenSpaceCoord, this.CurrentCamera.Projection, this.CurrentCamera.View, Matrix.Identity);
            Vector3.Subtract(ref vector2, ref ray.Position, out ray.Direction);
            Vector3.Normalize(ref ray.Direction, out ray.Direction);

            this.CastRay(ref ray, out lrColor, 0, null, null, 1.0f);

            // Only subdivide if we havent reached the recursion limit.
            if (iteration < this.MultisampleQuality)
            {
                Vector3 ulColorVector = ulColor.ToVector3();
                Vector3 urColorVector = urColor.ToVector3();
                Vector3 llColorVector = llColor.ToVector3();
                Vector3 lrColorVector = lrColor.ToVector3();
                Vector3 average = (ulColorVector + urColorVector + llColorVector + lrColorVector) / 4.0f;
                float average_length = average.Length();

                if (Math.Abs(average_length - ulColorVector.Length()) > TRESHOLD)
                {
                    this.GetColorForQuadrant(centerX - quarterSize, centerY - quarterSize, size / 2.0f, iteration + 1, out ulColor);
                }

                if (Math.Abs(average_length - urColorVector.Length()) > TRESHOLD)
                {
                    this.GetColorForQuadrant(centerX + quarterSize, centerY - quarterSize, size / 2.0f, iteration + 1, out urColor);
                }

                if (Math.Abs(average_length - llColorVector.Length()) > TRESHOLD)
                {
                    this.GetColorForQuadrant(centerX - quarterSize, centerY + quarterSize, size / 2.0f, iteration + 1, out llColor);
                }

                if (Math.Abs(average_length - lrColorVector.Length()) > TRESHOLD)
                {
                    this.GetColorForQuadrant(centerX + quarterSize, centerY + quarterSize, size / 2.0f, iteration + 1, out urColor);
                }
            }

            result = new Color((ulColor.ToVector3() + urColor.ToVector3() + llColor.ToVector3() + lrColor.ToVector3()) / 4.0f);

        }

        private void RenderSecondPass(object rectangleObject)
        {
            Rectangle viewportRectangle = (Rectangle)rectangleObject;
            bool finished = false;
            Matrix view = CurrentCamera.View;
            Matrix proj = CurrentCamera.Projection;
            Viewport viewport = GraphicsDevice.Viewport;
            Ray ray;
            while (!finished)
            {
                int scanline = this.GetNextScanline();
                if (scanline >= viewportRectangle.Height)
                {
                    finished = true;
                }
                else
                {
                    for (int i = 0; i < viewportRectangle.Width; i++)
                    {
                        this.GetColorForQuadrant(
                            ref viewportRectangle, i, scanline,
                            out this.renderTargetData[((scanline) * viewportRectangle.Width) + i]);
                    }
                }
            }
        }

        const float TRESHOLD = 0.5f;  //MAX 1.73205078f;
        private void GetColorForQuadrant(ref Rectangle viewportRectangle, int x, int y, out Color result)
        {
            int ul = ((y) * viewportRectangle.Width) + x;
            int ur = ((y) * viewportRectangle.Width) + x + 1;
            int ll = ((y + 1) * viewportRectangle.Width) + x;
            int lr = ((y + 1) * viewportRectangle.Width) + x + 1;
            // Once I can reconnect to my SVN I need to do a check-in, then refactor all code to use Vector3/Vector4 instead of Color
            Vector3 ulColor = this.renderTargetData[ul].ToVector3();
            Vector3 urColor = this.renderTargetData[ur].ToVector3();
            Vector3 llColor = this.renderTargetData[ll].ToVector3();
            Vector3 lrColor = this.renderTargetData[lr].ToVector3();

            Vector3 average = (ulColor + urColor + llColor + lrColor) / 4.0f;
            //float averageLength = average.Length();

            //float ulLength = ulColor.Length();
            //float urLength = urColor.Length();
            //float llLength = llColor.Length();
            //float lrLength = lrColor.Length();

            //if (Math.Abs(averageLength - ulLength) > TRESHOLD)
            //{
            //    this.SubdivideQuadrant(x, y, 0.5f, 0.5f)
            //    // Subdivide on upper left corner
            //}

            //if (Math.Abs(averageLength - urLength) > TRESHOLD)
            //{
            //    // Subdivide on upper right corner
            //}

            //if (Math.Abs(averageLength - llLength) > TRESHOLD)
            //{
            //    // Subdivide on upper right corner
            //}

            //if (Math.Abs(averageLength - lrLength) > TRESHOLD)
            //{
            //    // Subdivide on upper right corner
            //}

            result = new Color(average);

        }

        private void SubdivideQuadrant(int x, int y, float x2, float y2)
        {

        }

        private void Render(object rectangleObject)
        {
            Rectangle viewportRectangle = (Rectangle)rectangleObject;
            bool finished = false;
            Matrix view = CurrentCamera.View;
            Matrix proj = CurrentCamera.Projection;
            Viewport viewport = GraphicsDevice.Viewport;
            Ray ray;
            while (!finished)
            {
                int scanline = this.GetNextScanline();
                if (scanline >= viewportRectangle.Height)
                {
                    finished = true;
                }
                else
                {
                    Vector3 screenSpaceCoord;

                    for (int x = 0; x < viewportRectangle.Width; x++)
                    {
                        screenSpaceCoord.X = x;
                        screenSpaceCoord.Y = scanline;
                        screenSpaceCoord.Z = 0;
                        ray.Position = viewport.Unproject(screenSpaceCoord, proj, view, Matrix.Identity);
                        
                        screenSpaceCoord.Z = 1;
                        Matrix matrix = Matrix.Invert(Matrix.Multiply(Matrix.Multiply(Matrix.Identity, view), proj));
                        Vector3 vector2 = viewport.Unproject(screenSpaceCoord, proj, view, Matrix.Identity);
                        Vector3.Subtract(ref vector2, ref ray.Position, out ray.Direction);
                        Vector3.Normalize(ref ray.Direction, out ray.Direction);

                        Color color;
                        this.CastRay(ref ray, out color, 0, null, null, 1.0f);
                        renderTargetData[((scanline) * viewportRectangle.Width) + x] = color;
                    }
                }
            }
        }

        private void OnRenderCompleted()
        {
            this.IsBusy = false;
            this.scanline = -1;
            if (this.RenderCompleted != null)
                this.RenderCompleted(this, new AsyncCompletedEventArgs(null, false, null));
        }

        //private bool IsLightPathObstructed(RayTraceProject.Spatial.IntersectionResult result, ILight light)
        //{
        //    Vector3 dirToLight;
        //    float distanceToLight;
        //    if (light.IsPositionable)
        //    {
        //        dirToLight = light.Position - result.worldPosition;
        //        distanceToLight = dirToLight.Length();
        //        dirToLight.Normalize();
        //    }
        //    else
        //    {
        //        dirToLight = -light.Direction;
        //        distanceToLight = float.MaxValue;
        //    }


        //    Ray shadowRay = new Ray(result.worldPosition, dirToLight);

        //    RayTraceProject.Spatial.IntersectionResult? intersectionResult;
        //    if (CurrentScene.GetRayIntersection(ref shadowRay, out intersectionResult, result.triangle, null) && intersectionResult.Value.d < distanceToLight)
        //        return true;
        //    else
        //        return false;
        //}

        private float IsLightPathObstructed(RayTraceProject.Spatial.IntersectionResult result, ILight light)
        {
            Vector3 dirToLight;
            float distanceToLight;
            if (light.IsPositionable)
            {
                dirToLight = light.Position - result.worldPosition;
                distanceToLight = dirToLight.Length();
                dirToLight.Normalize();
            }
            else
            {
                dirToLight = -light.Direction;
                distanceToLight = float.MaxValue;
            }


            Ray shadowRay = new Ray(result.worldPosition, dirToLight);

            RayTraceProject.Spatial.IntersectionResult? intersectionResult;
            if (CurrentScene.GetRayIntersection(ref shadowRay, out intersectionResult, result.triangle, null) && intersectionResult.Value.d < distanceToLight)
            {
                Spatial.IntersectionResult res = intersectionResult.Value;
                Material material = result.mesh.MeshMaterial;
                if (material.Transparent)
                {
                    return res.triangle.color.W;
                }
                else
                {
                    return 1;
                }
            }
            else
            {
                return 0;
            }
        }

        public List<VertexPositionColor> points = new List<VertexPositionColor>();
        public float smallesttheta1 = float.MaxValue;
        public void CastRay(ref Ray ray, out Color resultColor, int iteration, Triangle origin, Mesh ignoreObject, float currentRefIndex)
        {
            resultColor = Color.White;

            RayTraceProject.Spatial.IntersectionResult? nullableResult;

            if (CurrentScene.GetRayIntersection(ref ray, out nullableResult, origin, ignoreObject))
            {
                RayTraceProject.Spatial.IntersectionResult result = (RayTraceProject.Spatial.IntersectionResult)nullableResult;
                Material material = result.mesh.MeshMaterial;
                Vector3 fragmentNormal;

                if (material.InterpolateNormals)
                {
                    Vector3 n1 = result.triangle.n2 - result.triangle.n1;
                    Vector3 n2 = result.triangle.n3 - result.triangle.n1;
                    fragmentNormal = result.triangle.n1 + (n1 * result.u) + (n2 * result.v);
                    fragmentNormal.Normalize();

                }
                else
                {
                    fragmentNormal = result.triangle.surfaceNormal;
                }

                Vector3 lightResult = Vector3.Zero;
                for (int i = 0; i < this.lights.Count; i++)
                {
                    float lightAmount = this.IsLightPathObstructed(result, this.lights[i]);
                    if (lightAmount != 1f)
                    {
                        lightResult += this.lights[i].GetLightForFragment(result.worldPosition, fragmentNormal) * (1.0f - lightAmount);
                    }
                }
                //Vector3 lightDir = new Vector3(1, 1, 0);
                //lightDir.Normalize();

                //float lightIntensity = Vector3.Dot(lightDir, fragmentNormal);
                //if (lightIntensity < 0.2f)
                //    lightIntensity = 0.2f;

                //addRayPoints(ray.Position, interpolatedPosition);

                if (iteration < this.MaxReflections)
                {
                    //addRayPoints(ray.Position, intersectionPosition.Value);
                    //addRayPoints(intersectionPosition.Value, intersectionPosition.Value + interpolatedNormal);
                    Ray r = new Ray();
                    r.Position = result.worldPosition;
                    r.Direction = Vector3.Reflect(ray.Direction, fragmentNormal);
                    r.Direction.Normalize();

                    //addRayPoints(ray.Position, ray.Position + ray.Direction * 100);

                    Color reflectionColor;

                    // If the triangle is part of convex geometry, there is no need to check for collisions for this next ray.

                    if (result.triangle.convexGeometry)
                        this.CastRay(ref r, out reflectionColor, iteration + 1, result.triangle, result.mesh, currentRefIndex);
                    else
                        this.CastRay(ref r, out reflectionColor, iteration + 1, result.triangle, null, currentRefIndex);

                    Vector3 surfaceColor;

#if DEBUG_NORMALS
                    resultColor = new Color(fragmentNormal);
#elif DEBUG_CONVEXFLAG
                    resultColor = result.triangle.convexGeometry ? Color.Green : Color.Red;
#else
                    if (material.UseTexture)
                    {
                        Vector2 uv1 = (result.triangle.uv2) - (result.triangle.uv1);
                        Vector2 uv2 = (result.triangle.uv3) - (result.triangle.uv1);
                        Vector2 interpolatedUV = (result.triangle.uv1) + (uv1 * result.u) + (uv2 * result.v);

                        material.LookupUV(interpolatedUV, this.AddressMode, this.TextureFiltering, out surfaceColor);
                    }
                    else
                    {
                        surfaceColor.X = result.triangle.color.X;
                        surfaceColor.Y = result.triangle.color.Y;
                        surfaceColor.Z = result.triangle.color.Z;
                    }

                    //if(reflectionColor == Color.Black)
                    //    resultColor = new Color(surfaceColor * lightResult);
                    //else

                    Vector3 colorVector = Vector3.Lerp(reflectionColor.ToVector3(), surfaceColor, 1.0f - material.Reflectiveness) * lightResult;

                    if (material.Transparent)
                    {
                        //addRayPoints(ray.Position, result.worldPosition, Color.White);
                        float theta1;

                        float n1, n2;
                        
                        if (currentRefIndex == material.RefractionIndex)
                        {
                            // Ray is inside material. Exiting to vacuum.
                            n1 = currentRefIndex;
                            n2 = 1.0f; // Index of vacuum.
                            theta1 = 1 - Vector3.Dot(-fragmentNormal, -ray.Direction);
                            //addRayPoints(ray.Position, ray.Position + (ray.Direction * 10), Color.Red);
                            //theta1 = -theta1;

                        }
                        else
                        {


                            n1 = currentRefIndex;
                            n2 = material.RefractionIndex;
                            theta1 = 1 - Vector3.Dot(fragmentNormal, -ray.Direction);
                        }

                        //} 
                        ////// SER DET INTE UT SOM EN SKÅLFORMAD LINS? DEN SKA INVERTERAS!
                        // n1 * sin(vinkel1) = n2 * sin(vinkel2)
                        // (n1 * sin(vinkel1) / n2 = sin(vinkel2)
                        // Jag tror inte jag behöver ta sinus av theta1, det är redan en sine.
                        float theta2 = (float)Math.Asin((n1 * theta1) / n2);
                        Vector3 refraction;
                        if (theta1 != 0.0f && theta2 != 0.0f)
                        {


                            Vector3 rotateAxis = Vector3.Cross(fragmentNormal, ray.Direction);
                            Matrix rotateMatrix = Matrix.CreateFromAxisAngle(rotateAxis, theta2);
                            refraction = Vector3.Transform(-fragmentNormal, rotateMatrix);

                        }
                        else
                        {
                            refraction = ray.Direction;
                        }
                        ray.Position = result.worldPosition;
                        ray.Direction = refraction;
                        ray.Direction.Normalize();


                        //addRayPoints(result.worldPosition, (refraction * 1000), new Color(red, red, red));
                        //red = (red + (255 / 2)) % 255;
                        Color refractColor;

                        this.CastRay(ref ray, out refractColor, iteration + 1, result.triangle, null, n2);

                        colorVector = Vector3.Lerp(refractColor.ToVector3(), colorVector, result.triangle.color.W);

                    }


                    resultColor = new Color(colorVector);
#endif
                }
                else
                {
                    Vector3 surfaceColor;
                    if (material.UseTexture)
                    {
                        Vector2 uv1 = (result.triangle.uv2) - (result.triangle.uv1);
                        Vector2 uv2 = (result.triangle.uv3) - (result.triangle.uv1);
                        Vector2 interpolatedUV = (result.triangle.uv1) + (uv1 * result.u) + (uv2 * result.v);

                        material.LookupUV(interpolatedUV, this.AddressMode, this.TextureFiltering, out surfaceColor);
                    }
                    else
                    {
                        surfaceColor.X = result.triangle.color.X;
                        surfaceColor.Y = result.triangle.color.Y;
                        surfaceColor.Z = result.triangle.color.Z;
                    }

                    resultColor = new Color(lightResult * surfaceColor);
                }
            }
            else // Ray does not intersect any object.
            {

                resultColor = new Color(Vector3.Zero);

                //addRayPoints(ray.Position, ray.Position + (1000 * ray.Direction));
            }

        }
        int red = 255;

        private void addRayPoints(Vector3 p, Vector3 q, Color c)
        {
            lock (points)
            {
                points.Add(new VertexPositionColor(p, c));
                points.Add(new VertexPositionColor(q, c));
            }
        }


    }
}
