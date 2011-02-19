using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    public class UIButton : UIPanel
    {
        public String Label;

        public UIButton(UIPanel Parent)
            : base(Parent)
        {
            ElementType = UIElementType.Button;
            Bounds = new Rectangle(0, 0, 32, 32);
        }

        protected override void DrawContent(SpriteBatch CurrentBatch)
        {
            if (Label != null)
            {
                Vector2 Size = Context.StandardFont.MeasureString(Label);
                Vector2 Offset = new Vector2(
                    (int)(ScreenBounds.Right  - (ClientBounds.Width  + Size.X) / 2),
                    (int)(ScreenBounds.Bottom - (ClientBounds.Height + Size.Y) / 2)
                );

                CurrentBatch.DrawString(
                    Context.StandardFont, Label, Offset,
                    Color.LightGray,
                    0.0f, new Vector2(0.0f, 0.0f),
                    1.0f, SpriteEffects.None, 0.5f
                );
            }
        }
    }
}
