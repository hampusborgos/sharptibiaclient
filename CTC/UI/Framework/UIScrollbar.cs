using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    public class UIScrollbar : UIView
    {
        private int _ScrollbarLength;
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
                if (_ScrollbarPosition < value)
                    _ScrollbarPosition = value;
                _ScrollbarLength = value;
            }
        }

        private int _ScrollbarPosition;
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
            }
        }

        protected UIButton TopButton;
        protected UIButton BottomButton;
        protected UIButton GemButton;

        /// <summary>
        /// Constructor of the scrollbar
        /// </summary>
        /// <param name="Parent"></param>
        public UIScrollbar(UIView Parent)
            : base(Parent)
        {
            // Set the background for this element
            ElementType = UIElementType.ScrollbarBackground;

            // Create the 3 buttons
            TopButton = new UIButton(this);
            TopButton.NormalType = UIElementType.ScrollbarTop;
            TopButton.HighlightType = UIElementType.ScrollbarTopHighlight;
            AddSubview(TopButton);

            BottomButton = new UIButton(this);
            BottomButton.NormalType = UIElementType.ScrollbarBottom;
            BottomButton.HighlightType = UIElementType.ScrollbarBottomHighlight;
            AddSubview(BottomButton);

            GemButton = new UIButton(this);
            GemButton.NormalType = UIElementType.ScrollbarGem;
            GemButton.HighlightType = UIElementType.ScrollbarGem;
            AddSubview(GemButton);

            // Set some defaults for the scrollbar
            ScrollbarLength = 100;
            ScrollbarPosition = 0;

            // Figure out how wide we are (dependant on the STUFF!)
            CalculateWidth();
        }

        private void CalculateWidth()
        {
            int width = 0;
            width += (int)Context.Skin.Measure(UIElementType.ScrollbarBackground, UISkinOrientation.Left).X;
            width += (int)Context.Skin.Measure(UIElementType.ScrollbarBackground, UISkinOrientation.Center).X;
            width += (int)Context.Skin.Measure(UIElementType.ScrollbarBackground, UISkinOrientation.Right).X;
            Bounds.Width = width;
        }

        public override void LayoutSubviews()
        {
            CalculateWidth();

            base.LayoutSubviews();
        }

        protected override void DrawContent(SpriteBatch CurrentBatch)
        {
            // Draw top button
            Rectangle top = new Rectangle();
            top.Width = (int)Context.Skin.Measure(UIElementType.ScrollbarTop, UISkinOrientation.Center).X;
            top.Height = (int)Context.Skin.Measure(UIElementType.ScrollbarTop, UISkinOrientation.Center).Y;
            TopButton.Bounds = top;

            // Draw bottom button
            Rectangle bottom = new Rectangle();
            bottom.Width = (int)Context.Skin.Measure(UIElementType.ScrollbarBottom, UISkinOrientation.Center).X;
            bottom.Height = (int)Context.Skin.Measure(UIElementType.ScrollbarBottom, UISkinOrientation.Center).Y;
            bottom.Y += Bounds.Height;
            bottom.Y -= bottom.Height;
            BottomButton.Bounds = bottom;

            // Draw the gem
            Rectangle gem = new Rectangle();
            gem.Width = (int)Context.Skin.Measure(UIElementType.ScrollbarGem, UISkinOrientation.Center).X;
            gem.Height = (int)Context.Skin.Measure(UIElementType.ScrollbarGem, UISkinOrientation.Center).Y;
            gem.Y += top.Height;

            int h = Bounds.Height - bottom.Height - top.Height - gem.Height;
            float pos = (float)ScrollbarPosition / (ScrollbarLength > 0 ? ScrollbarLength : 1);
            gem.Y += (int)(h * pos);

            GemButton.Bounds = gem;
        }
    }
}
