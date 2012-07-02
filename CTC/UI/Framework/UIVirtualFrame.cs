using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    public class UIVirtualFrame : UIFrame
    {
        public UIScrollbar Scrollbar;

        public override Margin SkinPadding
        {
            get
            {
                int left = (int)Context.Skin.Measure(ElementType, UISkinOrientation.Left).X;
                int top = (int)Context.Skin.Measure(ElementType, UISkinOrientation.Top).Y;
                int right = (int)Context.Skin.Measure(ElementType, UISkinOrientation.Right).X;
                int bottom = (int)Context.Skin.Measure(ElementType, UISkinOrientation.Bottom).Y;

                int width = 0;
                width += (int)Context.Skin.Measure(UIElementType.ScrollbarBackground, UISkinOrientation.Left).X;
                width += (int)Context.Skin.Measure(UIElementType.ScrollbarBackground, UISkinOrientation.Center).X;
                width += (int)Context.Skin.Measure(UIElementType.ScrollbarBackground, UISkinOrientation.Right).X;

                return new Margin(
                    left,
                    top,
                    right + width,
                    bottom
                );
            }
        }

        /// <summary>
        /// The internal bounds of the scrollable panel
        /// </summary>
        private Vector2 VirtualBounds;
        
        public UIVirtualFrame(UIView Parent)
            : base(Parent)
        {
            Scrollbar = new UIScrollbar(this);
            Scrollbar.Bounds = new Rectangle(Bounds.X, Bounds.Y, 12, 100);
            AddSubview(Scrollbar);
        }

        public override void Update(GameTime time)
        {
            base.Update(time);
            RecalculateScrollbars();
        }

        /// <summary>
        /// Recalulates the bounds of the scrollbar
        /// </summary>
        private void RecalculateScrollbars()
        {
            int width = 0;
            width += (int)Context.Skin.Measure(UIElementType.ScrollbarBackground, UISkinOrientation.Left).X;
            width += (int)Context.Skin.Measure(UIElementType.ScrollbarBackground, UISkinOrientation.Center).X;
            width += (int)Context.Skin.Measure(UIElementType.ScrollbarBackground, UISkinOrientation.Right).X;

            int top = 0;
            top += (int)Context.Skin.Measure(UIElementType.Frame, UISkinOrientation.Top).Y;

            Scrollbar.Bounds.X = Bounds.Width - width - (int)Context.Skin.Measure(UIElementType.Frame, UISkinOrientation.Right).X;
            Scrollbar.Bounds.Y = top;

            Scrollbar.Bounds.Height = ClientBounds.Height;
            Scrollbar.Bounds.Width = width;
        }

        /*
        public override void OnResize(object o, EventArgs args)
        {
        }
        */
    }
}
