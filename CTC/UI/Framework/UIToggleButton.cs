using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CTC
{
    class UIToggleButton : UIView
    {
        /// <summary>
        /// The button type that will be used when the button is in the 'Off' state.
        /// </summary>
        public UIElementType OffType
        {
            get { return _OffType; }
            set
            {
                if (!On)
                    ElementType = value;
                _OffType = value;
            }
        }
        private UIElementType _OffType;

        /// <summary>
        /// The Element type that will be used when the button is in the 'On' state.
        /// </summary>
        public UIElementType OnType
        {
            get { return _OnType; }
            set
            {
                if (On)
                    ElementType = value;
                _OnType = value;
            }
        }
        private UIElementType _OnType;

        /// <summary>
        /// Is the button on?
        /// </summary>
        public virtual bool On
        {
            get
            {
                return _On;
            }
            set
            {
                if (value)
                    ElementType = OnType;
                else
                    ElementType = OffType;
                _On = value;
            }
        }
        private bool _On = false;

        public Boolean Highlighted = false;

        /// <summary>
        /// The name of the button, will be displayed centered on it.
        /// </summary>
        // TODO: Replace this with an UILabel
        public String Label;

        public delegate void ButtonToggledEvent(UIToggleButton Button, MouseState mouse);

        public event ButtonToggledEvent ButtonToggled;

        public UIToggleButton(String Label = "")
        {
            this.Label = Label;
            OnType = UIElementType.ButtonHighlight;
            OffType = UIElementType.Button;

            Bounds = new Rectangle(0, 0, 32, 32);
        }

        public override bool MouseLeftClick(MouseState mouse)
        {
            if (mouse.LeftButton == ButtonState.Pressed)
            {
                if (CaptureMouse())
                {
                    Highlighted = true;
                }
            }
            else if (mouse.LeftButton == ButtonState.Released && Highlighted)
            {
                ReleaseMouse();
                
                // Fire some events
                if (ScreenBounds.Contains(new Point(mouse.X, mouse.Y)))
                {
                    On = !On;
                    if (ButtonToggled != null)
                        ButtonToggled(this, mouse);
                }
            }
            return true;
        }

        public override bool MouseLost()
        {
            Highlighted = false;
            return true;
        }

        protected override void DrawContent(SpriteBatch CurrentBatch)
        {
            if (Label != null)
            {
                Vector2 Size = UIContext.StandardFont.MeasureString(Label);
                Vector2 Offset = new Vector2(
                    (int)((ClientBounds.Width - Size.X) / 2),
                    (int)((ClientBounds.Height - Size.Y) / 2)
                );

                CurrentBatch.DrawString(
                    UIContext.StandardFont, Label, ScreenCoordinate(Offset),
                    Color.LightGray,
                    0.0f, new Vector2(0.0f, 0.0f),
                    1.0f, SpriteEffects.None, 0.5f
                );
            }
        }
    }
}
