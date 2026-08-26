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
        FastForward,
        Rewind,
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

        /// <summary>
        /// Whether this is the direction currently running. It draws as though
        /// held down, which is what it amounts to: the button whose state the
        /// client is in.
        /// </summary>
        public Boolean Active
        {
            get { return _Active; }
            set
            {
                _Active = value;
                ShowState();
            }
        }
        private Boolean _Active = false;

        /// <summary>
        /// A press wins over being active, so a button already lit still reacts
        /// to being clicked.
        /// </summary>
        public override bool Highlighted
        {
            get { return base.Highlighted; }
            set
            {
                base.Highlighted = value;
                ShowState();
            }
        }

        private void ShowState()
        {
            ElementType = base.Highlighted || _Active ? HighlightType : NormalType;
        }

        public PlaybackButton(PlaybackGlyph Glyph)
            : base(null)
        {
            this.Glyph = Glyph;
            Autoresizable = false;
            Bounds = new Rectangle(0, 0, 24, 18);
        }

        protected override void DrawContent(SpriteBatch CurrentBatch)
        {
            Color Ink = Highlighted || Active ? Color.White : Color.LightGray;

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

                case PlaybackGlyph.FastForward:
                case PlaybackGlyph.Rewind:
                    // Two arrows, each half as wide and just as tall, with a
                    // pixel of daylight between them.
                    int Facing = Glyph == PlaybackGlyph.FastForward ? 1 : -1;
                    int Wing = (Size - 1) / 2;
                    DrawTriangle(CurrentBatch, Left, Top, Wing, Size, Facing, Ink);
                    DrawTriangle(CurrentBatch, Left + Size - Wing, Top, Wing, Size, Facing, Ink);
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
            DrawTriangle(CurrentBatch, Left, Top, Size, Size, Facing, Ink);
        }

        /// <summary>
        /// The same, for a triangle that is not as wide as it is tall - which is
        /// what a pair of them needs to be to fit in the same box as one.
        /// </summary>
        private void DrawTriangle(SpriteBatch CurrentBatch, int Left, int Top, int Wide, int Tall, int Facing, Color Ink)
        {
            int Half = Math.Max(1, Tall / 2);
            for (int Row = 0; Row < Tall; ++Row)
            {
                // Full width across the middle, tapering to a point at both ends.
                int Width = Math.Max(1, Wide - Math.Abs(Row - Half) * Wide / Half);
                int X = Facing > 0 ? Left : Left + Wide - Width;

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
