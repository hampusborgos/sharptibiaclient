using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
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
        
        public Color TextColor;

        public UILabel(String Text)
            : base (null, UIElementType.None)
        {
            _Text = Text;
            TextColor = Color.LightGray;
        }

        protected override void DrawContent(SpriteBatch CurrentBatch)
        {
            CurrentBatch.DrawString(
                Context.StandardFont,
                _Text,
                ScreenCoordinate(Bounds.X, Bounds.Y),
                TextColor
            );
        }
    }
}
