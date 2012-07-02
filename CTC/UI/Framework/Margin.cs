using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    public struct Margin
    {
        public Margin(int AllMargins)
        {
            Top = AllMargins;
            Right = AllMargins;
            Bottom = AllMargins;
            Left = AllMargins;
        }

        public Margin(int VerticalMargin, int HorizontalMargin)
        {
            Top = VerticalMargin;
            Right = HorizontalMargin;
            Bottom = VerticalMargin;
            Left = HorizontalMargin;
        }

        public int Top;
        public int Right;
        public int Bottom;
        public int Left;

        public int TotalWidth
        {
            get { return Left + Right; }
        }
        public int TotalHeight
        {
            get { return Top + Bottom; }
        }
    };
}
