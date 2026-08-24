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
    /// The transport for a recording: a scrub bar across the top, and the
    /// controls centred underneath it with the timecode either side.
    ///
    /// Everything it drives lives on ClientState.Playback; this only decides when
    /// to ask for a seek. Dragging is rate limited to the same fifteen a second
    /// that backwards playback uses, because each one is a snapshot restore and a
    /// replay, and a drag across the whole bar at 120fps would ask for a hundred
    /// and twenty of them a second.
    /// </summary>
    public class PlaybackBar : UIView
    {
        private ClientState State;

        private UIScrubBar Scrub;
        private PlaybackButton ReverseButton;
        private PlaybackButton PlayButton;
        private UIButton SlowerButton;
        private UIButton FasterButton;
        private UILabel SpeedLabel;
        private UILabel ElapsedLabel;
        private UILabel TotalLabel;

        /// <summary>
        /// The speeds the -/+ buttons step through.
        /// </summary>
        private static readonly double[] Speeds = { 0.25, 0.5, 1, 2, 4, 8 };

        private static readonly TimeSpan ScrubInterval = TimeSpan.FromSeconds(1.0 / 15.0);
        private TimeSpan ScrubAccumulated = TimeSpan.Zero;

        /// <summary>
        /// How far a Left or Right press moves the playhead, and how far it moves
        /// with shift held.
        /// </summary>
        private static readonly TimeSpan Step = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan LongStep = TimeSpan.FromSeconds(60);

        public const int PreferredHeight = 44;

        public PlaybackBar()
            : base(null, UIElementType.Window)
        {
            Autoresizable = false;
            Bounds = new Rectangle(0, 0, 400, PreferredHeight);

            Scrub = new UIScrubBar();
            Scrub.ScrubMoved += OnScrubMoved;
            Scrub.ScrubReleased += OnScrubReleased;
            AddSubview(Scrub);

            ReverseButton = new PlaybackButton(PlaybackGlyph.Reverse);
            ReverseButton.ButtonReleasedInside += delegate(UIButton Button, MouseState mouse)
            {
                PlayBackwards();
            };
            AddSubview(ReverseButton);

            PlayButton = new PlaybackButton(PlaybackGlyph.Play);
            PlayButton.ButtonReleasedInside += delegate(UIButton Button, MouseState mouse)
            {
                TogglePlaying();
            };
            AddSubview(PlayButton);

            SlowerButton = new UIButton("-");
            SlowerButton.Autoresizable = false;
            SlowerButton.Bounds = new Rectangle(0, 0, 18, 18);
            SlowerButton.ButtonReleasedInside += delegate(UIButton Button, MouseState mouse)
            {
                StepSpeed(-1);
            };
            AddSubview(SlowerButton);

            SpeedLabel = new UILabel("1x");
            SpeedLabel.Autoresizable = false;
            SpeedLabel.TextAlignment = UITextAlignment.Center;
            SpeedLabel.Bounds = new Rectangle(0, 0, 44, 18);
            AddSubview(SpeedLabel);

            FasterButton = new UIButton("+");
            FasterButton.Autoresizable = false;
            FasterButton.Bounds = new Rectangle(0, 0, 18, 18);
            FasterButton.ButtonReleasedInside += delegate(UIButton Button, MouseState mouse)
            {
                StepSpeed(1);
            };
            AddSubview(FasterButton);

            ElapsedLabel = new UILabel("0:00");
            ElapsedLabel.Autoresizable = false;
            ElapsedLabel.Bounds = new Rectangle(0, 0, 70, 18);
            AddSubview(ElapsedLabel);

            TotalLabel = new UILabel("0:00");
            TotalLabel.Autoresizable = false;
            TotalLabel.TextAlignment = UITextAlignment.Right;
            TotalLabel.Bounds = new Rectangle(0, 0, 70, 18);
            AddSubview(TotalLabel);
        }

        /// <summary>
        /// Which client the transport drives. Hidden until there is one that is
        /// actually a recording; a live stream has no timeline to scrub.
        /// </summary>
        public void SetClient(ClientState NewState)
        {
            State = NewState;
            Visible = State != null && State.Playback != null;
            NeedsLayout = true;
        }

        #region Transport

        /// <summary>
        /// What the space bar does: stop whatever is playing, or start again in
        /// the direction it was last going. Unlike the play button, this does not
        /// turn a rewind into a forward run - pausing is what was asked for.
        /// </summary>
        public void TogglePaused()
        {
            if (State == null)
                return;

            if (State.Playback.Playing)
            {
                State.Playback.Playing = false;
                return;
            }

            if (State.Playback.Direction < 0)
                PlayBackwards();
            else
                TogglePlaying();
        }

        /// <summary>
        /// What the play button does: run forwards, or stop if that is already
        /// what is happening.
        /// </summary>
        public void TogglePlaying()
        {
            if (State == null)
                return;

            MoviePlayback Playback = State.Playback;
            if (Playback.Playing && Playback.Direction > 0)
            {
                Playback.Playing = false;
                return;
            }

            // Starting again from the very end would sit there doing nothing.
            if (Playback.Position >= Playback.Duration)
                State.SeekTo(State.SeekableStart);

            Playback.Direction = 1;
            Playback.Playing = true;
        }

        public void PlayBackwards()
        {
            if (State == null)
                return;

            MoviePlayback Playback = State.Playback;
            if (Playback.Playing && Playback.Direction < 0)
            {
                Playback.Playing = false;
                return;
            }

            Playback.Direction = -1;
            Playback.Playing = true;
        }

        public void StepSpeed(int By)
        {
            if (State == null)
                return;

            int At = 0;
            for (int i = 0; i < Speeds.Length; ++i)
                if (Math.Abs(Speeds[i] - State.Playback.Speed) < Math.Abs(Speeds[At] - State.Playback.Speed))
                    At = i;

            At = (int)MathHelper.Clamp(At + By, 0, Speeds.Length - 1);
            State.Playback.Speed = Speeds[At];
        }

        /// <summary>
        /// Moves the playhead by By, leaving playback in whatever state it was in.
        /// </summary>
        public void Nudge(TimeSpan By)
        {
            if (State == null)
                return;
            State.SeekTo(State.Playback.Position + By);
        }

        public void SeekToStart()
        {
            if (State != null)
                State.SeekTo(State.SeekableStart);
        }

        public void SeekToEnd()
        {
            if (State != null)
                State.SeekTo(State.Playback.Duration);
        }

        /// <summary>
        /// The keys the transport answers to.
        ///
        /// There is no focus system in this framework and building one for a
        /// single consumer would be a lot of machinery for nothing, so GameDesktop
        /// hands key presses straight here.
        /// </summary>
        public Boolean KeyPressed(Keys Key, KeyboardState Keyboard)
        {
            if (State == null)
                return false;

            Boolean Shift = Keyboard.IsKeyDown(Keys.LeftShift) || Keyboard.IsKeyDown(Keys.RightShift);

            switch (Key)
            {
                case Keys.Space:
                    TogglePaused();
                    return true;
                case Keys.R:
                    PlayBackwards();
                    return true;
                case Keys.Left:
                    Nudge(Shift ? -LongStep : -Step);
                    return true;
                case Keys.Right:
                    Nudge(Shift ? LongStep : Step);
                    return true;
                case Keys.Up:
                    StepSpeed(1);
                    return true;
                case Keys.Down:
                    StepSpeed(-1);
                    return true;
                case Keys.Home:
                    SeekToStart();
                    return true;
                case Keys.End:
                    SeekToEnd();
                    return true;
            }

            return false;
        }

        #endregion

        private void OnScrubMoved(UIScrubBar Bar)
        {
            // Rate limited in Update; this only records where the user wants to be.
        }

        private void OnScrubReleased(UIScrubBar Bar)
        {
            if (State != null)
                State.SeekTo(PositionOf(Bar));
        }

        private TimeSpan PositionOf(UIScrubBar Bar)
        {
            return TimeSpan.FromTicks((long)(State.Playback.Duration.Ticks * Bar.Position));
        }

        public override void Update(GameTime Time)
        {
            base.Update(Time);

            if (State == null)
                return;

            MoviePlayback Playback = State.Playback;

            if (Scrub.Dragging)
            {
                // Follow the thumb, but only as fast as a seek can keep up with.
                ScrubAccumulated += Time.ElapsedGameTime;
                if (ScrubAccumulated >= ScrubInterval)
                {
                    ScrubAccumulated = TimeSpan.Zero;
                    State.SeekTo(PositionOf(Scrub));
                }
            }
            else
            {
                ScrubAccumulated = TimeSpan.Zero;
                Scrub.Position = Fraction(Playback.Position, Playback.Duration);
            }

            Scrub.Played = Fraction(Playback.Position, Playback.Duration);

            PlayButton.Glyph = Playback.Playing && Playback.Direction > 0
                ? PlaybackGlyph.Pause
                : PlaybackGlyph.Play;
            ReverseButton.Glyph = Playback.Playing && Playback.Direction < 0
                ? PlaybackGlyph.Pause
                : PlaybackGlyph.Reverse;

            SpeedLabel.Text = FormatSpeed(Playback.Speed);
            ElapsedLabel.Text = FormatTime(Playback.Position);
            TotalLabel.Text = FormatTime(Playback.Duration);
        }

        private static double Fraction(TimeSpan At, TimeSpan Of)
        {
            if (Of.Ticks <= 0)
                return 0;
            return (double)At.Ticks / Of.Ticks;
        }

        private static String FormatTime(TimeSpan At)
        {
            if (At < TimeSpan.Zero)
                At = TimeSpan.Zero;

            // Minutes keep counting past an hour rather than rolling over, so the
            // number always means the same thing however long the recording is.
            return ((int)At.TotalMinutes) + ":" + At.Seconds.ToString("00");
        }

        private static String FormatSpeed(double Speed)
        {
            if (Speed < 1)
                return Speed.ToString("0.##",
                    System.Globalization.CultureInfo.InvariantCulture) + "x";
            return ((int)Speed) + "x";
        }

        public override void LayoutSubviews()
        {
            Rectangle Client = ClientBounds;

            Scrub.Bounds = new Rectangle(Client.Left, Client.Top, Client.Width, 12);

            int Row = Client.Top + 14;

            // The controls sit centred as a group, so the eye finds them in the
            // same place whatever the window is doing. The timecodes are laid out
            // against the edges rather than in the group, so a long one cannot
            // shove the centre off.
            const int Gap = 4;
            int GroupWidth =
                ReverseButton.Bounds.Width + Gap +
                PlayButton.Bounds.Width + Gap * 4 +
                SlowerButton.Bounds.Width + Gap +
                SpeedLabel.Bounds.Width + Gap +
                FasterButton.Bounds.Width;

            int At = Client.Left + (Client.Width - GroupWidth) / 2;

            ReverseButton.Bounds.X = At;
            ReverseButton.Bounds.Y = Row;
            At += ReverseButton.Bounds.Width + Gap;

            PlayButton.Bounds.X = At;
            PlayButton.Bounds.Y = Row;
            At += PlayButton.Bounds.Width + Gap * 4;

            SlowerButton.Bounds.X = At;
            SlowerButton.Bounds.Y = Row;
            At += SlowerButton.Bounds.Width + Gap;

            SpeedLabel.Bounds.X = At;
            SpeedLabel.Bounds.Y = Row + 3;
            At += SpeedLabel.Bounds.Width + Gap;

            FasterButton.Bounds.X = At;
            FasterButton.Bounds.Y = Row;

            ElapsedLabel.Bounds.X = Client.Left;
            ElapsedLabel.Bounds.Y = Row + 3;

            TotalLabel.Bounds.X = Client.Left + Client.Width - TotalLabel.Bounds.Width;
            TotalLabel.Bounds.Y = Row + 3;

            base.LayoutSubviews();
        }
    }
}
