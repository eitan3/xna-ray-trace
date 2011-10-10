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
            asyncThread.Start();
        }

        private void RenderInternal()
        {
            const int RENDER_ROWS = 2;
            const int RENDER_COLS = 2;
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
                    this.CastRay(ref ray, out color);
                    textureData[((y - rect.Y) * rect.Width) + (x - rect.X)] = color;  
                }

                context.Progress = (float)y / (float)(maxY - 1);
                if(context.ID == 0)
                    this.OnRenderProgressChanged();
            }

            CurrentGraphicsDevice.SetRenderTarget(null);
            CurrentTarget.SetData<Color>(0, rect, textureData, 0, dataSize);
        }

        public void CastRay(ref Ray ray, out Color result)
        {
            result = Color.White;

            Triangle? triangle;
            float? u, v;
            if (CurrentScene.GetRayIntersection(ref ray, out triangle, out u, out v))
            {
                Vector3 n1 = triangle.Value.n2 - triangle.Value.n1;
                Vector3 n2 = triangle.Value.n3 - triangle.Value.n1;
                Vector3 intersection = triangle.Value.n1 + (n1 * u.Value) + (n2 * v.Value);
                intersection.Normalize();
                result = new Color(intersection);
            }
            else
            {
                result = Color.IndianRed;
            }
            
        }


    }
}
