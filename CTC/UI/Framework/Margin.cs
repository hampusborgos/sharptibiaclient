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
    };

    public static class RectangleExtensions
    {
        public static Rectangle Subtract(this Rectangle self, Margin margin)
        {
            return new Rectangle
            {
                X = self.Left + margin.Left,
                Y = self.Top + margin.Top,
                Width = self.Width - margin.TotalWidth,
                Height = self.Height - margin.TotalHeight
            };
        }

        public static bool Overlaps(this Rectangle self, Rectangle other)
        {
            return self.Left <= other.Right && self.Right >= other.Left &&
                self.Top <= other.Bottom && self.Bottom >= other.Top;
        }
    }
}
