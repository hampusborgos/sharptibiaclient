using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace CTC
{
    /// <summary>
    /// Who is mid-step, and how far through it they are.
    ///
    /// None of this belongs in the client state. It is not in the recording, a
    /// snapshot has no business copying it, and it is thrown away on a seek like
    /// every other animation - so it lives beside the effects, keyed by creature
    /// id, and the renderer reads it while drawing.
    ///
    /// A step lasts exactly as long as the server thinks it does: the ground's
    /// speed over the creature's own. Everything about how the legs move follows
    /// from that, so a hasted knight's cycle runs fast and the same knight over
    /// swamp plods, with no separate animation speed to keep in sync.
    /// </summary>
    public class CreatureWalks
    {
        private class Step
        {
            public double Started;
            public double Duration;

            /// <summary>
            /// The tile step being taken, so the creature can be drawn short of
            /// the tile it has already arrived on as far as the map is concerned.
            /// </summary>
            public int DX, DY;

            /// <summary>
            /// Which step this is. Consecutive steps start on opposite feet.
            /// </summary>
            public int Sequence;

            public Step(double Started, double Duration, int DX, int DY, int Sequence)
            {
                this.Started = Started;
                this.Duration = Duration;
                this.DX = DX;
                this.DY = DY;
                this.Sequence = Sequence;
            }
        }

        /// <summary>
        /// A step over ground that never said how fast it is. The value the dat
        /// gives ordinary ground.
        /// </summary>
        public const int DefaultGroundSpeed = 150;

        /// <summary>
        /// What a creature that never announced its speed walks at. Roughly a
        /// character at level one.
        /// </summary>
        public const int DefaultSpeed = 220;

        /// <summary>
        /// Bounds on a step. Speeds arrive from the wire and a nonsense one would
        /// otherwise freeze a creature mid-stride or flicker it.
        /// </summary>
        private const double MinimumStep = 0.1;
        private const double MaximumStep = 2.0;

        /// <summary>
        /// How far above the floor a gap between two steps can be and still count
        /// as the same walk. Anything slower is a creature taking its time, not a
        /// rhythm to match.
        /// </summary>
        private const double RhythmFactor = 4.0;

        /// <summary>
        /// How long a finished step is remembered. Only its sequence number is
        /// still of use, to start the next step on the other foot.
        /// </summary>
        private const double Remember = 10.0;

        private readonly Dictionary<UInt32, Step> Steps = new Dictionary<UInt32, Step>();

        /// <summary>
        /// The playback clock, not the wall clock: at four times speed the legs
        /// move four times as fast, and a paused client holds its stride.
        /// </summary>
        private double Now;

        public void Update(GameTime Time)
        {
            Now += Time.ElapsedGameTime.TotalSeconds;
        }

        /// <summary>
        /// A creature moved from one tile to another, over ground of the given
        /// speed.
        ///
        /// Anything that is not a step to a neighbouring tile is a teleport - a
        /// summon appearing, a player taking a portal - and has no walk in it, so
        /// whatever the creature was doing is simply dropped.
        /// </summary>
        public void Stepped(UInt32 CreatureID, MapPosition From, MapPosition To, int GroundSpeed, int Speed)
        {
            int DX = To.X - From.X;
            int DY = To.Y - From.Y;

            if (From.Z != To.Z || Math.Abs(DX) > 1 || Math.Abs(DY) > 1 || (DX == 0 && DY == 0))
            {
                Steps.Remove(CreatureID);
                return;
            }

            if (GroundSpeed <= 0)
                GroundSpeed = DefaultGroundSpeed;
            if (Speed <= 0)
                Speed = DefaultSpeed;

            // The ground's speed over the creature's own is how fast the server
            // *can* walk it. What the server actually walks it at is that times
            // some constant of its own, which no packet ever states - measuring
            // this recording puts it between one and two - so this is a floor,
            // not an answer.
            double Floor = GroundSpeed / (double)Speed;

            // A diagonal covers more ground and costs more of it.
            if (DX != 0 && DY != 0)
                Floor *= 2;

            double Duration = Floor;

            int Sequence = 0;
            Step Previous;
            if (Steps.TryGetValue(CreatureID, out Previous))
            {
                // The same step can arrive twice. Your own walk comes as a map
                // scroll and, often, as a creature move in the same breath, and
                // starting it again would rubber-band the slide back a tile.
                if (Previous.Started == Now && Previous.DX == DX && Previous.DY == DY)
                    return;

                Sequence = Previous.Sequence + 1;

                // While a creature keeps walking, the gap since its last step is
                // a far better estimate of this one than the floor is: it is the
                // rhythm the server is actually walking it at, constant and all.
                // Match it and the slide runs out exactly as the next step lands,
                // so a walk of any length is one continuous movement instead of a
                // series of darts and pauses. Half the old estimate keeps a
                // single late packet from lurching the world.
                double Gap = Now - Previous.Started;
                if (Gap >= Floor && Gap <= Floor * RhythmFactor)
                    Duration = (Previous.Duration + Gap) / 2;
            }

            Steps[CreatureID] = new Step(
                Now, MathHelper.Clamp((float)Duration, (float)MinimumStep, (float)MaximumStep),
                DX, DY, Sequence);
        }

        /// <summary>
        /// Which pose to draw a creature in, out of the Phases its sprite has.
        /// Phase 0 is standing still; the rest are the walk cycle, spread over
        /// the step so it finishes exactly as the creature arrives.
        /// </summary>
        public int Phase(UInt32 CreatureID, int Phases)
        {
            if (Phases <= 1)
                return 0;

            double Progressed = Progress(CreatureID);
            if (Progressed < 0)
                return 0;

            int Cycle = Phases - 1;
            return 1 + (Steps[CreatureID].Sequence + (int)(Progressed * Cycle)) % Cycle;
        }

        /// <summary>
        /// Whether a creature is between tiles at all, which is what decides
        /// whether it has to be drawn out of its tile's turn.
        /// </summary>
        public Boolean Stepping(UInt32 CreatureID)
        {
            return Progress(CreatureID) >= 0;
        }

        /// <summary>
        /// How far short of its tile a creature still is, in map pixels. The map
        /// moves a creature the moment the packet arrives; this is the distance
        /// back to where it visually still is, and it runs out exactly as the
        /// walk cycle finishes.
        /// </summary>
        public Vector2 Offset(UInt32 CreatureID)
        {
            double Progressed = Progress(CreatureID);
            if (Progressed < 0)
                return Vector2.Zero;

            Step Current = Steps[CreatureID];
            float Remaining = (float)(1.0 - Progressed);

            return new Vector2(-32 * Current.DX * Remaining, -32 * Current.DY * Remaining);
        }

        /// <summary>
        /// How far through its step a creature is, or -1 when it is standing.
        /// </summary>
        private double Progress(UInt32 CreatureID)
        {
            Step Current;
            if (!Steps.TryGetValue(CreatureID, out Current))
                return -1;

            double Progressed = (Now - Current.Started) / Current.Duration;
            if (Progressed < 0 || Progressed >= 1)
                return -1;

            return Progressed;
        }

        /// <summary>
        /// Forgets creatures that stopped walking a while ago. Called from the
        /// canvas's own sweep rather than every frame.
        /// </summary>
        public void Cleanup()
        {
            List<UInt32> Done = new List<UInt32>();

            foreach (KeyValuePair<UInt32, Step> Walking in Steps)
                if (Now > Walking.Value.Started + Walking.Value.Duration + Remember)
                    Done.Add(Walking.Key);

            foreach (UInt32 CreatureID in Done)
                Steps.Remove(CreatureID);
        }

        public void Clear()
        {
            Steps.Clear();
        }
    }
}
