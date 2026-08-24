using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CTC
{
    /// <summary>
    /// A horizontal position bar with a draggable thumb, the way a video player
    /// has one.
    ///
    /// Structurally this is UIScrollbar with the axes swapped, and it inherits the
    /// one thing that makes that work: the thumb is a UIButton, and UIButton
    /// captures the mouse when it is pressed. Move events only ever reach the view
    /// holding the capture, so a thumb that did not do that would never see a drag.
    ///
    /// It also adds what UIScrollbar never had, a click anywhere on the track.
    /// </summary>
    class UIScrubBar : UIView
    {
        /// <summary>
        /// Where the thumb sits, from 0 at the left to 1 at the right.
        /// </summary>
        public double Position
        {
            get { return _Position; }
            set
            {
                _Position = Clamp(value);
                PositionGem();
            }
        }
        private double _Position = 0;

        /// <summary>
        /// True while the user has hold of the thumb. The owner uses this to stop
        /// writing a position the user is in the middle of choosing.
        /// </summary>
        public Boolean Dragging
        {
            get { return _Dragging; }
        }
        private Boolean _Dragging = false;

        public UIButton GemButton;

        public delegate void ScrubEvent(UIScrubBar Scrub);

        /// <summary>
        /// The user moved the thumb, or clicked the track. Not raised when the
        /// position is set programmatically, so that following playback does not
        /// feed back into a seek.
        /// </summary>
        public event ScrubEvent ScrubMoved;

        /// <summary>
        /// The user let go.
        /// </summary>
        public event ScrubEvent ScrubReleased;

        /// <summary>
        /// How much of the bar is played, drawn filled behind the thumb. Usually
        /// the same as Position, but while dragging it lags behind at whatever the
        /// client has actually managed to reach.
        /// </summary>
        public double Played = 0;

        private const int TrackHeight = 4;

        public UIScrubBar()
            : base(null, UIElementType.None)
        {
            Autoresizable = false;
            Bounds = new Rectangle(0, 0, 100, 12);

            GemButton = new UIButton();
            GemButton.NormalType = UIElementType.ScrollbarGem;
            GemButton.HighlightType = UIElementType.ScrollbarGem;
            GemButton.Autoresizable = false;
            GemButton.ButtonPressed += delegate(UIButton Button, MouseState mouse)
            {
                _Dragging = true;
            };
            GemButton.ButtonDragged += delegate(UIButton Button, MouseState mouse)
            {
                MoveTo(mouse.X);
            };
            GemButton.ButtonReleased += delegate(UIButton Button, MouseState mouse)
            {
                _Dragging = false;
                if (ScrubReleased != null)
                    ScrubReleased(this);
            };
            AddSubview(GemButton);
        }

        /// <summary>
        /// A click on the track jumps there. The thumb gets first refusal, since
        /// it is a child and base walks the children first.
        /// </summary>
        public override bool MouseLeftClick(MouseState mouse)
        {
            if (base.MouseLeftClick(mouse))
                return true;

            if (mouse.LeftButton != ButtonState.Pressed)
                return false;

            MoveTo(mouse.X);
            if (ScrubReleased != null)
                ScrubReleased(this);
            return true;
        }

        private void MoveTo(int ScreenX)
        {
            int Travel = TravelLength;
            if (Travel <= 0)
                return;

            int Along = ScreenX - ScreenClientBounds.Left - GemButton.Bounds.Width / 2;
            _Position = Clamp((double)Along / Travel);
            PositionGem();

            if (ScrubMoved != null)
                ScrubMoved(this);
        }

        /// <summary>
        /// How far the thumb's left edge can travel.
        /// </summary>
        private int TravelLength
        {
            get { return Math.Max(0, ClientBounds.Width - GemButton.Bounds.Width); }
        }

        private void PositionGem()
        {
            GemButton.Bounds.X = ClientBounds.Left + (int)(TravelLength * _Position);
            GemButton.Bounds.Y = ClientBounds.Top
                + (ClientBounds.Height - GemButton.Bounds.Height) / 2;
        }

        private static double Clamp(double Value)
        {
            if (Value < 0)
                return 0;
            if (Value > 1)
                return 1;
            return Value;
        }

        public override void LayoutSubviews()
        {
            Vector2 Gem = UIContext.Skin.Measure(UIElementType.ScrollbarGem, UISkinOrientation.Center);
            GemButton.Bounds.Width = (int)Gem.X;
            GemButton.Bounds.Height = (int)Gem.Y;

            PositionGem();

            base.LayoutSubviews();
        }

        /// <summary>
        /// The track is drawn flat rather than skinned. The only track element in
        /// the atlas is the vertical scrollbar's, and stretching a nine-slice built
        /// for one axis along the other looks like exactly what it is.
        /// </summary>
        protected override void DrawContent(SpriteBatch CurrentBatch)
        {
            int Y = ClientBounds.Top + (ClientBounds.Height - TrackHeight) / 2;

            UIContext.Skin.DrawRectangle(CurrentBatch,
                ScreenCoordinate(ClientBounds.Left, Y, ClientBounds.Width, TrackHeight),
                new Color(24, 24, 24));

            int Filled = (int)(ClientBounds.Width * Clamp(Played));
            if (Filled > 0)
                UIContext.Skin.DrawRectangle(CurrentBatch,
                    ScreenCoordinate(ClientBounds.Left, Y, Filled, TrackHeight),
                    new Color(140, 132, 112));
        }
    }
}
