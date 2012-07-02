using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    enum UIStackDirection
    {
        Horizontal,
        Vertical,
        HorizontalThenVertical,
        VerticalThenHorizontal
    };

    class UIStackView : UIView
    {
        public UIStackView(UIView Parent, UIStackDirection Direction)
            : base(Parent, UIElementType.None)
        {
            _StackDirection = Direction;
        }

        private UIStackDirection _StackDirection;
        public UIStackDirection StackDirection
        {
            get
            {
                return _StackDirection;
            }
            set
            {
                _StackDirection = value;
                LayoutSubviews();
            }
        }

        public override void LayoutSubviews()
        {
            int SpaceLeft = 0;
            int RowLeft = 0;
            int RowTop = SkinPadding.Top + Padding.Top;
            int HighestThisRow = 0;
            int Row = -1; // start on -1 since first iteration will increase it

            foreach (UIView Subview in Children)
            {
                // Start on the next row?
                if (SpaceLeft - Subview.Bounds.Width <= 0)
                {
                    SpaceLeft = FullBounds.Width;
                    RowLeft = SkinPadding.Left + Padding.Left;
                    RowTop += HighestThisRow;
                    ++Row;
                }

                HighestThisRow = Math.Max(HighestThisRow, Subview.FullBounds.Height);
                
                Subview.Bounds.X = RowLeft;
                Subview.Bounds.Y = RowTop;

                RowLeft += Subview.FullBounds.Width;

                SpaceLeft -= Subview.Bounds.Width;
            }
        }
    }
}
