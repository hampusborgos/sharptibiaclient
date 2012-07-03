using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CTC
{
    public class UIScrollbar : UIView
    {
        /// <summary>
        /// Length of the scrollbar
        /// </summary>
        public int ScrollbarLength
        {
            get
            {
                return _ScrollbarLength;
            }
            set
            {
                _ScrollbarLength = value;
                if (ScrollbarPosition < value)
                    ScrollbarPosition = value;
                Visible = value != 0;
            }
        }
        private int _ScrollbarLength;

        /// <summary>
        /// Position of the scrollbar
        /// </summary>
        public int ScrollbarPosition
        {
            get
            {
                return _ScrollbarPosition;
            }
            set
            {
                if (value > ScrollbarLength)
                    throw new ArgumentException("Scrollbar Position cannot be greater than Length.");
                else if (value < 0)
                    throw new ArgumentException("Scrollbar Position cannot be less than 0.");
                else
                    _ScrollbarPosition = value;

                PositionGem();
            }
        }
        private int _ScrollbarPosition;

        protected UIButton TopButton;
        protected UIButton BottomButton;
        protected UIButton GemButton;

        public delegate void ScrollbarMoved(UIScrollbar Scrollbar);

        /// <summary>
        /// Constructor of the scrollbar
        /// </summary>
        public UIScrollbar()
        {
            // Set the background for this element
            ElementType = UIElementType.ScrollbarBackground;

            // Create the 3 buttons
            TopButton = new UIButton();
            TopButton.NormalType = UIElementType.ScrollbarTop;
            TopButton.HighlightType = UIElementType.ScrollbarTopHighlight;
            AddSubview(TopButton);

            BottomButton = new UIButton();
            BottomButton.NormalType = UIElementType.ScrollbarBottom;
            BottomButton.HighlightType = UIElementType.ScrollbarBottomHighlight;
            AddSubview(BottomButton);

            GemButton = new UIButton();
            GemButton.NormalType = UIElementType.ScrollbarGem;
            GemButton.HighlightType = UIElementType.ScrollbarGem;
            GemButton.ButtonDragged += delegate(UIButton Button, MouseState mouse)
            {
                int S = mouse.Y - ScreenBounds.Y - TopButton.Bounds.Height;
                int ScrollAreaLength = Bounds.Height - TopButton.Bounds.Height - BottomButton.Bounds.Height;
                S = (int)(ScrollbarLength * ((double)S / ScrollAreaLength));
                ScrollbarPosition = Math.Min(ScrollbarLength, Math.Max(0, S));
            };
            AddSubview(GemButton);

            // Set some defaults for the scrollbar
            ScrollbarLength = 100;
            ScrollbarPosition = 0;
        }

        private void CalculateWidth()
        {
            int width = 0;
            width += (int)Context.Skin.Measure(UIElementType.ScrollbarBackground, UISkinOrientation.Left).X;
            width += (int)Context.Skin.Measure(UIElementType.ScrollbarBackground, UISkinOrientation.Center).X;
            width += (int)Context.Skin.Measure(UIElementType.ScrollbarBackground, UISkinOrientation.Right).X;
            Bounds.Width = width;
        }

        private void PositionGem()
        {
            int ScrollAreaLength = Bounds.Height - TopButton.Bounds.Height - BottomButton.Bounds.Height - GemButton.Bounds.Height;
            int S = (int)(ScrollAreaLength * ((double)ScrollbarPosition / (ScrollbarLength == 0 ? 1 : ScrollbarLength)));
            GemButton.Bounds.Y = TopButton.Bounds.Height + S;
        }

        public override void LayoutSubviews()
        {
            CalculateWidth();

            // Position the Top button
            Rectangle top = new Rectangle();
            top.Width = (int)Context.Skin.Measure(UIElementType.ScrollbarTop, UISkinOrientation.Center).X;
            top.Height = (int)Context.Skin.Measure(UIElementType.ScrollbarTop, UISkinOrientation.Center).Y;
            TopButton.Bounds = top;

            // Position the bottom
            Rectangle bottom = new Rectangle();
            bottom.Width = (int)Context.Skin.Measure(UIElementType.ScrollbarBottom, UISkinOrientation.Center).X;
            bottom.Height = (int)Context.Skin.Measure(UIElementType.ScrollbarBottom, UISkinOrientation.Center).Y;
            bottom.Y += Bounds.Height;
            bottom.Y -= bottom.Height;
            BottomButton.Bounds = bottom;

            // Position the gem
            Rectangle gem = new Rectangle();
            gem.Width = (int)Context.Skin.Measure(UIElementType.ScrollbarGem, UISkinOrientation.Center).X;
            gem.Height = (int)Context.Skin.Measure(UIElementType.ScrollbarGem, UISkinOrientation.Center).Y;
            GemButton.Bounds = gem;

            PositionGem();

            base.LayoutSubviews();
        }
    }
}
