using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using RayTracerTypeLibrary;
using System.Diagnostics;

namespace RayTraceProject
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        RasterizerState rasterState;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 256;
            graphics.PreferredBackBufferHeight = 256;
            Content.RootDirectory = "Content";
            this.IsMouseVisible = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            this.rasterState = new RasterizerState();
            this.rasterState.CullMode = CullMode.CullCounterClockwiseFace;
            this.rasterState.FillMode = FillMode.Solid; // FillMode.WireFrame;

            base.Initialize();
        }
        Spatial.OctreeSpatialManager scene;
        Camera camera;
        SceneObject android, crate, sony, crate2;
        RenderTarget2D rayTraceTarget;
        RayTracer tracer;
        SceneObject plane;
        Triangle triangle;
        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            Model planeModel = Content.Load<Model>("Plane");
            plane = new SceneObject(planeModel, Vector3.Zero, Vector3.Zero);

            //Model androidModel = Content.Load<Model>("Android");
            //android = new SceneObject(androidModel, new Vector3(0, 0, 0), Vector3.Zero);

            Model coffeeModel = Content.Load<Model>("coffeepot");
            crate = new SceneObject(coffeeModel, new Vector3(0, 9, 0), Vector3.Zero);

            //crate2 = new SceneObject(coffeeModel, new Vector3(50, 9, 0), Vector3.Zero);

            //Model sonyModel = Content.Load<Model>("Ant");
            //sony = new SceneObject(sonyModel, new Vector3(0, 0, 0), Vector3.Zero);

            this.scene = new Spatial.OctreeSpatialManager();

            scene.Bodies.Add(plane);
            scene.Bodies.Add(crate);
            //scene.Bodies.Add(crate2);
            //scene.Bodies.Add(sony);
            //scene.Bodies.Add(android); // Avoid using android model until it works. It has far too many triangles to use for testing.

            this.scene.Build();

            //this.camera = new Camera(new Vector3(0, 17, 70), Vector3.Zero, Vector3.Up, MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000);
            //this.camera = new Camera(new Vector3(0, 3, 17), Vector3.Zero, Vector3.Up, MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000);
            this.camera = new Camera(new Vector3(-58, 20, -21), Vector3.Zero, Vector3.Up, MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000);
            rayTraceTarget = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            tracer = new RayTracer();
            tracer.CurrentCamera = this.camera;
            tracer.CurrentScene = this.scene;
            tracer.CurrentTarget = rayTraceTarget;
            tracer.CurrentGraphicsDevice = GraphicsDevice;
            tracer.RenderCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(tracer_RenderCompleted);
            tracer.RenderProgressChanged += new EventHandler<System.ComponentModel.ProgressChangedEventArgs>(tracer_RenderProgressChanged);
            //tracer.Render();

            // TODO: use this.Content to load your game content here
        }

        int progress;
        bool running = false;
        Stopwatch rayTraceWatch;
        void tracer_RenderProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            progress = e.ProgressPercentage;
        }

        void tracer_RenderCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            rayTraceWatch.Stop();
            Debug.WriteLine(rayTraceWatch.Elapsed.ToString());
            running = false;
        }


        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }
        KeyboardState lastState;
        MouseState lastMouse;
        Ray ray;
        Ray crateRay;
        float distance;
        Vector2 lastMouseCoord;
        List<Triangle> tris;
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();
            KeyboardState state = Keyboard.GetState();

            Vector3 translation = Vector3.Zero;
            if (state.IsKeyDown(Keys.Q))
                translation += new Vector3(0, 1, 0);
            if (state.IsKeyDown(Keys.E))
                translation += new Vector3(0, -1, 0);
            if(state.IsKeyDown(Keys.W))
                translation += new Vector3(0, 0, 1);
            if (state.IsKeyDown(Keys.S))
                translation += new Vector3(0, 0, -1);
            if (state.IsKeyDown(Keys.A))
                translation += new Vector3(-1, 0, 0);
            if (state.IsKeyDown(Keys.D))
                translation += new Vector3(1, 0, 0);
            float speedFactor = 1.0f;

            if (state.IsKeyDown(Keys.LeftShift))
                speedFactor *= 10;

            camera.Position += translation * speedFactor;

            if (state.IsKeyDown(Keys.Enter) && !showRayTraceImage && !tracer.IsBusy)
            {
                tracer.CurrentCamera = this.camera;
                tracer.CurrentScene = this.scene;

                this.running = true;
                rayTraceWatch = Stopwatch.StartNew();
                tracer.RenderAsync();
            }

            if (state.IsKeyUp(Keys.Space) && lastState.IsKeyDown(Keys.Space))
                showRayTraceImage = !showRayTraceImage;

            
            MouseState mouseState = Mouse.GetState();
            Vector2 mouseCoord = new Vector2(mouseState.X, mouseState.Y);
            if (mouseState.LeftButton == ButtonState.Released && lastMouse.LeftButton == ButtonState.Pressed)
            {

               // Vector3 v1 = new Vector3(mouseState.X, mouseState.Y, 0f);
               // Vector3 v2 = new Vector3(mouseState.X, mouseState.Y, 1f);

               // Vector3 unv1 = GraphicsDevice.Viewport.Unproject(v1, camera.Projection, camera.View, Matrix.Identity);
               // Vector3 unv2 = GraphicsDevice.Viewport.Unproject(v2, camera.Projection, camera.View, Matrix.Identity);

               // ray = new Ray(unv1, unv2 - unv1);
               // ray.Direction.Normalize();

               // crateRay = ray;
               // Matrix invCrate = Matrix.Invert(crate2.World);

               // Vector3 t1 = Vector3.Transform(ray.Position, invCrate);
               // Vector3 t2 = Vector3.Transform(ray.Position + ray.Direction, invCrate);
               // crateRay = new Ray(t1, t2 - t1);


               // //Vector3.Transform(ref ray.Position, ref invCrate, out crateRay.Position);
               // //crateRay.Direction = Vector3.Transform(ray.Position + ray.Direction, invCrate);

               //// Vector3.Transform(ref ray.Direction, ref invCrate, out crateRay.Direction);

               // crateRay.Direction.Normalize();

               // float minT = float.MaxValue;
               // bool hit = false;
               // float u, v, t;
               // float hitU, hitV;
               // hitU = hitV = 0;
               // Triangle? triangle = null;

               // List<Triangle> tris2 = crate2.GetTriangles();

               // for (int i = 0; i < tris2.Count; i++)
               // {

               //     if (crateRay.IntersectsTriangle(tris2[i], out u, out v, out t))
               //     {
               //         if (t < minT)
               //         {
               //             minT = t;
               //             hit = true;
               //             triangle = tris2[i];
               //             hitU = u;
               //             hitV = v;
               //         }
               //     }
               // }

                
               // //for (int i = 0; i < tris.Count; i++)
               // //{
                   
               // //    if (ray.IntersectsTriangle(tris[i], out u, out v, out t))
               // //    {
               // //        if (t < minT)
               // //        {
               // //            minT = t;
               // //            hit = true;
               // //            triangle = tris[i];
               // //            hitU = u;
               // //            hitV = v;
               // //        }
               // //    }
               // //}

               // if (hit)
               // {
               //     distance = minT;
               //     Vector3 n1 = triangle.Value.n2 - triangle.Value.n1;
               //     Vector3 n2 = triangle.Value.n3 - triangle.Value.n1;
               //     Vector3 intersection = triangle.Value.n1 + (n1 * hitV) + (n2 * hitU);
               //     intersection.Normalize();
               //     Window.Title = string.Format("Distance: {0} - UV: [{1},{2}] - Normal: {3}", minT, hitU, hitV, intersection);
               //     System.Diagnostics.Debug.WriteLine(Window.Title);
               // }
               // else
               // {
               //     Window.Title = "No hit!";
               // }
            }
            if (mouseState.RightButton == ButtonState.Pressed)
            {
                Vector3 delta = new Vector3(lastMouseCoord - mouseCoord, 0);
                delta.X *= -1;
                camera.Position += delta;
                camera.Target += delta;
            }
            lastMouseCoord = mouseCoord;

            if (running)
                Window.Title = string.Format("Progress: {0}%", progress);
            //Window.Title = string.Format("{0} : {1}", mouseState.X, mouseState.Y);

            // TODO: Add your update logic here
            lastState = state;
            lastMouse = mouseState;
            base.Update(gameTime);
        }
        bool showRayTraceImage;
        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Magenta);
            GraphicsDevice.RasterizerState = rasterState;
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            if (showRayTraceImage)
            {
                spriteBatch.Begin();
                spriteBatch.Draw(rayTraceTarget, new Rectangle(0, 0, rayTraceTarget.Width, rayTraceTarget.Height), Color.White);
                spriteBatch.End();

            }
            else
            {
                Matrix view = camera.View;
                Matrix proj = camera.Projection;
                scene.Draw(camera, ref view, ref proj, this.GraphicsDevice, gameTime);
            }

            VertexPositionColor[] verts = new VertexPositionColor[]
            {
                new VertexPositionColor(ray.Position, Color.Red),
                new VertexPositionColor(ray.Position + (1000 * ray.Direction), Color.Red)
            };

            VertexPositionColor[] verts2 = new VertexPositionColor[]
            {
                new VertexPositionColor(crateRay.Position, Color.Green),
                new VertexPositionColor(crateRay.Position + (distance * crateRay.Direction), Color.Green)
            };

            if (eff == null)
                eff = new BasicEffect(GraphicsDevice);
            eff.World = Matrix.Identity;
            eff.Projection = camera.Projection;
            eff.View = camera.View;
            eff.CurrentTechnique.Passes[0].Apply();
            GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineList, verts, 0, 1, VertexPositionColor.VertexDeclaration);
            GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineList, verts2, 0, 1, VertexPositionColor.VertexDeclaration);




            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }

        BasicEffect eff;
    }
}
