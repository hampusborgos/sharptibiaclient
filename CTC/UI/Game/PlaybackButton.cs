using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    public enum PlaybackGlyph
    {
        Play,
        Pause,
        Reverse,
        Start,
        End
    };

    /// <summary>
    /// A transport button with a drawn glyph.
    ///
    /// The obvious thing would be to set Label to one of the media characters,
    /// but StandardFont.spritefont covers ASCII 32 to 126 and declares no
    /// DefaultCharacter, so asking SpriteFont to draw a triangle throws. These are
    /// painted out of the skin's one white pixel instead - a triangle being a
    /// stack of rows - which is the same trick ItemButton uses to draw a sprite
    /// where a label would go.
    /// </summary>
    public class PlaybackButton : UIButton
    {
        public PlaybackGlyph Glyph;

        public PlaybackButton(PlaybackGlyph Glyph)
            : base(null)
        {
            this.Glyph = Glyph;
            Autoresizable = false;
            Bounds = new Rectangle(0, 0, 24, 18);
        }

        protected override void DrawContent(SpriteBatch CurrentBatch)
        {
            Color Ink = Highlighted ? Color.White : Color.LightGray;

            // A square box in the middle of the button, so every glyph is the
            // same size whatever the button is.
            int Size = Math.Min(ClientBounds.Width, ClientBounds.Height) - 6;
            if (Size < 4)
                return;

            int Left = ClientBounds.Left + (ClientBounds.Width - Size) / 2;
            int Top = ClientBounds.Top + (ClientBounds.Height - Size) / 2;

            switch (Glyph)
            {
                case PlaybackGlyph.Play:
                    DrawTriangle(CurrentBatch, Left, Top, Size, 1, Ink);
                    break;

                case PlaybackGlyph.Reverse:
                    DrawTriangle(CurrentBatch, Left, Top, Size, -1, Ink);
                    break;

                case PlaybackGlyph.Pause:
                    int Bar = Math.Max(2, Size / 3);
                    Fill(CurrentBatch, Left, Top, Bar, Size, Ink);
                    Fill(CurrentBatch, Left + Size - Bar, Top, Bar, Size, Ink);
                    break;

                case PlaybackGlyph.Start:
                    Fill(CurrentBatch, Left, Top, 2, Size, Ink);
                    DrawTriangle(CurrentBatch, Left + 3, Top, Size - 3, -1, Ink);
                    break;

                case PlaybackGlyph.End:
                    DrawTriangle(CurrentBatch, Left, Top, Size - 3, 1, Ink);
                    Fill(CurrentBatch, Left + Size - 2, Top, 2, Size, Ink);
                    break;
            }
        }

        /// <summary>
        /// A filled triangle, pointing right when Facing is 1 and left when it is
        /// -1, built a row at a time.
        /// </summary>
        private void DrawTriangle(SpriteBatch CurrentBatch, int Left, int Top, int Size, int Facing, Color Ink)
        {
            int Half = Size / 2;
            for (int Row = 0; Row < Size; ++Row)
            {
                // Full width across the middle, tapering to a point at both ends.
                int Width = Math.Max(1, Size - Math.Abs(Row - Half) * 2);
                int X = Facing > 0 ? Left : Left + Size - Width;

                Fill(CurrentBatch, X, Top + Row, Width, 1, Ink);
            }
        }

        private void Fill(SpriteBatch CurrentBatch, int X, int Y, int Width, int Height, Color Ink)
        {
            UIContext.Skin.DrawRectangle(
                CurrentBatch, ScreenCoordinate(X, Y, Width, Height), Ink);
        }
    }
}
