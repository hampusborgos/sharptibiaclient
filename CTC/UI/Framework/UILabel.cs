using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    public enum UITextAlignment
    {
        Left,
        Center,
        Right
    };

    class UILabel : UIView
    {
        /// <summary>
        /// The text displayed inside this label
        /// </summary>
        public String Text
        {
            get { return _Text; }
            set
            {
                _Text = value;
                NeedsLayout = true;
            }
        }
        private String _Text;

        public UITextAlignment TextAlignment = UITextAlignment.Left;
        public Color TextColor = Color.LightGray;

        public UILabel(String Text = "")
            : base (new Rectangle(0, 0, 100, 18) , UIElementType.None)
        {
            _Text = Text;
        }

        protected override void DrawContent(SpriteBatch CurrentBatch)
        {
            if (Text == "")
                return;

            int RL = 0;
            if (TextAlignment == UITextAlignment.Center)
                RL = (int)(Bounds.Width - UIContext.StandardFont.MeasureString(Text).X) / 2;
            else if (TextAlignment == UITextAlignment.Right)
                RL = (int)(Bounds.Width - UIContext.StandardFont.MeasureString(Text).X);

            CurrentBatch.DrawString(
                UIContext.StandardFont,
                _Text,
                ScreenCoordinate(RL, 0),
                TextColor
            );
        }
    }
}
