using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace CTC
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager Graphics;

        GameDesktop Desktop;
        MouseState LastMouseState;

        /// <summary>
        /// The .tmv recording to play back, resolved from the command line.
        /// </summary>
        private readonly String MoviePath;

        /// <summary>
        /// Where playback starts and how fast, from --start and --speed.
        /// </summary>
        private readonly TimeSpan StartPosition;
        private readonly double StartSpeed;

        public Game(String[] Args)
        {
            MoviePath = AppPaths.ResolveRecording(Args);
            StartPosition = AppPaths.StartPosition(Args);
            StartSpeed = AppPaths.StartSpeed(Args);

            Graphics = new GraphicsDeviceManager(this);
            

            Graphics.PreparingDeviceSettings += PrepareDevice;
            Graphics.PreferredBackBufferWidth = 1280;
            Graphics.PreferredBackBufferHeight = 800;
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            // Setup the window
            // The original ran uncapped, which on modern hardware just pegs a GPU core.
            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(1.0 / 120.0);
            Graphics.SynchronizeWithVerticalRetrace = false;
            //graphics.GraphicsDevice.PresentationParameters.PresentationInterval = PresentInterval.One;
            //graphics.PreferWaitForVerticalTrace = false;
            IsMouseVisible = true;
            Window.AllowUserResizing = true;

            Graphics.ApplyChanges();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Initialize the context
            UIContext.Initialize(Window, Graphics, Content);
            UIContext.Load();

            // Create the game frame
            Desktop = new GameDesktop();
            Desktop.Load();
            Desktop.CreatePanels();

            // Initial layout of all views
            Desktop.LayoutSubviews();
            Desktop.NeedsLayout = true;

            ///////////////////////////////////////////////////////////////////
            // Read the TMV file we were pointed at

            FileInfo file = new FileInfo(MoviePath);
            Stream virtualStream = null;

            FileStream fileStream = file.OpenRead();
            if (file.Extension == ".tmv")
                virtualStream = new System.IO.Compression.GZipStream(fileStream, System.IO.Compression.CompressionMode.Decompress);
            else
                virtualStream = fileStream;

            TibiaMovieRecording Recording = TibiaMovieRecording.Load(virtualStream, file.Name);
            ClientState State = new ClientState(Recording);

            // The tab wants a player name and the parser cannot read a creature
            // before it has a Player, so always get as far as the login packet.
            State.PrimeToLogin();
            Desktop.AddClient(State);

            State.Playback.Speed = StartSpeed;
            State.Playback.Playing = true;
            if (StartPosition > TimeSpan.Zero)
                State.SeekTo(StartPosition);
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
            UIContext.Input.Update(gameTime);
            MouseState mouse = UIContext.Input.CurrentMouseState;

            if (UIContext.Input.Finished)
                Exit();

            // Do input handling

            // First check left mouse button
            if (LastMouseState != null)
            {
                if (LastMouseState.LeftButton != mouse.LeftButton)
                    Desktop.MouseLeftClick(mouse);
            }

            // Send the mouse moved event
            if (LastMouseState.X != mouse.X || LastMouseState.Y != mouse.Y)
                Desktop.MouseMove(mouse);

            // Save the state for next frame so we can see what changed
            LastMouseState = mouse;

            // Update the game state
            Desktop.Update(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            try
            {
                Desktop.Draw(null, Window.ClientBounds);
            }
            catch (Exception e)
            {
                throw;
            }

            base.Draw(gameTime);

            SaveScreenshotIfRequested();
        }

        /// <summary>
        /// Writes the frame that was just drawn to disk, when a script asked for
        /// it. Reading the back buffer here means a test gets exactly what the
        /// user would see, without going anywhere near the window server.
        /// </summary>
        private void SaveScreenshotIfRequested()
        {
            string Path = UIContext.Input.PendingScreenshot;
            if (Path == null)
                return;
            UIContext.Input.PendingScreenshot = null;

            int Width = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int Height = GraphicsDevice.PresentationParameters.BackBufferHeight;

            Color[] Pixels = new Color[Width * Height];
            GraphicsDevice.GetBackBufferData(Pixels);

            using (Texture2D Frame = new Texture2D(GraphicsDevice, Width, Height))
            {
                Frame.SetData(Pixels);
                using (Stream Output = File.Create(Path))
                    Frame.SaveAsPng(Output, Width, Height);
            }

            Log.Notice("Saved screenshot to " + Path, this);
        }

        protected void PrepareDevice(object sender, PreparingDeviceSettingsEventArgs e)
        {
            e.GraphicsDeviceInformation.PresentationParameters.RenderTargetUsage = RenderTargetUsage.PreserveContents;
        }
    }
}
