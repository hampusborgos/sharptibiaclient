using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    public struct Margin
    {
        public Margin(int Top, int Left, int Bottom, int Right)
        {
            this.Top = Top;
            this.Left = Left;
            this.Bottom = Bottom;
            this.Right = Right;
        }

        public int Top;
        public int Left;
        public int Bottom;
        public int Right;
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
