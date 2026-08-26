using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;

namespace CTC
{
    /// <summary>
    /// Holds all game data information for a single connection to a server
    /// or possibly a playing movie, either way this is usually passed to
    /// GameFrame.AddClient in order to create a new window displaying it.
    ///
    /// For a recording this also owns the seek engine. A .tmv has no index and
    /// no keyframes, so the only way to reach an arbitrary point is to feed the
    /// packets through and see what state comes out. What makes that affordable
    /// is keeping snapshots of the state as it goes, so a seek only ever replays
    /// from the nearest one rather than from the start of the recording.
    /// </summary>
    public class ClientState
    {
        public readonly ClientViewport Viewport;
        public readonly TibiaGameData GameData;
        public readonly TibiaGameProtocol Protocol;

        /// <summary>
        /// Non-null when this state is driven by a recording rather than a live
        /// stream. Seeking is only possible in that case.
        /// </summary>
        public readonly MoviePlayback Playback;

        private readonly PacketStream InStream;

        public ClientState(PacketStream InStream)
        {
            this.InStream = InStream;
            FileStream datFile = new FileStream(AppPaths.GameDataFile("Tibia.dat"), FileMode.Open, FileAccess.Read);
            FileStream sprFile = new FileStream(AppPaths.GameDataFile("Tibia.spr"), FileMode.Open, FileAccess.Read);
            GameData = new TibiaGameData(datFile, sprFile);
            Protocol = new TibiaGameProtocol(GameData);
            Viewport = new ClientViewport(GameData, Protocol);
        }

        public ClientState(TibiaMovieRecording Recording)
            : this(new MoviePlayback(Recording))
        {
            Playback = (MoviePlayback)InStream;
        }

        public String HostName
        {
            get {
                return InStream.Name;
            }
        }

        #region Snapshot density

        // Snapshots are not spread evenly. Reversing needs one within about a
        // second to be smooth, while a point half an hour back only has to be
        // reachable at all. So the required spacing widens with distance from the
        // playhead, and one rule drives both what gets captured and what gets
        // kept:
        //
        //   within  5s   1 second apart
        //   within 30s   5 seconds apart
        //   within 30m   30 seconds apart
        //   beyond       15 minutes apart
        //
        // Distant snapshots thin out as the playhead moves away from them, and
        // the region around wherever it lands fills back in on the way there.

        /// <summary>
        /// How far behind the playhead the fine snapshots reach, and how far apart
        /// the ordinary ones are. Beyond this the coarse grid takes over.
        /// </summary>
        public TimeSpan SnapshotInterval = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Where the coarse grid gives up and the sparse one starts. A recording
        /// longer than this does not need thirty second granularity throughout;
        /// a quarter hour is enough to get back to.
        /// </summary>
        public TimeSpan SparseDistance = TimeSpan.FromMinutes(30);
        public TimeSpan SparseInterval = TimeSpan.FromMinutes(15);

        /// <summary>
        /// The points behind the playhead a reverse step wants covered.
        /// </summary>
        private static readonly TimeSpan[] LadderOffsets =
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(20),
            TimeSpan.FromSeconds(25),
        };

        /// <summary>
        /// Backwards playback restores and replays on every step, so the steps are
        /// rationed: state is applied fifteen times a second and the frames in
        /// between redraw the last one. Nothing is lost by that here - the client
        /// has no walking animation and no interpolation, so creatures only ever
        /// move on packet boundaries anyway.
        /// </summary>
        private static readonly TimeSpan ReverseStepInterval = TimeSpan.FromSeconds(1.0 / 15.0);

        #endregion

        #region Seeking

        private readonly List<ClientSnapshot> Snapshots = new List<ClientSnapshot>();
        private TimeSpan ReverseAccumulated = TimeSpan.Zero;

        /// <summary>
        /// The earliest position that can be restored.
        ///
        /// Before the login packet there is no player, and the parser dereferences
        /// it unconditionally when reading any creature, so a state rewound past
        /// that point crashes on the next packet rather than showing an empty
        /// screen. PrimeToLogin establishes this and SeekTo clamps to it.
        /// </summary>
        public TimeSpan SeekableStart
        {
            get { return _SeekableStart; }
        }
        private TimeSpan _SeekableStart = TimeSpan.Zero;

        /// <summary>
        /// True while a seek is replaying. Views use it to drop the cosmetic
        /// events - magic effects, missiles, animated text - that would otherwise
        /// all fire at once as the packets rush past.
        /// </summary>
        public Boolean Seeking
        {
            get { return _Seeking; }
        }
        private Boolean _Seeking = false;

        /// <summary>
        /// Bumped once per seek, for anything holding time-based state that has to
        /// be thrown away rather than carried across.
        /// </summary>
        public int SeekGeneration
        {
            get { return _SeekGeneration; }
        }
        private int _SeekGeneration = 0;

        public delegate void StateEvent(ClientState State);
        public event StateEvent SeekCompleted;
        public event StateEvent PlayerIdentityChanged;

        #endregion

        private void ReadPackets(GameTime Time)
        {
            while (InStream.Poll(Time))
            {
                NetworkMessage nmsg = InStream.Read(Time);
                if (nmsg == null)
                    return;
                Deliver(nmsg);
            }
        }

        /// <summary>
        /// Parses one packet into the client state.
        ///
        /// A malformed packet is logged and skipped rather than thrown: the
        /// original fast-forward let it escape, which would abort a seek halfway
        /// and leave the state neither where it was nor where it was going.
        /// </summary>
        private void Deliver(NetworkMessage nmsg)
        {
            try
            {
                Protocol.parsePacket(nmsg);
            }
            catch (Exception ex)
            {
                Log.Error("Protocol Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Feeds packets forward until Target, capturing snapshots on the way.
        ///
        /// This is the one and only delivery path: normal playback, the startup
        /// prime and seek replay all come through here, which is why a long
        /// forward seek leaves the timeline behind it populated without that
        /// needing to be a special case.
        /// </summary>
        private void ReplayTo(TimeSpan Target, List<TimeSpan> Marks)
        {
            TibiaMovieRecording Recording = Playback.Recording;
            int Mark = 0;

            while (true)
            {
                // A snapshot taken now describes the timeline from the last packet
                // delivered until the next one arrives, so it can serve any mark
                // falling inside that window.
                TimeSpan From = Recording.TimeAt(Playback.Cursor - 1);
                TimeSpan Until = Recording.TimeAt(Playback.Cursor);

                Boolean Captured = false;
                while (Mark < Marks.Count && Marks[Mark] < Until)
                {
                    if (!Captured && Marks[Mark] >= From)
                    {
                        Capture();
                        Captured = true;
                    }
                    ++Mark;
                }

                if (Playback.Cursor >= Recording.Packets.Count)
                    break;
                if (Recording.Packets[Playback.Cursor].Time > Target)
                    break;

                MaybeSnapshot();

                NetworkMessage nmsg = Playback.Read(null);
                if (nmsg == null)
                    break;
                Deliver(nmsg);
            }
        }

        /// <summary>
        /// Reads far enough into the recording for the player to exist.
        ///
        /// The desktop needs a name for the tab and the parser needs a Player
        /// before it can read any creature, so this always runs at startup. In
        /// all three bundled recordings the very first packet carries the login
        /// and the first map description together.
        /// </summary>
        public void PrimeToLogin()
        {
            if (Playback == null)
                throw new NotSupportedException("Can only prime a recording.");

            while (Viewport.Player == null && !Playback.AtEnd)
            {
                NetworkMessage nmsg = Playback.Read(null);
                if (nmsg == null)
                    break;
                Deliver(nmsg);
            }

            _SeekableStart = Playback.Recording.TimeAt(Playback.Cursor - 1);
            Playback.Floor = _SeekableStart;
            Playback.Rebase(_SeekableStart, Playback.Cursor);

            // The baseline. Everything else can be thinned away; this one is the
            // only route back to the start of the recording.
            Capture();
        }

        /// <summary>
        /// Moves the playhead to Target and brings the client state with it.
        /// </summary>
        public void SeekTo(TimeSpan Target)
        {
            SeekTo(Target, Target < Playback.Position);
        }

        private void SeekTo(TimeSpan Target, Boolean BuildLadder)
        {
            if (Playback == null)
                throw new NotSupportedException("Can't seek a live stream.");

            // Clamp first. Everything below assumes there is a player.
            if (Target < SeekableStart)
                Target = SeekableStart;
            if (Target > Playback.Duration)
                Target = Playback.Duration;

            ++_SeekGeneration;
            _Seeking = true;
            Boolean IdentityChanged = false;
            try
            {
                List<TimeSpan> Marks = BuildLadder
                    ? LadderMarks(Target)
                    : new List<TimeSpan>();

                TimeSpan Earliest = Target;
                foreach (TimeSpan Mark in Marks)
                    if (Mark < Earliest)
                        Earliest = Mark;

                // Replaying forward from where we already are only works if the
                // state is not already past the earliest point we need.
                Boolean CanContinue =
                    Playback.Recording.TimeAt(Playback.Cursor - 1) <= Earliest;

                int Base = IndexAtOrBefore(Earliest);
                Boolean Shortcut = Base >= 0 && Snapshots[Base].Cursor > Playback.Cursor;

                if (!CanContinue || Shortcut)
                {
                    if (Base < 0)
                        Base = 0;
                    IdentityChanged = Snapshots[Base].RestoreInto(this);
                    Playback.Rebase(Snapshots[Base].Position, Snapshots[Base].Cursor);
                }

                ReplayTo(Target, Marks);
                Playback.Rebase(Target, Playback.Cursor);
                Thin();
            }
            finally
            {
                _Seeking = false;
            }

            if (IdentityChanged && PlayerIdentityChanged != null)
                PlayerIdentityChanged(this);
            if (SeekCompleted != null)
                SeekCompleted(this);
        }

        public void Update(GameTime Time)
        {
            if (Playback == null)
            {
                ReadPackets(Time);
                return;
            }

            TimeSpan Before = Playback.Position;
            Playback.Advance(Time);

            if (Playback.Position >= Before)
            {
                ReverseAccumulated = TimeSpan.Zero;
                ReplayTo(Playback.Position, NoMarks);
                Thin();
                return;
            }

            // Backwards. See ReverseStepInterval.
            ReverseAccumulated += Time.ElapsedGameTime;
            if (ReverseAccumulated < ReverseStepInterval)
                return;
            ReverseAccumulated = TimeSpan.Zero;

            SeekTo(Playback.Position, true);
        }

        private static readonly List<TimeSpan> NoMarks = new List<TimeSpan>();

        #region Snapshot store

        private void Capture()
        {
            ClientSnapshot Snapshot = ClientSnapshot.Capture(this);

            int At = IndexAtOrBefore(Snapshot.Position);
            if (At >= 0 && Snapshots[At].Cursor == Snapshot.Cursor)
                return;

            Snapshots.Insert(At + 1, Snapshot);
        }

        /// <summary>
        /// Takes a snapshot if the timeline behind the playhead has gone longer
        /// than SnapshotInterval without one.
        /// </summary>
        private void MaybeSnapshot()
        {
            // Before the login there is nothing worth restoring, and a restore to
            // it would crash the parser on the next creature.
            if (Viewport.Player == null)
                return;

            TimeSpan Here = Playback.Recording.TimeAt(Playback.Cursor - 1);
            int At = IndexAtOrBefore(Here);
            if (At >= 0 && Here - Snapshots[At].Position < SnapshotInterval)
                return;

            Capture();
        }

        /// <summary>
        /// The points behind Target that want a snapshot and do not have one near
        /// enough to be worth reusing.
        ///
        /// The tolerance on each rung is the gap to the rung above it, so a mark
        /// built once serves until the playhead has travelled that far - otherwise
        /// every single reverse step would rebuild the whole ladder.
        /// </summary>
        private List<TimeSpan> LadderMarks(TimeSpan Target)
        {
            List<TimeSpan> Marks = new List<TimeSpan>();

            TimeSpan Previous = TimeSpan.Zero;
            foreach (TimeSpan Offset in LadderOffsets)
            {
                TimeSpan Tolerance = Offset - Previous;
                Previous = Offset;

                TimeSpan At = Target - Offset;
                if (At < SeekableStart)
                    break;

                int Index = IndexAtOrBefore(At);
                if (Index >= 0 && At - Snapshots[Index].Position <= Tolerance)
                    continue;

                Marks.Add(At);
            }

            Marks.Sort();
            return Marks;
        }

        /// <summary>
        /// Index of the newest snapshot at or before Target, or -1 if there is none.
        /// </summary>
        private int IndexAtOrBefore(TimeSpan Target)
        {
            int Low = 0;
            int High = Snapshots.Count;
            while (Low < High)
            {
                int Middle = Low + (High - Low) / 2;
                if (Snapshots[Middle].Position <= Target)
                    Low = Middle + 1;
                else
                    High = Middle;
            }
            return Low - 1;
        }

        /// <summary>
        /// Drops snapshots that have become denser than their distance from the
        /// playhead warrants. See the note on SnapshotInterval.
        /// </summary>
        private void Thin()
        {
            if (Snapshots.Count < 3)
                return;

            TimeSpan Playhead = Playback.Position;
            int Write = 1;

            for (int Read = 1; Read < Snapshots.Count; ++Read)
            {
                ClientSnapshot Snapshot = Snapshots[Read];

                // The newest is always worth keeping: it is the one the next
                // reverse step will start from.
                Boolean Keep = Read == Snapshots.Count - 1;

                if (!Keep)
                {
                    TimeSpan Distance = Snapshot.Position > Playhead
                        ? Snapshot.Position - Playhead
                        : Playhead - Snapshot.Position;

                    Keep = Snapshot.Position - Snapshots[Write - 1].Position
                        >= RequiredSpacing(Distance);
                }

                if (Keep)
                    Snapshots[Write++] = Snapshot;
            }

            Snapshots.RemoveRange(Write, Snapshots.Count - Write);
        }

        private TimeSpan RequiredSpacing(TimeSpan Distance)
        {
            if (Distance <= LadderOffsets[1])
                return LadderOffsets[0];
            if (Distance <= SnapshotInterval)
                return LadderOffsets[1] - LadderOffsets[0];
            if (Distance <= SparseDistance)
                return SnapshotInterval;
            return SparseInterval;
        }

        /// <summary>
        /// How many snapshots are being held. Diagnostics only.
        /// </summary>
        public int SnapshotCount
        {
            get { return Snapshots.Count; }
        }

        #endregion
    }
}
