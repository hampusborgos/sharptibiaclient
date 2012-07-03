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

        public override Rectangle  ClientBounds
        {
	        get 
	        {
                Rectangle b = base.ClientBounds;
                b.Width -= Scrollbar.Bounds.Width;
		        return b;
	        }
        }
        
        public UIVirtualFrame(UIView Parent)
            : base(Parent)
        {
            Scrollbar = new UIScrollbar(this);
            AddSubview(Scrollbar);
        }

        public override void LayoutSubviews()
        {
            Margin sp = SkinPadding;
            Rectangle sc = sp.SubtractFrom(Bounds);
            Scrollbar.Bounds = new Rectangle
            {
                X = Bounds.Width - sp.Right - Scrollbar.Bounds.Width,
                Y = sp.Top,
                Width = Scrollbar.Bounds.Width,
                Height = Bounds.Height - sp.TotalHeight
            };

            base.LayoutSubviews();
        }
    }
}
