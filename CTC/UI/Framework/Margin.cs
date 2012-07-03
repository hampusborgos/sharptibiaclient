using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

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

        public Rectangle SubtractFrom(Rectangle rect)
        {
            return new Rectangle
            {
                X = rect.Left + Left,
                Y = rect.Top + Top,
                Width = rect.Width - Right,
                Height = rect.Height - Bottom
            };
        }
    };
}
