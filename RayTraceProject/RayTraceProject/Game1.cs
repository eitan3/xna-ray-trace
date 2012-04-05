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
using AviFile;

namespace RayTraceProject
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        RasterizerState solidRasterState, wireRasterState;
        bool useWireframe = false;
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
        private string folder;

        private Model coneModel;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 512;
            graphics.PreferredBackBufferHeight = 512;
            Content.RootDirectory = "Content";
            Vector3 v1 = new Vector3(0, 0, 1);
            Vector3 v2 = new Vector3(-0.0076134061f, -0.22078878f, -0.97529179f);
            float dot = Vector3.Dot(v1, v2);
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
            this.solidRasterState = new RasterizerState();
            this.solidRasterState.CullMode = CullMode.CullCounterClockwiseFace;
            this.solidRasterState.FillMode = FillMode.Solid; // FillMode.WireFrame;
            this.wireRasterState = new RasterizerState();
            this.wireRasterState.CullMode = CullMode.CullCounterClockwiseFace;
            this.wireRasterState.FillMode = FillMode.WireFrame;

            Plane p = new Plane(new Vector3(0, 1, 0), -50);
            p.Normal.Normalize();

            Vector3 point = new Vector3(0, 51, 0);

            float dot = p.DotCoordinate(point);

            Vector3 planeCenter = -p.D * p.Normal;

            float dot2 = Vector3.Dot(Vector3.Normalize(point - planeCenter), p.Normal);

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

            Model planeModel = Content.Load<Model>("Ground");
            Model androidModel = Content.Load<Model>("SonyLogo");
            Model coffeeModel = Content.Load<Model>("coffeepot");
            Model sphereModel = Content.Load<Model>("Sphere");
            Model crateModel = Content.Load<Model>("Crate_Fragile");
            //Model cubeModel = Content.Load<Model>("cube");
            Model monkeyModel = Content.Load<Model>("monkey");
            Model torusModel = Content.Load<Model>("torus");
            Model matModel = Content.Load<Model>("mat");
            Model chessModel = Content.Load<Model>("chesspiece");
            Model wossyModel = Content.Load<Model>("wossy");
            Model mehModel = Content.Load<Model>("meh");
           // Model chessboardModel = Content.Load<Model>("chessboard");

            
            

            plane = new SceneObject(GraphicsDevice, planeModel, Vector3.Zero, new Vector3(MathHelper.PiOver2, 0, 0));
            plane.Name = "Ground";



            //android = new SceneObject(androidModel, new Vector3(0, 8, 0), Vector3.Zero);
            //android.Rotation = new Vector3(0, -MathHelper.PiOver2, 0);

            //System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), string.Format("Trace{0}", DateTime.Now.ToString("yyMMddHHmmss")));
            ////////////this.folder = System.IO.Path.Combine(@"D:\Videos", string.Format("Trace{0}", DateTime.Now.ToString("yyMMddHHmmss")));

            ////////////if (!System.IO.Directory.Exists(folder))
            ////////////{
            ////////////    System.IO.Directory.CreateDirectory(this.folder);
            ////////////}

            this.scene = new Spatial.OctreeSpatialManager();
            this.scene.Bodies.Add(plane);

            SceneObject sphere = new SceneObject(GraphicsDevice, sphereModel, new Vector3(0, 0, 8), new Vector3(0, 0, 0));
            sphere.Scale = Vector3.One;
            sphere.Name = "Sphere";
            scene.Bodies.Add(sphere);

            this.scene.Build();

            //this.camera = new Camera(new Vector3(22, 27, 58), new Vector3(0, 0, 0), Vector3.Up, MathHelper.PiOver4, GraphicsDevice.Viewport.AspectRatio, 0.1f, 1000);
            this.camera = new Camera(new Vector3(0, 0, 16), sphere.Position, Vector3.Up, 1.57f, GraphicsDevice.Viewport.AspectRatio, 1.0f, 1000.0f);
            rayTraceTarget = new RenderTarget2D(GraphicsDevice, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

            tracer = new RayTracer();
            tracer.TextureFiltering = TextureFiltering.Point;
            tracer.AddressMode = UVAddressMode.Wrap;
            tracer.CurrentCamera = this.camera;
            tracer.CurrentScene = this.scene;
            tracer.CurrentTarget = rayTraceTarget;
            tracer.GraphicsDevice = GraphicsDevice;
            tracer.MaxReflections = 4;
            tracer.UseMultisampling = false;
            tracer.MultisampleQuality = 1;
            tracer.RenderCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(tracer_RenderCompleted);


            light = new SpotLight();
            light.Color = Vector3.One;
            light.Position = (new Vector3(0, 20, 10));
            light.Direction = -Vector3.Normalize(light.Position);
            light.SpotAngle = MathHelper.PiOver2;
            light.Intensity = 1f;
            tracer.Lights.Add(light);

            // TODO: use this.Content to load your game content here
        }
        SpotLight light;
        SceneObject[] sphereRow1 = new SceneObject[5];
        SceneObject[] sphereRow2 = new SceneObject[4];
        SceneObject meh;
        bool finished = false;
        bool videocompiled = false;
        bool nextframe = true;
        int frame = 0;
        
        float rot_step = MathHelper.TwoPi / 360.0f;
        float rot_spaceRow1 = MathHelper.TwoPi / 5.0f;
        float rot_spaceRow2 = MathHelper.TwoPi / 4.0f;
        float rot = 0;
        void tracer_RenderCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            //////////rayTraceWatch.Stop();
            //////////Debug.WriteLine(rayTraceWatch.Elapsed.ToString());
            //////////string filePath = System.IO.Path.Combine(this.folder, string.Format("image{0}.png", frame++));
            //////////using(System.IO.FileStream fs = new System.IO.FileStream(filePath, System.IO.FileMode.Create))
            //////////{
            //////////    rayTraceTarget.SaveAsPng(fs, rayTraceTarget.Width, rayTraceTarget.Height);
            //////////    fs.Close();
            //////////}

            //////////rot += rot_step;
            //////////light.Direction = new Vector3((float)Math.Sin(rot), -(float)Math.Cos(rot), 0);
            //////////if (rot >= MathHelper.TwoPi)
            //////////{
            //////////    finished = true;
            //////////}
            ////////////for (int i = 0; i < 5; i++)
            ////////////{
            ////////////    sphereRow1[i].Position = new Vector3((float)Math.Sin(rot + (i * rot_spaceRow1)) * 8, 1, (float)Math.Cos(rot + (i * rot_spaceRow1)) * 8);
            ////////////}

            ////////////for (int i = 0; i < 4; i++)
            ////////////{
            ////////////    sphereRow2[i].Position = new Vector3((float)Math.Sin((rot*2) + (i * rot_spaceRow2)) * 5, 4, (float)Math.Cos((rot*2) + (i * rot_spaceRow2)) * 5);
            ////////////}
            //////////if (rot >= MathHelper.TwoPi)
            //////////{
            //////////    finished = true;
            //////////}


            ////////////if (frame == maxI)
            ////////////{
            ////////////    finished = true;
            ////////////}

            //////////nextframe = true;
        }

        private void compileVideo()
        {
            AviManager aviManager = new AviManager(System.IO.Path.Combine(this.folder, "VideoCompressed.avi"), false);

            System.Drawing.Bitmap firstBitmap = (System.Drawing.Bitmap)System.Drawing.Bitmap.FromFile(System.IO.Path.Combine(this.folder, string.Format("image{0}.png", 0)));

            VideoStream aviStream = aviManager.AddVideoStream(true, 30, firstBitmap);
            //VideoStream aviStream = aviManager.AddVideoStream(true, 30, firstBitmap);

            System.Drawing.Bitmap bitmap;
            for (int i = 1; i < 178; i++)
            {
                bitmap = (System.Drawing.Bitmap)System.Drawing.Bitmap.FromFile(System.IO.Path.Combine(this.folder, string.Format("image{0}.png", i)));
                aviStream.AddFrame(bitmap);
                bitmap.Dispose();
            }
            aviManager.Close();
            Debug.WriteLine("Video compiled");
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
            if (state.IsKeyDown(Keys.W))
                translation += new Vector3(0, 0, 1);
            if (state.IsKeyDown(Keys.S))
                translation += new Vector3(0, 0, -1);
            if (state.IsKeyDown(Keys.A))
                translation += new Vector3(-1, 0, 0);
            if (state.IsKeyDown(Keys.D))
                translation += new Vector3(1, 0, 0);
            float speedFactor = 1.0f;
            if (state.IsKeyDown(Keys.D1) && lastState.IsKeyUp(Keys.D1))
                this.useWireframe = !this.useWireframe;
            if (state.IsKeyDown(Keys.Back))
                tracer.points.Clear();

            if (state.IsKeyDown(Keys.LeftShift))
                speedFactor *= 10;

            camera.Position += translation * speedFactor;
            if (state.IsKeyDown(Keys.Right) && lastState.IsKeyUp(Keys.Right))
            {
                rot -= rot_step;
                this.camera.Position = new Vector3((float)Math.Sin(rot) * 60, 4, (float)Math.Cos(rot) * 60);
            }
            if (state.IsKeyDown(Keys.Left) && lastState.IsKeyUp(Keys.Left))
            {
                rot += rot_step;
                this.camera.Position = new Vector3((float)Math.Sin(rot) * 60, 4, (float)Math.Cos(rot) * 60);
            }

            if (state.IsKeyDown(Keys.Enter) && !showRayTraceImage && !tracer.IsBusy)
            {
                tracer.CurrentCamera = this.camera;
                tracer.CurrentScene = this.scene;

                rayTraceWatch = Stopwatch.StartNew();
                tracer.RenderAsync();
            }

            if (state.IsKeyUp(Keys.Space) && lastState.IsKeyDown(Keys.Space) && !tracer.IsBusy)
                showRayTraceImage = !showRayTraceImage;

            
            MouseState mouseState = Mouse.GetState();
            if (mouseState.LeftButton == ButtonState.Released && lastMouse.LeftButton == ButtonState.Pressed)
            {

                Vector3 screenSpaceCoord;
                Ray ray;
                screenSpaceCoord.X = mouseState.X;
                screenSpaceCoord.Y = mouseState.Y;
                screenSpaceCoord.Z = 0;
                ray.Position = GraphicsDevice.Viewport.Unproject(screenSpaceCoord, camera.Projection, camera.View, Matrix.Identity);

                screenSpaceCoord.Z = 1;
                Vector3 vector2 = GraphicsDevice.Viewport.Unproject(screenSpaceCoord, camera.Projection, camera.View, Matrix.Identity);
                Vector3.Subtract(ref vector2, ref ray.Position, out ray.Direction);
                Vector3.Normalize(ref ray.Direction, out ray.Direction);
                Color color;
                tracer.CastRay(ref ray, out color, 1, null, null, 1.0f);
                //tracer.CastRay(ray, out color, 1, null);

            }


            if (tracer.IsBusy)
            {
                progressUpdate += gameTime.ElapsedGameTime.TotalSeconds;
                if (progressUpdate > 3.0)
                {
                    progressUpdate -= 3.0;
                    currentProgress = tracer.Progress * 100;
                }

                onScreenText = string.Format("Elapsed: {0}\nProgress: {1}%", rayTraceWatch.Elapsed.ToString(), currentProgress);
            }
            else
            {
                onScreenText = string.Format("Camera: {0}", camera.Position);
                //if (finished && !videocompiled)
                //{
                //    this.videocompiled = true;
                //    this.compileVideo();
                //    nextframe = false;
                //}
                //if (nextframe)
                //{
                //    nextframe = false;
                //    rayTraceWatch = Stopwatch.StartNew();
                //    //for (int i = 0; i < spheres.Length; i++)
                //    //{
                //    //    spheres[i].Position = new Vector3((float)Math.Sin(rot + (i * rot_space)) * 10, 3, (float)Math.Cos(rot + (i * rot_space)) * 10);
                //    //}
                //    //this.tracer.RenderAsync();
                //    this.tracer.RenderAsync();
                //}

            }

            lastState = state;
            lastMouse = mouseState;

            base.Update(gameTime);
        }
        bool showRayTraceImage;
        double progressUpdate;
        float currentProgress;
        string progressText = String.Empty;
        string onScreenText;
        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Magenta);
            if (this.useWireframe)
                GraphicsDevice.RasterizerState = wireRasterState;
            else
                GraphicsDevice.RasterizerState = solidRasterState;
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

#if DEBUG
                if (tracer.points.Count > 0 && !tracer.IsBusy)
                {
                    GraphicsDevice.BlendState = BlendState.AlphaBlend;
                    SceneObject.boundingEffect.Alpha = 0.75f;
                    SceneObject.boundingEffect.VertexColorEnabled = true;
                    //SceneObject.boundingEffect.DiffuseColor = Vector3.One;
                    SceneObject.boundingEffect.World = Matrix.Identity;
                    SceneObject.boundingEffect.Techniques[0].Passes[0].Apply();
                    GraphicsDevice.BlendState = BlendState.AlphaBlend;
                    //GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                    GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineList, tracer.points.ToArray(), 0, tracer.points.Count / 2);
                    GraphicsDevice.BlendState = BlendState.Opaque;
                }
#endif
            }

            spriteBatch.Begin();
            spriteBatch.DrawString(this.onScreenFont, onScreenText, new Vector2(16, 16), Color.White);
            spriteBatch.End();



            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
        
    }
}
