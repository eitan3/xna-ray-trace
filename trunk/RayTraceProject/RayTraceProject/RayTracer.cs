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
        Color[] renderTargetData;

        public Spatial.ISpatialManager CurrentScene { get; set; }
        public Camera CurrentCamera { get; set; }
        public RenderTarget2D CurrentTarget { get; set; }
        public GraphicsDevice GraphicsDevice { get; set; }
        public int MaxReflections { get; set; }
        public bool IsBusy { get; private set; }
        public event EventHandler<AsyncCompletedEventArgs> RenderCompleted;
        public TextureFiltering TextureFiltering { get; set; }
        public UVAddressMode AddressMode { get; set; }
        public List<ILight> Lights { get { return this.lights; } }

        

        public float Progress
        {
            get { return (float)this.scanline / (float)CurrentTarget.Height;  }
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
            if (this.IsBusy)
                throw new InvalidOperationException("Current render operation not finished.");

#if DEBUG_CONVEXFLAG
            this.MaxReflections = 1;
#endif

            this.IsBusy = true;
            System.Threading.Thread asyncThread = new System.Threading.Thread(RenderInternal);
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

        private  void RenderInternal()
        {
            this.renderTargetData = new Color[this.CurrentTarget.Width * this.CurrentTarget.Height];

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
                        if (scanline == 418 && x == 474)
                        {
                            int isd = 5;
                        }
                        screenSpaceCoord.X = x;
                        screenSpaceCoord.Y = scanline;
                        screenSpaceCoord.Z = 0;
                        ray.Position = viewport.Unproject(screenSpaceCoord, proj, view, Matrix.Identity);

                        screenSpaceCoord.Z = 1;
                        Vector3 vector2 = viewport.Unproject(screenSpaceCoord, proj, view, Matrix.Identity);
                        Vector3.Subtract(ref vector2, ref ray.Position, out ray.Direction);
                        Vector3.Normalize(ref ray.Direction, out ray.Direction);

                        Color color;
                        this.CastRay(ref ray, out color, 0, null, null);
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

        private bool IsLightPathObstructed(RayTraceProject.Spatial.IntersectionResult result, ILight light)
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
                return true;
            else
                return false;
        }

        public List<VertexPositionColor> points = new List<VertexPositionColor>();
        public void CastRay(ref Ray ray, out Color resultColor, int iteration, Triangle origin, SceneObject ignoreObject)
        {
            resultColor = Color.White;

            RayTraceProject.Spatial.IntersectionResult? nullableResult;

            if (CurrentScene.GetRayIntersection(ref ray, out nullableResult, origin, ignoreObject))
            {
                RayTraceProject.Spatial.IntersectionResult result = (RayTraceProject.Spatial.IntersectionResult)nullableResult;
                Material material = result.triangle.material;
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
                    if (!this.IsLightPathObstructed(result, this.lights[i]))
                    {
                        lightResult += this.lights[i].GetLightForFragment(result.worldPosition, fragmentNormal);
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
                    ray.Position = result.worldPosition;
                    ray.Direction = Vector3.Reflect(ray.Direction, fragmentNormal);
                    ray.Direction.Normalize();                  
                    
                    //addRayPoints(ray.Position, ray.Position + ray.Direction * 100);

                    Color reflectionColor;

                    // If the triangle is part of convex geometry, there is no need to check for collisions for this next ray.

                    if(result.triangle.convexGeometry)
                        this.CastRay(ref ray, out reflectionColor, iteration + 1, result.triangle, result.sceneObject);
                    else
                        this.CastRay(ref ray, out reflectionColor, iteration + 1, result.triangle, null);
                    
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
                        surfaceColor = result.triangle.color;
                    }

                    //if(reflectionColor == Color.Black)
                    //    resultColor = new Color(surfaceColor * lightResult);
                    //else
                        resultColor = new Color(Vector3.Lerp(reflectionColor.ToVector3(), (surfaceColor * lightResult), 1.0f - material.Reflectiveness));
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
                        surfaceColor = result.triangle.color;
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

        private void addRayPoints(Vector3 p, Vector3 q)
        {
            lock (points)
            {
                points.Add(new VertexPositionColor(p, Color.White));
                points.Add(new VertexPositionColor(q, Color.White));
            }
        }


    }
}
