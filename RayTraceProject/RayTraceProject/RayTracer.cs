using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using RayTracerTypeLibrary;

namespace RayTraceProject
{
    class RayTracer
    {
        const float RAY_INCREMENT = 0.1f;
        public Spatial.ISpatialManager CurrentScene { get; set; }
        public Camera CurrentCamera { get; set; }
        public RenderTarget2D CurrentTarget { get; set; }
        public Viewport CurrentViewport { get; set; }

        public void Render()
        {
            Rectangle rect = new Rectangle(0, 0, CurrentTarget.Width, CurrentTarget.Height);
            this.RenderRectangle(rect);
        }

        private void RenderRectangle(Rectangle rect)
        {
            int dataSize = rect.Width * rect.Height;
            Color[] textureData = new Color[dataSize];


            Spatial.ISpatialManager scene = this.CurrentScene;
            Viewport viewport = this.CurrentViewport;
            Matrix view = this.CurrentCamera.View;
            Matrix proj = this.CurrentCamera.Projection;
            Random rand = new Random();
            for (int y = rect.Y; y < rect.Y + rect.Height; y++)
            {
                for (int x = rect.X; x < rect.X + rect.Width; x++)
                {
                    Vector3 vector1 = viewport.Unproject(new Vector3(x, y, 0), proj, view, Matrix.Identity);
                    Vector3 vector2 = viewport.Unproject(new Vector3(x, y, 1), proj, view, Matrix.Identity);
                    Vector3 direction;
                    Vector3.Subtract(ref vector2, ref vector1, out direction);
                    Vector3.Normalize(ref direction, out direction);
                    
                    Ray ray = new Ray(vector1, direction);

                    uint cubeoidId = scene.GetCubeoidId(ray.Position);
                    if (x == 364 && y == 220)
                    {
                        int df = 5;
                    }
                    bool skip = false;
                    if (cubeoidId == uint.MaxValue)
                    {
                        if (!scene.TranslateRayToScene(ref ray))
                        {
                            skip = true;
                        }
                        else
                        {
                            cubeoidId = scene.GetCubeoidId(ray.Position);
                        }
                    }

                    Color c = new Color(0, 0, 255);

                    if (!skip)
                    {

                        uint lastCubeoidId = cubeoidId;

                        float closestDistance = float.MaxValue;
                        float closestU, closestV;
                        Triangle? closestTriangle = null;

                        do
                        {
                            List<Spatial.ISpatialBody> bodies = scene.GetContainedBodies(cubeoidId);
                            for (int i = 0; i < bodies.Count; i++)
                            {
                                Ray inverseRay;
                                Matrix inverseWorld = (bodies[i] as SceneObject).InverseWorld;
                                Vector3.Transform(ref ray.Position, ref inverseWorld, out inverseRay.Position);
                                Vector3.Transform(ref ray.Direction, ref inverseWorld, out inverseRay.Direction);
                                Vector3.Normalize(ref inverseRay.Direction, out inverseRay.Direction);

                                if (bodies[i].RayIntersects(inverseRay))
                                {
                                    List<Triangle> tris = bodies[i].GetTriangles();
                                    for (int j = 0; j < tris.Count; j++)
                                    {
                                        float u, v, distance;
                                        if (inverseRay.IntersectsTriangle(tris[j], out u, out v, out distance))
                                        {
                                            if (distance < closestDistance)
                                            {
                                                closestDistance = distance;
                                                closestTriangle = tris[j];
                                                closestU = u;
                                                closestV = v;
                                            }
                                        }
                                    }
                                }
                            }

                            if (!closestTriangle.HasValue)
                            {
                                do
                                {
                                    ray.Position += ray.Direction * RAY_INCREMENT;
                                    cubeoidId = scene.GetCubeoidId(ray.Position);
                                } while (lastCubeoidId == cubeoidId);
                                lastCubeoidId = cubeoidId;
                            }


                        } while (cubeoidId != uint.MaxValue && !closestTriangle.HasValue);

                        if (closestTriangle.HasValue)
                        {
                            float dist = closestDistance / 14.0f;
                            Vector3 col = new Vector3(0.0f, dist, 0.0f);
                           
                            c = new Color(col);
                        }
                    }

                    textureData[(y * rect.Width) + x] = c;
                    //while (true)
                    //{
                    //    ray.Position += ray.Direction * RAY_INCREMENT;

                    //    List<Spatial.ISpatialBody> bodies = scene.GetPossibleIntersections(ray.Position);

                    //    float minDistance = float.MaxValue;
                    //    float? currentDistance;
                    //    Vector3 minNormal;
                    //    Vector3 currentNormal;
                    //    for (int i = 0; i < bodies.Count; i++)
                    //    {
                    //        if(bodies[i].GetIntersectingFaceNormal(ray, out currentNormal, out currentDistance) &&
                    //            currentDistance.Value < minDistance)
                    //        {
                    //            minNormal = currentNormal;
                    //        }
                    //    }
                    //}

   
                }
            }

            CurrentTarget.SetData<Color>(0, rect, textureData, 0, dataSize);
        }


    }
}
