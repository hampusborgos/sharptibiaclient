using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace CTC
{
    public class AnimatedText : GameEffect
    {
        public static double DefaultDuration = 1.0;

        public AnimatedText(String Text, int Color, Vector2 Overhead)
        {
            this.Text = Text;
            this.Color = Color;
            this.Overhead = Overhead;
            this.Duration = DefaultDuration;
        }
        
        public readonly int Color;
        public readonly String Text;

        /// <summary>
        /// Where over the tile it floats from, in map pixels: the point the
        /// creature that took the hit hangs its name and bar from, taken when
        /// the hit landed so the number keeps its place if the creature walks
        /// out from under it.
        /// </summary>
        public readonly Vector2 Overhead;

        /// <summary>
        /// How far it has floated since, in screen pixels.
        /// </summary>
        public Vector2 Offset = new Vector2(0, 0);

        public override void Update(GameTime Time)
        {
            base.Update(Time);

            Offset.Y -= (float)(Time.ElapsedGameTime.TotalSeconds * 16);
        }
    }
}
