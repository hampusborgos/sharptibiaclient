using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    public class TileAnimations
    {
        public List<GameEffect> Effects = new List<GameEffect>();

        public Boolean Empty
        {
            get
            {
                return Effects.Count == 0;
            }
        }
    }

    class GameRenderer
    {
        TibiaGameData GameData;
        GraphicsDevice Device;

        /// <summary>
        /// Who is mid-step, which is all the renderer needs to know to pick a
        /// creature's pose. Owned by the canvas, which keeps it wound. The
        /// renderers that only draw inventory items leave it null.
        /// </summary>
        CreatureWalks Walks;

        public GameRenderer(TibiaGameData GameData, CreatureWalks Walks = null)
        {
            this.Device = UIContext.Graphics.GraphicsDevice;
            this.GameData = GameData;
            this.Walks = Walks;
        }

        #region Drawing Code

        /// <summary>
        /// Tibia's 216 colour palette: six steps of 51 per channel, red counting
        /// slowest. Animated text arrives as an index into it, and the message
        /// colours in SpokenText are the classic entries of the same table.
        ///
        /// The red channel used to divide by six rather than five and never
        /// reached zero, which tinted every colour - pure green came out
        /// (35, 255, 0).
        /// </summary>
        public static Color MakeColor(int id)
        {
            return new Color(
                (byte)(((id / 36) % 6) * 51),
                (byte)(((id / 6) % 6) * 51),
                (byte)((id % 6) * 51));
        }

        private void DrawImage(SpriteBatch Batch, GameImage Image, Rectangle dest, Color clr)
        {
            if (Image.Texture == null)
            {
                Texture2D Texture = new Texture2D(Device, 32, 32, false, SurfaceFormat.Color);
                Texture.SetData<Byte>(Image.LoadRGBA());
                Image.Texture = Texture;
            }

            Batch.Draw(Image.Texture, dest, clr);
        }

        /// <summary>
        /// Colourised creature images, keyed by the two sprites they were built
        /// from and the four colour indices. An outfit is worn by everyone wearing
        /// it, so the cache holds one texture per distinct look rather than per
        /// creature, and a recording only ever shows a handful of them.
        /// </summary>
        private readonly Dictionary<UInt64, Texture2D> ColorizedImages = new Dictionary<UInt64, Texture2D>();

        /// <summary>
        /// Draws a creature image dyed by its outfit.
        ///
        /// The sprite itself is painted in greys; the colours live in a second
        /// layer that masks it in pure yellow, red, green and blue for head, body,
        /// legs and feet. Multiplying the grey through the masked colour is what
        /// turns one sprite sheet into every outfit in the game.
        /// </summary>
        private void DrawImage(SpriteBatch Batch, GameImage Image, GameImage Template, ClientOutfit Outfit, Rectangle dest, Color clr)
        {
            UInt64 Key = ((UInt64)(UInt16)Image.ID << 48)
                       | ((UInt64)(UInt16)Template.ID << 32)
                       | ((UInt64)(Byte)Outfit.LookHead << 24)
                       | ((UInt64)(Byte)Outfit.LookBody << 16)
                       | ((UInt64)(Byte)Outfit.LookLegs << 8)
                       | ((UInt64)(Byte)Outfit.LookFeet);

            Texture2D Texture;
            if (!ColorizedImages.TryGetValue(Key, out Texture))
            {
                Texture = new Texture2D(Device, 32, 32, false, SurfaceFormat.Color);
                Texture.SetData<Byte>(OutfitColors.Dye(Image.LoadRGBA(), Template.LoadRGBA(), Outfit));
                ColorizedImages[Key] = Texture;
            }

            Batch.Draw(Texture, dest, clr);
        }

        public void DrawSprite(SpriteBatch Batch, GameTime Time, ClientTile Tile, GameSprite Sprite, int SubType, int Frame, Vector2 Position, Color clr)
        {
            Vector2 tmp = Position;
            DrawSprite(Batch, Time, Tile, Sprite, SubType, Frame, ref tmp, clr);
        }

        public void DrawSprite(SpriteBatch Batch, GameTime Time, ClientTile Tile, GameSprite Sprite, int SubType, int Frame, ref Vector2 Position, Color clr)
        {
            if (Sprite == null)
                return;

            int xdiv = 0, ydiv = 0, zdiv = 0;

            if (Tile != null)
            {
                MapPosition mPos = Tile.Position;
                xdiv = mPos.X % Sprite.XDiv;
                ydiv = mPos.Y % Sprite.YDiv;
                zdiv = mPos.Z % Sprite.ZDiv;
            }

            /*
            if (Sprite.IsHangable)
            {
                if (Tile->hasProperty(ISVERTICAL))
                {
                    xdiv = 2;
                }
                else if (Tile->hasProperty(ISHORIZONTAL))
                {
                    xdiv = 1;
                }
                else
                {
                    xdiv = -0;
                }
            }
            else */ if (Sprite.IsStackable)
            {
                if (SubType <= 1)
                    SubType = 0;
                else if (SubType <= 2)
                    SubType = 1;
                else if (SubType <= 3)
                    SubType = 2;
                else if (SubType <= 4)
                    SubType = 3;
                else if (SubType < 10)
                    SubType = 4;
                else if (SubType < 25)
                    SubType = 5;
                else if (SubType < 50)
                    SubType = 6;
                else
                    SubType = 7;
            }

            Vector2 Offset = Position;

            Offset.X += Sprite.RenderOffset;
            Offset.Y += Sprite.RenderOffset;

            if (Frame == -1)
                Frame = (int)(Time.TotalGameTime.TotalMilliseconds / Sprite.AnimationSpeed);

            for (int cx = 0; cx != Sprite.Width; cx++)
            {
                for (int cy = 0; cy != Sprite.Height; cy++)
                {
                    for (int cf = 0; cf != Sprite.BlendFrames; cf++)
                    {
                        GameImage Image = Sprite.GetImage(
                            cx, cy, cf,
                            SubType,
                            xdiv,
                            ydiv,
                            zdiv,
                            Frame
                        );

                        Rectangle rect = new Rectangle((int)Offset.X - 32 * cx, (int)Offset.Y - 32 * cy, 32, 32);
                        DrawImage(Batch, Image, rect, clr);
                    }
                }
            }

            if (Sprite.RenderHeight > 0)
            {
                Position.X -= Sprite.RenderHeight;
                Position.Y -= Sprite.RenderHeight;
            }
        }

        public void DrawInventoryItem(SpriteBatch Batch, ClientItem Item, Rectangle rect)
        {
            if (Item.Sprite == null)
                return;

            DrawSprite(Batch, UIContext.GameTime, null, Item.Sprite, Item.Subtype, 0, new Vector2(rect.X, rect.Y), Color.White);

            if (Item.Type.IsStackable)
            {
                String count = Item.Subtype.ToString();
                Vector2 textSize = UIContext.StandardFont.MeasureString(count);
                DrawBoldedText(Batch, count, new Vector2(rect.X + 32 - textSize.X - 1, rect.Y + 32 - textSize.Y + 1), false, Color.LightGray);
            }
        }

        public void DrawInventorySlot(SpriteBatch Batch, Rectangle rect)
        {
            UIContext.Skin.DrawBox(Batch, UIElementType.InventorySlot, rect);
        }

        /// <summary>
        /// A creature mid-step, held back until the rest of its floor is drawn.
        /// </summary>
        private struct Sliding
        {
            public ClientCreature Creature;
            public Vector2 Position;

            public Sliding(ClientCreature Creature, Vector2 Position)
            {
                this.Creature = Creature;
                this.Position = Position;
            }
        }

        /// <summary>
        /// How far the world is drawn from where the map says it is, in map
        /// pixels.
        ///
        /// The player is always in the middle of the view, so a step of theirs is
        /// the world moving rather than them: the map has already put them on the
        /// tile they are walking to, and everything else is drawn back by as far
        /// as they still have to go. Their own offset then cancels it out and
        /// leaves them centred, walking on the spot while the ground slides under
        /// them.
        /// </summary>
        private Vector2 WorldSlide(ClientViewport Viewport)
        {
            if (Walks == null || Viewport.Player == null)
                return Vector2.Zero;

            return -Walks.Offset(Viewport.Player.ID);
        }

        public void DrawCreature(SpriteBatch Batch, GameTime Time, ClientCreature Creature, Vector2 Offset, Color clr)
        {
            if (Creature.Outfit.LookItem != 0)
            {
                DrawSprite(Batch, Time, null, GameData.GetItemSprite(Creature.Outfit.LookItem), 1, -1, Offset, clr);
            }
            else if (Creature.Outfit.LookType != 0)
            {
                GameSprite Sprite = GameData.GetCreatureSprite(Creature.Outfit.LookType);

                Offset.X += Sprite.RenderOffset;
                Offset.Y += Sprite.RenderOffset;

                if (Walks != null)
                    Offset += Walks.Offset(Creature.ID);
                if (Sprite != null)
                {
                    for (int cx = 0; cx != Sprite.Width; ++cx)
                    {
                        for (int cy = 0; cy != Sprite.Height; ++cy)
                        {
                            int Phase = Walks == null ? 0 : Walks.Phase(Creature.ID, Sprite.Phases);

                            GameImage Image = Sprite.GetImage(cx, cy, Creature.Direction, 0, Phase);
                            Rectangle rect = new Rectangle((int)Offset.X - 32 * cx, (int)Offset.Y - 32 * cy, 32, 32);

                            if (Sprite.HasColorTemplate)
                                DrawImage(Batch, Image, Sprite.GetImage(cx, cy, Creature.Direction, 1, Phase), Creature.Outfit, rect, clr);
                            else
                                DrawImage(Batch, Image, rect, clr);
                        }
                    }
                }
            }
        }

        public void DrawText(SpriteBatch Batch, String Text, Vector2 Offset, Color Color)
        {
            Offset.X = (int)Offset.X;
            Offset.Y = (int)Offset.Y;

            Batch.DrawString(
                UIContext.StandardFont, Text, Offset,
                Color, 0.0f, new Vector2(0.0f, 0.0f),
                1.0f, SpriteEffects.None, 0.5f
           );
        }

        public void DrawBoldedText(SpriteBatch Batch, String Text, Vector2 Offset, Boolean Centered, Color Primary)
        {
            Offset.X = (int)Offset.X;
            Offset.Y = (int)Offset.Y;

            if (Centered)
            {
                Vector2 TextSize = UIContext.StandardFont.MeasureString(Text);
                Offset.X -= (int)(TextSize.X / 2);
            }
            DrawText(Batch, Text, new Vector2(Offset.X + 1, Offset.Y), Color.Black);
            DrawText(Batch, Text, new Vector2(Offset.X - 1, Offset.Y), Color.Black);
            DrawText(Batch, Text, new Vector2(Offset.X, Offset.Y + 1), Color.Black);
            DrawText(Batch, Text, new Vector2(Offset.X, Offset.Y - 1), Color.Black);
            DrawText(Batch, Text, new Vector2(Offset.X, Offset.Y), Primary);
        }

        public Color LifeColorForCreature(ClientCreature Creature)
        {
            ColorGradient cg = UIContext.Skin.Gradient("Health");
            if (cg != null)
                return cg.Sample(Creature.HealthPercent);
            return new Color(255, 255, 255);
        }

        public void DrawCreatureBars(SpriteBatch Batch, ClientCreature Creature, Vector2 Offset)
        {
            if (Creature.Name != "")
            {
                GameSprite Sprite = GameData.GetCreatureSprite(Creature.Outfit.LookType);
                Vector2 TextSize = UIContext.StandardFont.MeasureString(Creature.Name);
                Color LifeColor = LifeColorForCreature(Creature);
                
                // Put at the center of the sprite
                Offset.X += Sprite.Width * 16;
                Offset.Y -= Sprite.Height * 16;
                
                // Render offsets are negative
                Offset.X += Sprite.RenderOffset;
                Offset.Y += Sprite.RenderOffset;

                // Render the text
                Vector2 TextOffset = Offset;
                // Move it above the health bar
                TextOffset.X = (int)(TextOffset.X - TextSize.X / 2);
                TextOffset.Y -= 16;
                DrawBoldedText(Batch, Creature.Name, TextOffset, false, LifeColor);

                // 
                Rectangle BlackBar = new Rectangle(
                    (int)(Offset.X - 14),
                    (int)Offset.Y,
                    28, 4
                );
                UIContext.Skin.DrawBorderedRectangle(Batch, BlackBar, Color.Black);

                Rectangle InsideBar = BlackBar.Subtract(new Margin(1));
                if (Creature.MaxHealth > 0)
                    InsideBar.Width = InsideBar.Width * Creature.Health / Creature.MaxHealth;
                UIContext.Skin.DrawBorderedRectangle(Batch, InsideBar, LifeColor);
            }
        }

        private void DrawTile(SpriteBatch Batch, GameTime Time, Vector2 Position, ClientTile Tile, TileAnimations Animations, List<Sliding> Deferred)
        {
            if (Tile == null)
                return;

            // Draw ground
            if (Tile.Ground != null)
                DrawSprite(Batch, Time, Tile, Tile.Ground.Sprite, Tile.Ground.Subtype, -1, ref Position, Color.White);

            foreach (ClientThing Thing in Tile.ObjectsByDrawOrder)
            {
                if (Thing is ClientCreature)
                {
                    ClientCreature Creature = (ClientCreature)Thing;

                    // A creature between two tiles hangs over one of them, and
                    // the tiles are drawn in order: whichever it hangs over is
                    // painted after it half the time. So it waits for the rest of
                    // its floor and goes on top of it.
                    if (Deferred != null && Walks != null && Walks.Stepping(Creature.ID))
                        Deferred.Add(new Sliding(Creature, Position));
                    else
                        DrawCreature(Batch, Time, Creature, Position, Color.White);
                }
                else
                {
                    ClientItem Item = (ClientItem)Thing;
                    DrawSprite(Batch, Time, Tile, Item.Sprite, Item.Subtype, -1, ref Position, Color.White);
                }
            }

            if (Animations != null)
            {
                foreach (GameEffect Effect in Animations.Effects)
                {
                    if (!Effect.Expired)
                    {
                        if (Effect is MagicEffect)
                        {
                            MagicEffect Magic = (MagicEffect)Effect;
                            DrawSprite(Batch, Time, Tile, Magic.Sprite, -1, Magic.Frame, Position, Color.White);
                        }
                        else if (Effect is DistanceEffect)
                        {
                            DistanceEffect Distance = (DistanceEffect)Effect;
                            DrawSprite(Batch, Time, Tile, Distance.Sprite, Distance.Frame, 0, Position + Distance.Offset, Color.White);
                        }
                    }
                }
            }
        }

        public void DrawTileForeground(SpriteBatch Batch, GameTime Time, Vector2 Offset, Vector2 Scale, ClientTile Tile, TileAnimations Animations)
        {
            if (Tile == null)
                return;

            foreach (ClientThing Thing in Tile.Objects)
            {
                if (Thing is ClientCreature)
                {
                    ClientCreature Creature = (ClientCreature)Thing;

                    // The name and bar ride along with a creature mid-step.
                    Vector2 Slide = Walks == null ? Vector2.Zero : Walks.Offset(Creature.ID) * Scale;
                    DrawCreatureBars(Batch, Creature, Offset + Slide);
                }
            }

            if (Animations != null)
            {
                foreach (GameEffect Effect in Animations.Effects)
                {
                    if (!Effect.Expired && Effect is AnimatedText)
                    {
                        AnimatedText Text = (AnimatedText)Effect;
                        Vector2 DrawOffset = Offset + Text.Offset;
                        // DrawOffset.X += 16;
                        // DrawOffset.Y += 26;
                        DrawBoldedText(Batch, Text.Text, DrawOffset, true, MakeColor(Text.Color));
                    }
                }
            }
        }

        /// <summary>
        /// Where the top left corner of a map position lands on the screen. The
        /// scene is drawn around Viewport.ViewPosition, which sits seven tiles in
        /// and five down.
        ///
        /// Positions on another floor are projected as if they were on this one.
        /// The only thing that reaches this from another floor is a yell, and a
        /// yell from a floor away is clamped to the edge of the view regardless.
        /// </summary>
        public static Vector2 ScreenPositionOf(ClientViewport Viewport, Vector2 ScreenOffset, Vector2 Scale, MapPosition Position)
        {
            MapPosition Center = Viewport.ViewPosition;

            return new Vector2(
                ScreenOffset.X + (32 * (Position.X - Center.X + 7)) * Scale.X,
                ScreenOffset.Y + (32 * (Position.Y - Center.Y + 5)) * Scale.Y
            );
        }

        /// <summary>
        /// Draws what is being said over the map. Text is screen space, like the
        /// names and health bars, so it stays crisp at any window size.
        /// </summary>
        public void DrawSpeech(SpriteBatch Batch, Rectangle View, Vector2 Scale, ClientViewport Viewport, List<SpokenText> Speech)
        {
            float LineHeight = UIContext.StandardFont.LineSpacing;
            Vector2 ScreenOffset = new Vector2(View.X, View.Y) + WorldSlide(Viewport) * Scale;

            // How much is already stacked over each tile. Newest first, so the
            // last thing said sits closest to the head and pushes older lines up.
            Dictionary<MapPosition, float> Stacked = new Dictionary<MapPosition, float>();

            for (int i = Speech.Count - 1; i >= 0; --i)
            {
                SpokenText Text = Speech[i];
                if (Text.Expired)
                    continue;

                MapPosition Anchor = Text.FollowsPlayer ? Viewport.ViewPosition : Text.Position;
                Vector2 Origin = ScreenPositionOf(Viewport, ScreenOffset, Scale, Anchor);

                float Height = Text.Lines.Count * LineHeight;
                float Used;
                Stacked.TryGetValue(Anchor, out Used);
                Stacked[Anchor] = Used + Height;

                // Clear of the name and health bar, which take the 32 pixels
                // above the tile, with a line's worth of air under the text.
                Vector2 At = new Vector2(
                    Origin.X + 16 * Scale.X,
                    Origin.Y - 40 - Height - Used
                );

                // A creature can talk from outside the view - a monster yelling
                // a screen away - so the text is pinned to the edge it would
                // otherwise go past, pointing at where the shouting came from.
                At.X = MathHelper.Clamp(At.X, View.Left + Text.Width / 2 + 2, View.Right - Text.Width / 2 - 2);
                At.Y = MathHelper.Clamp(At.Y, View.Top + 2, View.Bottom - Height - 2);

                Color Tint = MakeColor(Text.Color);
                foreach (String Line in Text.Lines)
                {
                    DrawBoldedText(Batch, Line, At, true, Tint);
                    At.Y += LineHeight;
                }
            }
        }

        public void DrawSceneForeground(SpriteBatch Batch, Vector2 ScreenOffset, Vector2 Scale, GameTime Time, ClientViewport Viewport, Dictionary<MapPosition, TileAnimations> PlayingAnimations = null)
        {
            ScreenOffset += WorldSlide(Viewport) * Scale;

            // Which tiles are on screen. Above ground the window shifts with the
            // floor; where they draw is ScreenPositionOf's business.
            MapPosition Center = Viewport.ViewPosition;

            if (Center.Z <= 7)
            {
                Center.Y -= (7 - Center.Z);
                Center.X -= (7 - Center.Z);
            }

            for (int X = Center.X - 8; X <= Center.X + 8; ++X)
            {
                for (int Y = Center.Y - 6; Y <= Center.Y + 6; ++Y)
                {
                    MapPosition Position = new MapPosition(X, Y, Center.Z);
                    ClientTile Tile = Viewport.Map[Position];

                    TileAnimations Animations = null;
                    if (PlayingAnimations != null && Tile != null)
                        PlayingAnimations.TryGetValue(Tile.Position, out Animations);
                    DrawTileForeground(Batch, Time,
                        ScreenPositionOf(Viewport, ScreenOffset, Scale, Position), Scale, Tile, Animations);
                }
            }
        }

        public void DrawScene(SpriteBatch Batch, GameTime Time, ClientViewport Viewport, Dictionary<MapPosition, TileAnimations> PlayingAnimations = null)
        {
            MapPosition Center = Viewport.ViewPosition;

            int StartZ = 7;
            int EndZ = Center.Z;
            if (Center.Z <= 7)
            {
                Center.Y -= (7 - Center.Z);
                Center.X -= (7 - Center.Z);
            }
            else
            {
                StartZ = Math.Min(Center.Z + 2, 15);
                EndZ = Center.Z;
                Center.X -= 2;
                Center.Y -= 2;
            }

            Vector2 TopLeft = new Vector2(
                -(Center.X - 7) * 32,
                -(Center.Y - 5) * 32
            ) + WorldSlide(Viewport);

            List<Sliding> Deferred = new List<Sliding>();

            for (int Z = StartZ; Z >= EndZ; --Z)
            {
                for (int X = Center.X - 8; X <= Center.X + 9; ++X)
                {
                    for (int Y = Center.Y - 6; Y <= Center.Y + 7; ++Y)
                    {
                        ClientTile Tile = Viewport.Map[new MapPosition(X, Y, Z)];

                        Vector2 pos = new Vector2(32 * X + TopLeft.X, 32 * Y + TopLeft.Y);
                        
                        TileAnimations Animations = null;
                        if (PlayingAnimations != null && Tile != null)
                            PlayingAnimations.TryGetValue(Tile.Position, out Animations);
                        DrawTile(Batch, Time, pos, Tile, Animations, Deferred);
                    }
                }

                foreach (Sliding Walker in Deferred)
                    DrawCreature(Batch, Time, Walker.Creature, Walker.Position, Color.White);
                Deferred.Clear();

                TopLeft -= new Vector2(32, 32);
            }
        }

        /// <summary>
        /// Accumulates every light source on screen into the currently bound
        /// render target. Walks the same floors and tiles as DrawScene so the
        /// glows land exactly where the sprites that cast them were drawn.
        ///
        /// The caller is responsible for clearing the target to the ambient
        /// colour first and for compositing the result over the scene.
        /// </summary>
        public void DrawLights(SpriteBatch Batch, ClientViewport Viewport)
        {
            MapPosition Center = Viewport.ViewPosition;

            int StartZ = 7;
            int EndZ = Center.Z;
            if (Center.Z <= 7)
            {
                Center.Y -= (7 - Center.Z);
                Center.X -= (7 - Center.Z);
            }
            else
            {
                StartZ = Math.Min(Center.Z + 2, 15);
                EndZ = Center.Z;
                Center.X -= 2;
                Center.Y -= 2;
            }

            Vector2 TopLeft = new Vector2(
                -(Center.X - 7) * 32,
                -(Center.Y - 5) * 32
            ) + WorldSlide(Viewport);

            Texture2D Glow = LightMap.Glow(Batch.GraphicsDevice);

            for (int Z = StartZ; Z >= EndZ; --Z)
            {
                // A tile's light can spill onto neighbours, so sweep a little
                // wider than the visible area to avoid glows popping in at the
                // screen edge.
                for (int X = Center.X - 12; X <= Center.X + 13; ++X)
                {
                    for (int Y = Center.Y - 10; Y <= Center.Y + 11; ++Y)
                    {
                        ClientTile Tile = Viewport.Map[new MapPosition(X, Y, Z)];
                        if (Tile == null)
                            continue;

                        Vector2 Position = new Vector2(32 * X + TopLeft.X, 32 * Y + TopLeft.Y);

                        if (Tile.Ground != null && Tile.Ground.Type != null && Tile.Ground.Type.MakesLight)
                            DrawLight(Batch, Glow, Position,
                                Tile.Ground.Type.LightLevel, Tile.Ground.Type.LightColor);

                        foreach (ClientThing Thing in Tile.ObjectsByDrawOrder)
                        {
                            if (Thing is ClientCreature)
                            {
                                ClientCreature Creature = (ClientCreature)Thing;
                                Vector2 Slide = Walks == null ? Vector2.Zero : Walks.Offset(Creature.ID);
                                DrawLight(Batch, Glow, Position + Slide,
                                    Creature.Light.Level, Creature.Light.Color);
                            }
                            else
                            {
                                ClientItem Item = (ClientItem)Thing;
                                if (Item.Type != null && Item.Type.MakesLight)
                                    DrawLight(Batch, Glow, Position,
                                        Item.Type.LightLevel, Item.Type.LightColor);
                            }
                        }
                    }
                }
                TopLeft -= new Vector2(32, 32);
            }
        }

        /// <summary>
        /// Draws one radial glow. Level is a radius in tiles, Color an index
        /// into the Tibia palette.
        /// </summary>
        private void DrawLight(SpriteBatch Batch, Texture2D Glow, Vector2 TilePosition, int Level, int Color)
        {
            if (Level <= 0)
                return;

            // Light is centred on the tile, not its top-left corner.
            Vector2 Origin = TilePosition + new Vector2(16, 16);
            float Radius = Level * 32.0f;

            Rectangle Destination = new Rectangle(
                (int)(Origin.X - Radius),
                (int)(Origin.Y - Radius),
                (int)(Radius * 2),
                (int)(Radius * 2));

            Batch.Draw(Glow, Destination, LightMap.FromEightBit(Color));
        }

        #endregion
    }
}
