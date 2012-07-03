using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    enum UIStackDirection
    {
        Horizontal,
        Vertical
    };

    class UIStackView : UIView
    {
        public UIStackView(UIStackDirection Direction = UIStackDirection.Vertical, Boolean Overflow = false)
            : base(null, UIElementType.None)
        {
            _StackDirection = Direction;
            this.Overflow = Overflow;
        }

        /// <summary>
        /// Which direction should elements flow?
        /// You can use Overflow to get a grid.
        /// </summary>
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
        private UIStackDirection _StackDirection;

        /// <summary>
        /// Should the stack view fill up in the other direction once the primary
        /// direction is full?
        /// </summary>
        public Boolean Overflow = false;

        public override void LayoutSubviews()
        {
            NeedsLayout = false;

            if (StackDirection == UIStackDirection.Vertical)
                LayoutVertical();
            else
                LayoutHorizontal();
        }

        private void LayoutHorizontal()
        {
            int SpaceLeft = 0;
            int RowLeft = 0;
            int RowTop = SkinPadding.Top + Padding.Top;
            int HighestThisRow = 0;
            int Row = -1; // start on -1 since first iteration will increase it

            foreach (UIView Subview in Children)
            {
                // Start on the next row?
                if (SpaceLeft - Subview.Bounds.Width <= 0 && Overflow)
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

                SpaceLeft -= Subview.FullBounds.Width;
            }
        }

        private void LayoutVertical()
        {
            int SpaceLeft = FullBounds.Height;
            int ColumnTop = SkinPadding.Top + Padding.Top;
            int ColumnLeft = SkinPadding.Left + Padding.Left;
            int WidestThisColumn = 0;
            int Column = -1; // start on -1 since first iteration will increase it

            foreach (UIView Subview in Children)
            {
                // Start on the next row?
                if (SpaceLeft - Subview.Bounds.Height <= 0 && Overflow)
                {
                    SpaceLeft = FullBounds.Height;
                    ColumnTop = SkinPadding.Top + Padding.Top;
                    ColumnLeft += WidestThisColumn;
                    ++Column;
                }

                WidestThisColumn = Math.Max(WidestThisColumn, Subview.FullBounds.Width);

                Subview.Bounds.X = ColumnLeft;
                Subview.Bounds.Y = ColumnTop;

                ColumnTop += Subview.FullBounds.Height;

                SpaceLeft -= Subview.FullBounds.Height;
            }
        }
    }
}
