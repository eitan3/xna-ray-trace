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
        SpriteFont onScreenFont;
        KeyboardState lastState;
        MouseState lastMouse;
        Vector2 lastMouseCoord;

        Spatial.OctreeSpatialManager scene;
        Camera camera;
        SceneObject crate, crate2;
        SceneObject android;
        RenderTarget2D rayTraceTarget;
        RayTracer tracer;
        SceneObject plane;
        Stopwatch rayTraceWatch;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 800;
            graphics.PreferredBackBufferHeight = 600;
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


        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            this.onScreenFont = Content.Load<SpriteFont>("Fonts\\OnScreenFont");

            Model planeModel = Content.Load<Model>("Plane");
            plane = new SceneObject(planeModel, Vector3.Zero, Vector3.Zero);

            Model androidModel = Content.Load<Model>("SonyLogo");
            android = new SceneObject(androidModel, new Vector3(0, 8, 0), Vector3.Zero);
            android.Rotation = new Vector3(0, -MathHelper.PiOver2, 0);

            Model coffeeModel = Content.Load<Model>("coffeepot");
            crate = new SceneObject(coffeeModel, new Vector3(0, 9, 0), Vector3.Zero);

            Model crateModel = Content.Load<Model>("Crate_Fragile");
            crate2 = new SceneObject(crateModel, new Vector3(50, 9, 0), Vector3.Zero);

            

            this.scene = new Spatial.OctreeSpatialManager();

            scene.Bodies.Add(plane);
            scene.Bodies.Add(crate);
            //scene.Bodies.Add(android); // Avoid using android model until it works. It has far too many triangles to use for testing.

            this.scene.Build();

            //this.camera = new Camera(new Vector3(0, 17, 70), Vector3.Zero, Vector3.Up, MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000);
            //this.camera = new Camera(new Vector3(0, 3, 17), Vector3.Zero, Vector3.Up, MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000);
            //this.camera = new Camera(new Vector3(-58, 20, -21), Vector3.Zero, Vector3.Up, MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000);
            this.camera = new Camera(new Vector3(-58, 20, -21), Vector3.Zero, Vector3.Up, MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000);
            rayTraceTarget = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

            tracer = new RayTracer();
            tracer.CurrentCamera = this.camera;
            tracer.CurrentScene = this.scene;
            tracer.CurrentTarget = rayTraceTarget;
            tracer.CurrentGraphicsDevice = GraphicsDevice;
            tracer.RenderCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(tracer_RenderCompleted);

            // TODO: use this.Content to load your game content here
        }

        void tracer_RenderProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
        }

        void tracer_RenderCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            rayTraceWatch.Stop();
            Debug.WriteLine(rayTraceWatch.Elapsed.ToString());
        }


        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

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

                rayTraceWatch = Stopwatch.StartNew();
                tracer.RenderAsyncV2();
            }

            if (state.IsKeyUp(Keys.Space) && lastState.IsKeyDown(Keys.Space) && !tracer.IsBusy)
                showRayTraceImage = !showRayTraceImage;

            
            MouseState mouseState = Mouse.GetState();
            Vector2 mouseCoord = new Vector2(mouseState.X, mouseState.Y);

            if (mouseState.RightButton == ButtonState.Pressed)
            {
                Vector3 delta = new Vector3(lastMouseCoord - mouseCoord, 0);
                delta.X *= -1;
                camera.Position += delta;
                camera.Target += delta;
            }
            lastMouseCoord = mouseCoord;
            lastState = state;
            lastMouse = mouseState;

            base.Update(gameTime);
        }
        bool showRayTraceImage;
        double progressUpdate;
        string progressText = String.Empty;
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
            if (showRayTraceImage && !tracer.IsBusy)
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

            if (tracer.IsBusy)
            {
                progressUpdate += gameTime.ElapsedGameTime.TotalSeconds;
                if (progressUpdate > 3.0)
                {
                    progressUpdate -= 3.0;
                    progressText = String.Format("Progress: {0}%", tracer.Progress * 100);
                }

                spriteBatch.Begin();
                spriteBatch.DrawString(this.onScreenFont, this.progressText, new Vector2(16, 16), Color.White);
                spriteBatch.End();
            }

            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}
