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

namespace RayTraceProject
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
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
            Ray ray = new Ray(new Vector3(0, 0, 0), -Vector3.UnitZ);
            RayTracerTypeLibrary.Triangle triangle = new RayTracerTypeLibrary.Triangle();
            triangle.v1 = new Vector3(-1, 1, -10);
            triangle.v2 = new Vector3(1, 1, -10);
            triangle.v3 = new Vector3(0, -1, -10);

            float u, v, distance;
            ray.IntersectsTriangle(triangle, out u, out v, out distance);
            //bool result = SceneObject.RayTriangleIntesercts(ray, triangle, out u, out v, out distance);

            base.Initialize();
        }
        Spatial.OctreeSpatialManager scene;
        Camera camera;
        SceneObject android, crate;
        RenderTarget2D rayTraceTarget;
        RayTracer tracer;
        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            Model planeModel = Content.Load<Model>("Plane");
            SceneObject plane = new SceneObject(planeModel, Vector3.Zero, Vector3.Zero);

            //Model androidModel = Content.Load<Model>("Android");
            //android = new SceneObject(androidModel, new Vector3(0, 0, 0), Vector3.Zero);

            Model crateModel = Content.Load<Model>("Crate_Fragile");
            crate = new SceneObject(crateModel, new Vector3(0, 0, 0), Vector3.Zero);

            this.scene = new Spatial.OctreeSpatialManager();

            scene.Bodies.Add(plane);
            scene.Bodies.Add(crate);
            //scene.Bodies.Add(android); // Avoid using android model until it works. It has far too many triangles to use for testing.

            this.scene.Build();

            //this.camera = new Camera(new Vector3(0, 17, 70), Vector3.Zero, Vector3.Up, MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000);
            //this.camera = new Camera(new Vector3(0, 3, 17), Vector3.Zero, Vector3.Up, MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000);
            this.camera = new Camera(new Vector3(0, 30, 1), Vector3.Zero, Vector3.Up, MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000);
            rayTraceTarget = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            tracer = new RayTracer();
            tracer.CurrentCamera = this.camera;
            tracer.CurrentScene = this.scene;
            tracer.CurrentTarget = rayTraceTarget;
            tracer.CurrentViewport = GraphicsDevice.Viewport;

            tracer.Render();

            // TODO: use this.Content to load your game content here
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

            if (state.IsKeyDown(Keys.Q))
                camera.Position += new Vector3(0, 0.1f, 0);
            else if (state.IsKeyDown(Keys.E))
                camera.Position -= new Vector3(0, 0.1f, 0);
            else if(state.IsKeyDown(Keys.W))
                camera.Position += new Vector3(0, 0, 0.1f);
            else if (state.IsKeyDown(Keys.S))
                camera.Position -= new Vector3(0, 0, 0.1f);
            else if(state.IsKeyDown(Keys.Enter) && !showRayTraceImage)
                tracer.Render();


            if (state.IsKeyUp(Keys.Space) && lastState.IsKeyDown(Keys.Space))
                showRayTraceImage = !showRayTraceImage;

            MouseState mouseState = Mouse.GetState();
            Window.Title = string.Format("{0} : {1}", mouseState.X, mouseState.Y);

            // TODO: Add your update logic here
            lastState = state;
            base.Update(gameTime);
        }
        bool showRayTraceImage;
        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

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



            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}
