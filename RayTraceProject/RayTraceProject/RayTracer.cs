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
        const int MAX_REFLECTIONS = 6;
        const float RAY_INCREMENT = 0.1f;
        public Spatial.ISpatialManager CurrentScene { get; set; }
        public Camera CurrentCamera { get; set; }
        public RenderTarget2D CurrentTarget { get; set; }
        public GraphicsDevice CurrentGraphicsDevice { get; set; }
        public event EventHandler<AsyncCompletedEventArgs> RenderCompleted;
        public event EventHandler<ProgressChangedEventArgs> RenderProgressChanged;
        private RenderThreadContext[] contexts;

        private class RenderThreadContext
        {
            public int ID { get; private set; }
            public float Progress { get; set; }
            public Rectangle RenderRectangle { get; private set; }

            public RenderThreadContext(int ID, Rectangle rect)
            {
                this.ID = ID;
                this.RenderRectangle = rect;
            }
        }

        public bool IsBusy
        {
            get;
            private set;
        }

        public Material.TextureFiltering TextureFiltering
        {
            get;
            set;
        }

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
            
        }

        public void RenderAsync()
        {
            if (this.IsBusy)
                throw new InvalidOperationException("Current render operation not finished.");

            this.IsBusy = true;
            System.Threading.Thread asyncThread = new System.Threading.Thread(RenderInternal);
            asyncThread.IsBackground = true;
            asyncThread.Name = "RenderDispatcherThread";
            asyncThread.Start();
        }

        Color[] data;
        public void RenderAsyncV2()
        {
            if (this.IsBusy)
                throw new InvalidOperationException("Current render operation not finished.");

            this.IsBusy = true;
            System.Threading.Thread asyncThread = new System.Threading.Thread(RenderInternalV2);
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

        private  void RenderInternalV2()
        {
            this.data = new Color[this.CurrentTarget.Width * this.CurrentTarget.Height];

            int numberOfThreads = 1; // this.GetNumberOfThreads();
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

            CurrentGraphicsDevice.SetRenderTarget(null);
            CurrentTarget.SetData<Color>(this.data);

            this.OnRenderCompleted();
        }

        private void Render(object rectangleObject)
        {
            Rectangle viewportRectangle = (Rectangle)rectangleObject;
            bool finished = false;
            Matrix view = CurrentCamera.View;
            Matrix proj = CurrentCamera.Projection;
            Viewport viewport = CurrentGraphicsDevice.Viewport;
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
                        if (scanline == 300 && x == 400)
                        {
                            int sd = 5;
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
                        this.CastRay(ray, out color, 1, null);
                        data[((scanline) * viewportRectangle.Width) + x] = color;  
                    }
                }
            }
        }

        private void RenderInternal()
        {
            const int RENDER_ROWS = 2;
            const int RENDER_COLS = 4;
            Rectangle[] rects = new Rectangle[RENDER_ROWS * RENDER_COLS];
            int rectWidth = CurrentTarget.Width / RENDER_COLS;
            int rectHeight = CurrentTarget.Height / RENDER_ROWS;

            System.Threading.Thread[] renderThreads = new System.Threading.Thread[RENDER_ROWS * RENDER_COLS];
            this.contexts = new RenderThreadContext[RENDER_ROWS * RENDER_COLS];
            for (int y = 0; y < RENDER_ROWS; y++)
            {
                for (int x = 0; x < RENDER_COLS; x++)
                {
                    int threadIndex = (y * RENDER_COLS) + x;
                    this.contexts[threadIndex] = new RenderThreadContext(threadIndex, new Rectangle(x * rectWidth, y * rectHeight, rectWidth, rectHeight));
                    renderThreads[threadIndex] = new System.Threading.Thread(this.RenderRectangle);
                    renderThreads[threadIndex].IsBackground = true;
                    renderThreads[threadIndex].Name = string.Format("RenderThread ({0})", threadIndex);
                    renderThreads[threadIndex].Start(this.contexts[threadIndex]);
                }
            }

            for (int i = 0; i < RENDER_ROWS * RENDER_COLS; i++)
                renderThreads[i].Join();

            this.OnRenderCompleted();
        }

        private void OnRenderProgressChanged()
        {
            float total = this.contexts.Sum(x => x.Progress) / (float)this.contexts.Length;

            if (this.RenderProgressChanged != null)
                this.RenderProgressChanged(this, new ProgressChangedEventArgs((int)(total * 100), null));
        }

        private void OnRenderCompleted()
        {
            this.IsBusy = false;
            this.scanline = -1;
            if (this.RenderCompleted != null)
                this.RenderCompleted(this, new AsyncCompletedEventArgs(null, false, null));
        }

        private void RenderRectangle(Object ctx)
        {
            RenderThreadContext context = (RenderThreadContext)ctx;
            Rectangle rect = (Rectangle)context.RenderRectangle;
            int dataSize = rect.Width * rect.Height;
            Color[] textureData = new Color[dataSize];


            Spatial.ISpatialManager scene = this.CurrentScene;
            Viewport viewport = this.CurrentGraphicsDevice.Viewport;
            Matrix view = this.CurrentCamera.View;
            Matrix proj = this.CurrentCamera.Projection;

            int maxY = rect.Y + rect.Height;
            int maxX = rect.X + rect.Width;
            Ray ray;
            Vector3 screenSpaceCoord;
            for (int y = rect.Y; y < maxY; y++)
            {
                for (int x = rect.X; x < maxX; x++)
                {
                    screenSpaceCoord.X = x;
                    screenSpaceCoord.Y = y;
                    screenSpaceCoord.Z = 0;
                    ray.Position = viewport.Unproject(screenSpaceCoord, proj, view, Matrix.Identity);

                    screenSpaceCoord.Z = 1;
                    Vector3 vector2 = viewport.Unproject(screenSpaceCoord, proj, view, Matrix.Identity);
                    Vector3.Subtract(ref vector2, ref ray.Position, out ray.Direction);
                    Vector3.Normalize(ref ray.Direction, out ray.Direction);
                    
                    Color color;
                    this.CastRay(ray, out color, 1, null);
                    textureData[((y - rect.Y) * rect.Width) + (x - rect.X)] = color;  
                }

                context.Progress = (float)y / (float)(maxY - 1);
                if(context.ID == 0)
                    this.OnRenderProgressChanged();
            }

            CurrentGraphicsDevice.SetRenderTarget(null);
            CurrentTarget.SetData<Color>(0, rect, textureData, 0, dataSize);
        }

        private struct Vector7
        {
            public float x, y, z, w, s, t, q;
            public Vector7(float x, float y, float z, float w, float s, float t, float q)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.w = w;
                this.s = s;
                this.t = t;
                this.q = q;
            }

            public static Vector7 operator+(Vector7 a, Vector7 b)
            {
                return new Vector7(
                    a.x + b.x,
                    a.y + b.y,
                    a.z + b.z,
                    a.w + b.w,
                    a.s + b.s,
                    a.t + b.t,
                    a.q + b.q);
            }

            public static Vector7 operator -(Vector7 a, Vector7 b)
            {
                return new Vector7(
                    a.x - b.x,
                    a.y - b.y,
                    a.z - b.z,
                    a.w - b.w,
                    a.s - b.s,
                    a.t - b.t,
                    a.q - b.q);
            }

            public static Vector7 operator /(Vector7 a, float b)
            {
                return new Vector7(
                    a.x / b,
                    a.y / b,
                    a.z / b,
                    a.w / b,
                    a.s / b,
                    a.t / b,
                    a.q / b);
            }

            public static Vector7 operator *(Vector7 a, float b)
            {
                return new Vector7(
                    a.x * b,
                    a.y * b,
                    a.z * b,
                    a.w * b,
                    a.s * b,
                    a.t * b,
                    a.q * b);
            }

            public override string ToString()
            {
                return string.Format("X:{0} Y:{1} Z:{2} W:{3} S:{4} T:{5} Q:{6}",
                    this.x, this.y, this.z, this.w, this.s, this.t, this.q);
            }
        }

        public List<VertexPositionColor> points = new List<VertexPositionColor>();
        public void CastRay(Ray ray, out Color result, int iteration, Triangle origin)
        {
            result = Color.White;
            
            Triangle triangle;
            float? u, v;
            Vector3? intersectionPosition;
            if (CurrentScene.GetRayIntersection(ray, out triangle, out u, out v, out intersectionPosition, origin))
            {
                Vector3 n1 = triangle.n2 - triangle.n1;
                Vector3 n2 = triangle.n3 - triangle.n1;
                Vector3 interpolatedNormal = triangle.n1 + (n1 * u.Value) + (n2 * v.Value);
                interpolatedNormal.Normalize();


                Vector3 lightDir = new Vector3(1, 1, 0);
                lightDir.Normalize();

                float lightIntensity = Vector3.Dot(lightDir, interpolatedNormal);
                if (lightIntensity < 0.4f)
                    lightIntensity = 0.4f;


                // This position interpolation is just here for debug purposes. It should really only be performed if the ray should reflect.


                //addRayPoints(ray.Position, interpolatedPosition);

                if (iteration < MAX_REFLECTIONS)
                {
                    // GetRayIntersection does this work for us now.
                    //Vector3 p1 = triangle.v2 - triangle.v1;
                    //Vector3 p2 = triangle.v3 - triangle.v1;
                    //Vector3 interpolatedPosition = triangle.v1 + (p1 * u.Value) + (p2 * v.Value);

                    //addRayPoints(ray.Position, intersectionPosition.Value);
                    //addRayPoints(intersectionPosition.Value, intersectionPosition.Value + interpolatedNormal);
                    ray.Position = intersectionPosition.Value;
                    ray.Direction = Vector3.Reflect(ray.Direction, interpolatedNormal);
                    ray.Direction.Normalize();
                    //System.Diagnostics.Debug.WriteLine("Dot: {0}", Vector3.Dot(ray.Direction, interpolatedNormal));
                    
                    
                    //addRayPoints(ray.Position, ray.Position + ray.Direction * 100);

                    Color reflectionColor;
                    this.CastRay(ray, out reflectionColor, iteration + 1, triangle);

                    Material material = triangle.material;
                    Vector3 surfaceColor;
                    
#if DEBUG_NORMALS
                    result = new Color(interpolatedNormal);
#else
                    if (material.UseTexture)
                    {
                        Vector2 uv1 = (triangle.uv2) - (triangle.uv1);
                        Vector2 uv2 = (triangle.uv3) - (triangle.uv1);
                        Vector2 interpolatedUV = (triangle.uv1) + (uv1 * u.Value) + (uv2 * v.Value);

                        material.LookupUV(interpolatedUV, this.TextureFiltering, out surfaceColor);
                    }
                    else
                    {
                        surfaceColor = triangle.color;
                    }
                    result = new Color(Vector3.Lerp(reflectionColor.ToVector3(), (surfaceColor * lightIntensity), 1.0f - material.Reflectiveness));
#endif
                }
                else
                {
                    Material material = triangle.material;
                    Vector3 surfaceColor;
                    if (material.UseTexture)
                    {
                        Vector2 uv1 = (triangle.uv2) - (triangle.uv1);
                        Vector2 uv2 = (triangle.uv3) - (triangle.uv1);
                        Vector2 interpolatedUV = (triangle.uv1) + (uv1 * u.Value) + (uv2 * v.Value);

                        material.LookupUV(interpolatedUV, this.TextureFiltering, out surfaceColor);
                    }
                    else
                    {
                        surfaceColor = triangle.color;
                    }

                    result = new Color(lightIntensity * surfaceColor);
                }


                
            }
            else
            {
                Vector3 lightDir = new Vector3(1, 1, 0);
                lightDir.Normalize();
                float lightIntensity = Vector3.Dot(ray.Direction, lightDir);
                if (lightIntensity < 0.4f)
                    lightIntensity = 0.4f;

                result = new Color(Vector3.One * lightIntensity);

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
