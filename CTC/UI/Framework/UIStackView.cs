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
            : base(null, UIElementType.BorderlessWindow)
        {
            _StackDirection = Direction;
            Autoresizable = false;
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

        public Boolean StretchOtherDirection = false;

        /// <summary>
        /// Should the stack view fill up in the other direction once the primary
        /// direction is full?
        /// </summary>
        public Boolean Overflow
        {
            get { return _Overflow; }
            set
            {
                _Overflow = value;
            }
        }
        private Boolean _Overflow;

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            if (StackDirection == UIStackDirection.Vertical)
                LayoutVertical();
            else
                LayoutHorizontal();

            // Layout subviews again if we changed size
            if (!Autoresizable)
                base.LayoutSubviews();
        }

        private void LayoutHorizontal()
        {
            int SpaceLeft = FullBounds.Width; ;
            int RowLeft = 0;
            int RowTop = SkinPadding.Top + Padding.Top;
            int HighestThisRow = 0;
            int Row = 0;

            foreach (UIView Subview in Children)
            {
                if (!Subview.Autoresizable)
                    return;

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

            if (Row == 0 && StretchOtherDirection)
            {
                HighestThisRow = Math.Max(ClientBounds.Height, HighestThisRow);
                foreach (UIView Subview in Subviews)
                    Subview.Bounds.Height = HighestThisRow;
            }

            if (!Autoresizable)
            {
                Bounds.Width = RowLeft;
                Bounds.Height = RowTop + HighestThisRow;
            }
        }

        private void LayoutVertical()
        {
            int SpaceLeft = FullBounds.Height;
            int ColumnTop = SkinPadding.Top + Padding.Top;
            int ColumnLeft = SkinPadding.Left + Padding.Left;
            int WidestThisColumn = 0;
            int Column = 0;

            foreach (UIView Subview in Children)
            {
                if (!Subview.Autoresizable)
                    return;

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

            if (Column == 0 && StretchOtherDirection)
            {
                WidestThisColumn = Math.Max(ClientBounds.Width, WidestThisColumn);
                foreach (UIView Subview in Subviews)
                    Subview.Bounds.Width = WidestThisColumn;
            }

            if (!Autoresizable)
            {
                Bounds.Width = ColumnLeft + WidestThisColumn;
                Bounds.Height = ColumnTop;
            }
        }
    }
}
