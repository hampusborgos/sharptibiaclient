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

        public AnimatedText(String Text, int Color)
        {
            this.Text = Text;
            this.Color = Color;
            this.Duration = DefaultDuration;

            Offset.X = 16;
            Offset.Y = -16;
        }
        
        public readonly int Color;
        public readonly String Text;
        public Vector2 Offset = new Vector2(0, 0);

        public override void Update(GameTime Time)
        {
            base.Update(Time);

            Offset.Y -= (float)(Time.ElapsedGameTime.TotalSeconds * 16);
        }
    }
}
