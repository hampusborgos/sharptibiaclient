using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace CTC
{
    public class GameSprite
    {
        private UInt16 id;
        public UInt16 ID {
            get {return id;}
        }
        public Boolean IsStackable = false;
        public Boolean IsFluidContainer = false;
        public Boolean IsSplash = false;
        public Boolean FloorTransparent = false;
        public Boolean ColorTemplate = false;

        public int RenderHeight = 0;
        public int RenderOffset = 0;
        public int HealthOffset = 0;
        public int AnimationSpeed = 500;

        public int Height = 1;
        public int Width = 1;
        public int BlendFrames = 1;
        public int XDiv = 1;
        public int YDiv = 1;
        public int ZDiv = 1;
        public int AnimationLength = 0;

        public int SpriteCount
        {
            get
            {
                return Height * Width * BlendFrames * XDiv * YDiv * ZDiv * AnimationLength;
            }
        }

        public List<GameImage> Images = new List<GameImage>();

        public GameSprite(UInt16 id)
        {
            this.id = id;
        }

        public GameImage GetImage(int X, int Y, int Frame, int Count, int XDiv, int YDiv, int ZDiv, int Time)
        {
            int v;
            if (Count >= 0 && this.Height <= 1 && this.Width <= 1)
                v = Count;
            else
                v = ((((((Time % this.AnimationLength) * this.YDiv + YDiv) * this.XDiv + XDiv) * this.BlendFrames + Frame) * this.Height + Y) * this.Width + X);
            
            if (v >= this.Images.Count)
            {
                if (this.Images.Count == 1)
                {
                    v = 0;
                }
                else
                {
                    v %= this.Images.Count;
                }
            }
            return this.Images[v];
        }

        /// <summary>
        /// True when the second sprite layer is an outfit colour mask rather than
        /// another blend frame, which is what makes DrawCreature dye the sprite.
        /// </summary>
        public Boolean HasColorTemplate
        {
            get { return ColorTemplate && BlendFrames > 1; }
        }

        /// <summary>
        /// Where the furniture over a creature hangs, relative to the corner of
        /// its tile and in map pixels: the top of the sprite, in the middle. Its
        /// name, its health bar, what it is saying and the damage rolling off it
        /// all hang from this one point.
        ///
        /// The half sprite of air that used to be left above the head was the
        /// original code's, and it read as the bar belonging to whatever was on
        /// the tile behind.
        /// </summary>
        public Vector2 Overhead
        {
            get
            {
                return new Vector2(
                    Width * 16 + RenderOffset,
                    -Height * 16 + RenderOffset + 16);
            }
        }

        /// <summary>
        /// The same point over a tile with nothing standing on it.
        /// </summary>
        public static readonly Vector2 DefaultOverhead = new Vector2(16, 0);

        public static Vector2 OverheadOf(GameSprite Sprite)
        {
            return Sprite == null ? DefaultOverhead : Sprite.Overhead;
        }

        /// <summary>
        /// How many poses this creature has. Phase 0 is standing still and the
        /// rest are the walk cycle; see CreatureWalks.
        /// </summary>
        public int Phases
        {
            get { return Math.Max(1, AnimationLength); }
        }

        /// <summary>
        /// A creature image. Direction picks the column of the sheet, Layer picks
        /// 0 for the sprite itself and 1 for its colour mask, Phase the pose.
        ///
        /// Diagonals are folded onto the four columns a creature sheet actually
        /// has, which the modulo below used to do by accident - now that a phase
        /// multiplies the column, a fifth direction would land in the next pose.
        /// </summary>
        public GameImage GetImage(int X, int Y, Direction Direction, int Layer, int Phase)
        {
            int Column = ((int)Direction) % this.XDiv;
            int v = (((Phase * this.XDiv + Column) * this.BlendFrames + Layer) * this.Height + Y) * this.Width + X;
            if(v >= this.Images.Count)
            {
                if(this.Images.Count == 1)
                    v = 0;
                else
                    v %= this.Images.Count;
            }
            return this.Images[v];
        }
    }
}
