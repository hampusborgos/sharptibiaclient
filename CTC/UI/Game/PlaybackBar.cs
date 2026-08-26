using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// to ask for a seek. Dragging pays for itself as it goes: a seek is a
    /// snapshot restore and a replay, and how long that takes depends entirely on
    /// where the thumb has got to, so each one is timed and the next waits out
    /// twice what the last one cost.
    /// </summary>
    public class PlaybackBar : UIView
    {
        private ClientState State;

        private UIScrubBar Scrub;
        private PlaybackButton ReverseButton;
        private PlaybackButton PauseButton;
        private PlaybackButton ForwardButton;
        private UILabel SpeedLabel;
        private UILabel ElapsedLabel;
        private UILabel TotalLabel;

        /// <summary>
        /// The speeds the -/+ buttons step through.
        /// </summary>
        private static readonly double[] Speeds = { 0.25, 0.5, 1, 2, 4, 8 };

        /// <summary>
        /// The share of its time the client is willing to spend seeking while the
        /// thumb is being dragged. A third leaves the drag itself smooth: the seek
        /// that costs a millisecond happens again on the next frame, and the one
        /// that costs eighty waits out its own cost twice over rather than
        /// stalling every frame behind it.
        /// </summary>
        private const double ScrubDutyCost = 2.0;

        /// <summary>
        /// Never wait longer than this between drag seeks, however dear the last
        /// one was, or a drag through an expensive stretch would stop showing
        /// anything at all.
        /// </summary>
        private static readonly TimeSpan MaximumScrubInterval = TimeSpan.FromSeconds(0.5);

        private TimeSpan ScrubAccumulated = TimeSpan.Zero;
        private TimeSpan ScrubInterval = TimeSpan.Zero;

        /// <summary>
        /// Where the last drag seek went, so a thumb that has not moved since is
        /// not sought to again.
        /// </summary>
        private TimeSpan ScrubbedTo = TimeSpan.MinValue;

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

            // Which way, and whether at all: the two outer buttons choose a
            // direction and the middle one starts and stops, so pausing never
            // costs you the direction you were going in.
            ReverseButton = new PlaybackButton(PlaybackGlyph.Rewind);
            ReverseButton.ButtonReleasedInside += delegate(UIButton Button, MouseState mouse)
            {
                PlayBackwards();
            };
            AddSubview(ReverseButton);

            PauseButton = new PlaybackButton(PlaybackGlyph.Play);
            PauseButton.ButtonReleasedInside += delegate(UIButton Button, MouseState mouse)
            {
                TogglePaused();
            };
            AddSubview(PauseButton);

            ForwardButton = new PlaybackButton(PlaybackGlyph.FastForward);
            ForwardButton.ButtonReleasedInside += delegate(UIButton Button, MouseState mouse)
            {
                PlayForwards();
            };
            AddSubview(ForwardButton);

            // No stepper buttons: the direction buttons speed themselves up, and
            // the readout carries the sign, so this is a readout and nothing more.
            SpeedLabel = new UILabel("1x");
            SpeedLabel.Autoresizable = false;
            SpeedLabel.TextAlignment = UITextAlignment.Center;
            SpeedLabel.Bounds = new Rectangle(0, 0, 52, 18);
            AddSubview(SpeedLabel);

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

            // Starting again from the very end would sit there doing nothing.
            if (State.Playback.Direction > 0 && State.Playback.Position >= State.Playback.Duration)
                State.SeekTo(State.SeekableStart);

            State.Playback.Playing = true;
        }

        public void PlayForwards()
        {
            Play(1);
        }

        public void PlayBackwards()
        {
            Play(-1);
        }

        /// <summary>
        /// What the outer buttons do: set off that way, and once already going
        /// that way, go faster. Tapping never stops playback - stopping is the
        /// middle button's job, and a transport where the same button starts and
        /// stops cannot be tapped up to speed.
        ///
        /// Turning round starts again at 1x. Whatever speed suited watching one
        /// way is a fresh decision the other.
        /// </summary>
        private void Play(int Direction)
        {
            if (State == null)
                return;

            MoviePlayback Playback = State.Playback;

            if (Math.Sign(Playback.Direction) == Direction)
            {
                if (Playback.Playing)
                    StepSpeed(1);
                else
                    Playback.Playing = true;
                return;
            }

            // Starting forwards from the very end, or backwards from the start,
            // would sit there doing nothing.
            if (Direction > 0 && Playback.Position >= Playback.Duration)
                State.SeekTo(State.SeekableStart);

            Playback.Direction = Direction;
            Playback.Speed = 1;
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

        private static TimeSpan Shorter(TimeSpan One, TimeSpan Other)
        {
            return One < Other ? One : Other;
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
                // Follow the thumb as closely as the seeking can be afforded.
                ScrubAccumulated += Time.ElapsedGameTime;

                TimeSpan Target = PositionOf(Scrub);
                if (ScrubAccumulated >= ScrubInterval && Target != ScrubbedTo)
                {
                    ScrubAccumulated = TimeSpan.Zero;
                    ScrubbedTo = Target;

                    Stopwatch Watch = Stopwatch.StartNew();
                    State.SeekTo(Target);
                    ScrubInterval = Shorter(
                        TimeSpan.FromTicks((long)(Watch.Elapsed.Ticks * ScrubDutyCost)),
                        MaximumScrubInterval);
                }
            }
            else
            {
                ScrubAccumulated = TimeSpan.Zero;
                ScrubInterval = TimeSpan.Zero;
                ScrubbedTo = TimeSpan.MinValue;
                Scrub.Position = Fraction(Playback.Position, Playback.Duration);
            }

            Scrub.Played = Fraction(Playback.Position, Playback.Duration);

            // The middle button says what pressing it will do: stop what is
            // running, or start again the way it was last going.
            if (Playback.Playing)
                PauseButton.Glyph = PlaybackGlyph.Pause;
            else
                PauseButton.Glyph = Playback.Direction < 0
                    ? PlaybackGlyph.Reverse
                    : PlaybackGlyph.Play;

            // The arrows light when they are doing something plain play is not.
            // Forward at 1x is plain play, and the middle button already says so;
            // any other speed is the fast forward's doing. Backwards is lit
            // whatever the speed, because nothing else on the bar shows the
            // direction while it is running - the middle button reads as a pause.
            ForwardButton.Active = Playback.Playing && Playback.Direction > 0 && Playback.Speed != 1;
            ReverseButton.Active = Playback.Playing && Playback.Direction < 0;

            SpeedLabel.Text = FormatSpeed(Playback.Speed * Math.Sign(Playback.Direction));
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

        /// <summary>
        /// The speed, signed: running backwards reads -2x, which is the only
        /// place the direction is written down now that the stepper buttons are
        /// gone.
        /// </summary>
        private static String FormatSpeed(double Speed)
        {
            String Sign = Speed < 0 ? "-" : "";
            double Magnitude = Math.Abs(Speed);

            if (Magnitude < 1)
                return Sign + Magnitude.ToString("0.##",
                    System.Globalization.CultureInfo.InvariantCulture) + "x";
            return Sign + ((int)Magnitude) + "x";
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
                PauseButton.Bounds.Width + Gap +
                ForwardButton.Bounds.Width + Gap * 4 +
                SpeedLabel.Bounds.Width;

            int At = Client.Left + (Client.Width - GroupWidth) / 2;

            ReverseButton.Bounds.X = At;
            ReverseButton.Bounds.Y = Row;
            At += ReverseButton.Bounds.Width + Gap;

            PauseButton.Bounds.X = At;
            PauseButton.Bounds.Y = Row;
            At += PauseButton.Bounds.Width + Gap;

            ForwardButton.Bounds.X = At;
            ForwardButton.Bounds.Y = Row;
            At += ForwardButton.Bounds.Width + Gap * 4;

            SpeedLabel.Bounds.X = At;
            SpeedLabel.Bounds.Y = Row + 3;

            ElapsedLabel.Bounds.X = Client.Left;
            ElapsedLabel.Bounds.Y = Row + 3;

            TotalLabel.Bounds.X = Client.Left + Client.Width - TotalLabel.Bounds.Width;
            TotalLabel.Bounds.Y = Row + 3;

            base.LayoutSubviews();
        }
    }
}
