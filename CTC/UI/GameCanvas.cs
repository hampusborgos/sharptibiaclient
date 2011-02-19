using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    public class GameCanvas : UIPanel, ICleanupable
    {
        public GameCanvas(UIPanel Parent, ClientState State)
            : base(Parent)
        {
            Protocol = State.Protocol;
            Viewport = State.Viewport;
            Renderer = new GameRenderer(Context, Viewport.GameData);

            Backbuffer = new RenderTarget2D(
                Context.Graphics.GraphicsDevice,
                480, 352, false,
                SurfaceFormat.Color, DepthFormat.None, 0,
                RenderTargetUsage.PreserveContents
            );
            Bounds = new Rectangle(0, 0, 883, 883 / 4 * 3);

            RegisterEvents();

            UpdateName();
        }

        private ClientViewport Viewport;
        private TibiaGameProtocol Protocol;
        private GameRenderer Renderer;

        private double LastCleanup = 0;
        private RenderTarget2D Backbuffer;

        private Dictionary<MapPosition, TileAnimations> PlayingAnimations = new Dictionary<MapPosition, TileAnimations>();

        #region Logic Code

        public override void Update(GameTime Time)
        {
            base.Update(Time);

            // TODO: cleanup animations outside view
            foreach (TileAnimations Animations in PlayingAnimations.Values)
            {
                foreach (GameEffect Effect in Animations.Effects)
                    if (!Effect.Expired)
                        Effect.Update(Time);
            }

            LastCleanup += Time.ElapsedGameTime.TotalMilliseconds;
            if (LastCleanup > 1000)
            {
                Cleanup();
                LastCleanup = 0;
            }
        }

        public void Cleanup()
        {
            List<GameEffect> ToRemove = new List<GameEffect>();
            List<MapPosition> ToRemoveAnims = new List<MapPosition>();

            // TODO: cleanup animations outside view
            foreach (KeyValuePair<MapPosition, TileAnimations> Anim in PlayingAnimations)
            {
                ToRemove.Clear();

                foreach (GameEffect Effect in Anim.Value.Effects)
                    if (Effect.Expired)
                        ToRemove.Add(Effect);
                
                foreach (GameEffect Effect in ToRemove)
                    Anim.Value.Effects.Remove(Effect);

                if (Anim.Value.Empty)
                    ToRemoveAnims.Add(Anim.Key);
            }

            foreach (MapPosition Position in ToRemoveAnims)
                PlayingAnimations.Remove(Position);
        }

        private void UpdateName()
        {
            /*
            if (Viewport.Player != null)
                Name = Viewport.Player.Name + " (localhost:7171)";
            else
                Name = "No Player (localhost:7171)";
             */
        }

        #endregion

        #region Drawing Code

        public override void Draw(SpriteBatch CurrentBatch)
        {
            // Draw scene

            // Draw a generic border

            Context.Graphics.GraphicsDevice.SetRenderTarget(Backbuffer);
            Batch.Begin();
            Renderer.DrawScene(Batch, Context.GameTime, Viewport, PlayingAnimations);
            Batch.End();
            Context.Graphics.GraphicsDevice.SetRenderTarget(null);
            
            BeginDraw();

            Rectangle C = ClientBounds;
            C.X += ScreenBounds.X;
            C.Y += ScreenBounds.Y;
            Batch.Draw(Backbuffer, C, Color.White);

            Vector2 Scale = new Vector2(
                C.Width / 480f,
                C.Height / 352f
            );
            Renderer.DrawSceneForeground(Batch, Scale, Context.GameTime, Viewport, PlayingAnimations);


            DrawBorder(Batch);
            EndDraw();

            // Draw taskbar buttons
            DrawChildren(CurrentBatch);
        }

        #endregion

        #region Protocol Event Handlers

        private void RegisterEvents()
        {
            Protocol.PlayerLogin.Add(OnPlayerLogin);
            Protocol.Effect.Add(OnMagicEffect);
            Protocol.ShootEffect.Add(OnShootEffect);
            Protocol.AnimatedText.Add(OnAnimatedText);
        }

        private void OnPlayerLogin(Packet props)
        {
            // Next map description will contain the player
            Protocol.MapDescription.Add(OnMapDescription);
        }

        private void OnMapDescription(Packet props)
        {
            // Update the name of the window
            UpdateName();
            // Don't need to receive further updates
            Protocol.MapDescription.Remove(OnMapDescription);
        }

        private void OnShootEffect(Packet props)
        {
            MapPosition FromPosition = (MapPosition)props["From"];
            MapPosition ToPosition = (MapPosition)props["To"];
            int Type = (int)props["Effect"];

            MapPosition Max = new MapPosition();
            Max.X = Math.Max(FromPosition.X, ToPosition.X);
            Max.Y = Math.Max(FromPosition.Y, ToPosition.Y);
            Max.Z = ToPosition.Z;

            TileAnimations Animations = null;
            if (!PlayingAnimations.TryGetValue(Max, out Animations))
            {
                Animations = new TileAnimations();
                PlayingAnimations.Add(Max, Animations);
            }
            Animations.Effects.Add(new DistanceEffect(Viewport.GameData, Type, FromPosition, ToPosition));
        }

        private void OnMagicEffect(Packet props)
        {
            MapPosition Position = (MapPosition)props["Position"];
            MagicEffect Effect = new MagicEffect(Viewport.GameData, (int)props["Effect"]);

            TileAnimations Animations = null;
            if (!PlayingAnimations.TryGetValue(Position, out Animations))
            {
                Animations = new TileAnimations();
                PlayingAnimations.Add(Position, Animations);
            }
            Animations.Effects.Add(Effect);
        }

        private void OnAnimatedText(Packet props)
        {
            MapPosition Position = (MapPosition)props["Position"];
            String Text = (String)props["Text"];
            int Color = (int)props["Color"];

            TileAnimations Animations = null;
            if (!PlayingAnimations.TryGetValue(Position, out Animations))
            {
                Animations = new TileAnimations();
                PlayingAnimations.Add(Position, Animations);
            }
            Animations.Effects.Add(new AnimatedText(Text, Color));
        }

        #endregion
    }
}
