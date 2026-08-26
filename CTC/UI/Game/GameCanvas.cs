using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CTC
{
    public class GameCanvas : UIView, ICleanupable
    {
        public GameCanvas(ClientState State) : base(null, UIElementType.Window)
        {
            this.State = State;
            Protocol = State.Protocol;
            Viewport = State.Viewport;

            RegisterEvents();

            ReplayButton = new UIButton("Replay?");
            ReplayButton.Autoresizable = false;
            ReplayButton.Bounds = new Rectangle(0, 0, 90, 22);
            ReplayButton.ZOrder = 1;
            ReplayButton.Visible = false;
            ReplayButton.ButtonReleasedInside += delegate(UIButton Button, MouseState mouse)
            {
                Replay();
            };
            AddSubview(ReplayButton);

            State.SeekCompleted += OnSeekCompleted;

            UpdateName();
        }

        private ClientState State;
        private ClientViewport Viewport;
        private TibiaGameProtocol Protocol;
        private GameRenderer Renderer;

        private double LastCleanup = 0;
        private RenderTarget2D Backbuffer;
        private RenderTarget2D Lightbuffer;

        private Dictionary<MapPosition, TileAnimations> PlayingAnimations = new Dictionary<MapPosition, TileAnimations>();

        /// <summary>
        /// What is currently being said over the map, oldest first. Not per tile
        /// like the other effects: several people can talk from one tile, a
        /// message about the player follows the player, and a monster yelling
        /// from off screen has no tile on screen at all.
        /// </summary>
        private List<SpokenText> Speech = new List<SpokenText>();

        /// <summary>
        /// Who is mid-step. Read by the renderer to pick each creature's pose.
        /// </summary>
        private CreatureWalks Walks = new CreatureWalks();

        /// <summary>
        /// Offered when the recording runs out. A recording that has ended looks
        /// exactly like one that is paused, so it says which it is.
        /// </summary>
        private UIButton ReplayButton;

        private Boolean Ended
        {
            get
            {
                return State.Playback != null
                    && State.Playback.Position >= State.Playback.Duration;
            }
        }


        #region Logic Code

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            if (Backbuffer == null)
            {
                Renderer = new GameRenderer(Viewport.GameData, Walks);

                Backbuffer = new RenderTarget2D(
                    UIContext.Graphics.GraphicsDevice,
                    480, 352, false,
                    SurfaceFormat.Color, DepthFormat.None, 0,
                    RenderTargetUsage.PreserveContents
                );

                Lightbuffer = new RenderTarget2D(
                    UIContext.Graphics.GraphicsDevice,
                    480, 352, false,
                    SurfaceFormat.Color, DepthFormat.None, 0,
                    RenderTargetUsage.PreserveContents
                );
            }

            // Bounds = new Rectangle(0, 0, 883, 883 / 4 * 3);

            ReplayButton.Bounds.X = ClientBounds.Left + (ClientBounds.Width - ReplayButton.Bounds.Width) / 2;
            ReplayButton.Bounds.Y = ClientBounds.Top + ClientBounds.Height / 2 + 12;
        }

        public override void Update(GameTime Time)
        {
            base.Update(Time);

            // Effects run on the playback clock, not the wall clock. They
            // accumulate elapsed time, so left alone a spell at eight times speed
            // would still take its full second to finish, and a paused client
            // would go on animating.
            GameTime EffectTime = ScaleToPlayback(Time);
            if (EffectTime != null)
            {
                // TODO: cleanup animations outside view
                foreach (TileAnimations Animations in PlayingAnimations.Values)
                {
                    foreach (GameEffect Effect in Animations.Effects)
                        if (!Effect.Expired)
                            Effect.Update(EffectTime);
                }

                foreach (SpokenText Text in Speech)
                    if (!Text.Expired)
                        Text.Update(EffectTime);

                Walks.Update(EffectTime);
            }

            ReplayButton.Visible = Ended;

            LastCleanup += Time.ElapsedGameTime.TotalMilliseconds;
            if (LastCleanup > 1000)
            {
                Cleanup();
                LastCleanup = 0;
            }
        }

        /// <summary>
        /// Time as the recording sees it, or null when nothing should animate.
        /// </summary>
        private GameTime ScaleToPlayback(GameTime Time)
        {
            if (State.Playback == null)
                return Time;

            // Backwards, an effect has no meaning: it was cleared on the way in
            // and would only be shown running the wrong way round.
            if (!State.Playback.Playing || State.Playback.Direction < 0)
                return null;

            return new GameTime(
                Time.TotalGameTime,
                TimeSpan.FromTicks((long)(Time.ElapsedGameTime.Ticks * State.Playback.Speed)));
        }

        /// <summary>
        /// Effects accumulate elapsed time from wherever they started, so there is
        /// no meaningful way to move them to a new point on the timeline. They are
        /// decoration; drop them and let the next packets put new ones up.
        /// </summary>
        private void OnSeekCompleted(ClientState State)
        {
            PlayingAnimations.Clear();
            Speech.Clear();
            Walks.Clear();
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

            for (int i = Speech.Count - 1; i >= 0; --i)
                if (Speech[i].Expired)
                    Speech.RemoveAt(i);

            Walks.Cleanup();
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

        protected override void DrawBackground(SpriteBatch CurrentBatch)
        {
            // do nothing
        }

        public override void Draw(SpriteBatch CurrentBatch, Rectangle BoundingBox)
        {
            // Create the batch if this is the first time we're being drawn
            if (Batch == null)
                Batch = new SpriteBatch(UIContext.Graphics.GraphicsDevice);

            // Accumulate every light source into its own buffer, starting from
            // the ambient level so unlit ground still gets the time-of-day tint.
            UIContext.Graphics.GraphicsDevice.SetRenderTarget(Lightbuffer);
            UIContext.Graphics.GraphicsDevice.Clear(
                LightMap.Ambient(Viewport.WorldLight, Viewport.ViewPosition.Z));
            Batch.Begin(SpriteSortMode.Deferred, BlendState.Additive);
            Renderer.DrawLights(Batch, Viewport);
            Batch.End();

            // Draw the game to a backbuffer
            UIContext.Graphics.GraphicsDevice.SetRenderTarget(Backbuffer);
            Batch.Begin();
            Renderer.DrawScene(Batch, UIContext.GameTime, Viewport, PlayingAnimations);
            Batch.End();

            // Multiply the light over the scene: unlit areas darken, lit areas
            // keep their colour and pick up the light's tint.
            Batch.Begin(SpriteSortMode.Deferred, LightMap.Multiply);
            Batch.Draw(Lightbuffer, Vector2.Zero, Color.White);
            Batch.End();

            UIContext.Graphics.GraphicsDevice.SetRenderTarget(null);
            
            // Then start the normal drawing cycle
            BeginDraw();

            // Draw the backbuffer to the screen
            Batch.Draw(Backbuffer, ScreenClientBounds, Color.White);

            Vector2 Offset = new Vector2(ScreenClientBounds.X, ScreenClientBounds.Y);
            Vector2 Scale = new Vector2(
                Bounds.Width / 480f,
                Bounds.Height / 352f
            );
            Renderer.DrawSceneForeground(Batch, Offset, Scale, UIContext.GameTime, Viewport, PlayingAnimations);
            Renderer.DrawSpeech(Batch, ScreenClientBounds, Scale, Viewport, Speech);

            if (Ended)
                DrawEnd();

            DrawBorder(Batch);
            EndDraw();

            // GameCanvas draws itself rather than going through UIView.Draw, so
            // its one child has to be asked for explicitly.
            DrawForegroundChildren(Batch, BoundingBox);
        }

        /// <summary>
        /// The end of the recording: the picture washed towards grey, and what
        /// happened said in the middle of it.
        ///
        /// Washing rather than desaturating properly, because a real desaturation
        /// wants a shader and this client has none - the whole of it is
        /// SpriteBatch. Pulling everything a little way towards a flat grey takes
        /// the colour out of it much the same way, at the cost of some contrast.
        /// </summary>
        private void DrawEnd()
        {
            Rectangle View = ScreenClientBounds;

            UIContext.Skin.DrawRectangle(Batch, View, new Color(118, 118, 118) * 0.45f);

            Renderer.DrawBoldedText(Batch, "The end",
                new Vector2(View.Center.X, View.Center.Y - 28), true, Color.White, 2.0f);
        }

        /// <summary>
        /// Back to the beginning, running.
        /// </summary>
        private void Replay()
        {
            State.SeekTo(State.SeekableStart);
            State.Playback.Direction = 1;
            State.Playback.Speed = 1;
            State.Playback.Playing = true;
        }

        #endregion


        #region Protocol Event Handlers

        private void RegisterEvents()
        {
            Protocol.PlayerLogin.Add(OnPlayerLogin);
            Protocol.Effect.Add(OnMagicEffect);
            Protocol.ShootEffect.Add(OnShootEffect);
            Protocol.AnimatedText.Add(OnAnimatedText);
            Protocol.CreatureSpeak.Add(OnCreatureSpeak);
            Protocol.TextMessage.Add(OnTextMessage);

            Viewport.CreatureMoved += OnCreatureMoved;

            // Your own steps do not arrive as creature moves: the server scrolls
            // the map instead and says which way it went, so the direction is in
            // which packet arrived rather than in anything it carries.
            Protocol.MoveNorth.Add(OnStepNorth);
            Protocol.MoveSouth.Add(OnStepSouth);
            Protocol.MoveEast.Add(OnStepEast);
            Protocol.MoveWest.Add(OnStepWest);
        }

        /// <summary>
        /// Starts the walk cycle for a creature that just moved a tile. How long
        /// it runs for is the ground it stepped onto over how fast the creature
        /// is, which is the same arithmetic the server walked it with.
        /// </summary>
        private void OnCreatureMoved(ClientViewport Viewport, ClientCreature Creature, MapPosition From, MapPosition To)
        {
            if (State.Seeking)
                return;

            Walks.Stepped(Creature.ID, From, To, GroundSpeedAt(To), Creature.Speed);
        }

        private void OnStepNorth(Packet props) { PlayerStepped(0, -1); }
        private void OnStepSouth(Packet props) { PlayerStepped(0, 1); }
        private void OnStepEast(Packet props) { PlayerStepped(1, 0); }
        private void OnStepWest(Packet props) { PlayerStepped(-1, 0); }

        private void PlayerStepped(int DX, int DY)
        {
            if (State.Seeking || Viewport.Player == null)
                return;

            MapPosition To = Viewport.ViewPosition;
            MapPosition From = new MapPosition(To.X - DX, To.Y - DY, To.Z);

            Walks.Stepped(Viewport.Player.ID, From, To, GroundSpeedAt(To), Viewport.Player.Speed);
        }

        /// <summary>
        /// The point over a tile that whoever is standing on it hangs their name
        /// and health bar from, so anything put over that tile lines up with
        /// them. Taken when the text is made rather than when it is drawn: the
        /// creature may well have walked on by then, and the words should not
        /// slide sideways when they do.
        /// </summary>
        private Vector2 OverheadAt(MapPosition Position)
        {
            ClientTile Tile = Viewport.Map[Position];
            if (Tile != null)
                foreach (ClientThing Thing in Tile.Objects)
                    if (Thing is ClientCreature)
                        return GameSprite.OverheadOf(Viewport.GameData.GetCreatureSprite(
                            ((ClientCreature)Thing).Outfit.LookType));

            return GameSprite.DefaultOverhead;
        }

        private int GroundSpeedAt(MapPosition Position)
        {
            ClientTile Tile = Viewport.Map[Position];
            if (Tile == null || Tile.Ground == null || Tile.Ground.Type == null)
                return CreatureWalks.DefaultGroundSpeed;

            return Tile.Ground.Type.WalkSpeed;
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
            if (State.Seeking)
                return;

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
            // A seek rushes minutes of packets past in a few milliseconds. Left
            // alone that would pile up every spell cast in between and play them
            // all at once on arrival.
            if (State.Seeking)
                return;

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
            if (State.Seeking)
                return;

            MapPosition Position = (MapPosition)props["Position"];
            String Text = (String)props["Text"];
            int Color = (int)props["Color"];

            TileAnimations Animations = null;
            if (!PlayingAnimations.TryGetValue(Position, out Animations))
            {
                Animations = new TileAnimations();
                PlayingAnimations.Add(Position, Animations);
            }
            Animations.Effects.Add(new AnimatedText(Text, Color, OverheadAt(Position)));
        }

        /// <summary>
        /// Talk that happened somewhere on the map. Channel and private talk
        /// carries no position - it is the chat panel's business, not the map's.
        /// </summary>
        private void OnCreatureSpeak(Packet props)
        {
            if (State.Seeking)
                return;

            if (!props.Has("Position"))
                return;

            ClientMessage Message = (ClientMessage)props["Message"];

            int Color;
            String Speaker;
            switch (Message.Type)
            {
                case MessageType.Say:
                case MessageType.Whisper:
                case MessageType.Yell:
                case MessageType.PrivateNPCToPlayer:
                case MessageType.PrivatePlayerToNPC:
                    Color = SpokenText.Yellow;
                    Speaker = Message.Speaker;
                    break;
                case MessageType.MonsterSay:
                case MessageType.MonsterYell:
                    Color = SpokenText.Red;
                    Speaker = null;
                    break;
                default:
                    return;
            }

            MapPosition Said = (MapPosition)props["Position"];
            Speech.Add(new SpokenText(Speaker, Message.Text, Color, Said, OverheadAt(Said)));
        }

        /// <summary>
        /// Messages the server sends about you. Only the three the original
        /// client showed in the game window belong over the map - looking at
        /// something, advancing, and a warning. The rest are console lines.
        /// </summary>
        private void OnTextMessage(Packet props)
        {
            if (State.Seeking)
                return;

            ClientMessage Message = (ClientMessage)props["Message"];

            int Color;
            switch (Message.Type)
            {
                case MessageType.Description:
                    Color = SpokenText.Green;
                    break;
                case MessageType.Advance:
                    Color = SpokenText.White;
                    break;
                case MessageType.Warning:
                    Color = SpokenText.Red;
                    break;
                default:
                    return;
            }

            Speech.Add(new SpokenText(Message.Text, Color, OverheadAt(Viewport.ViewPosition)));
        }

        #endregion
    }
}
