using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace CTC
{
    /// <summary>
    /// The playback clock for a recording, and the cursor into it.
    ///
    /// The old TibiaMovieStream scheduled the next packet against
    /// GameTime.TotalGameTime, which is absolute and monotonic: the playback
    /// timeline was welded to the game's own clock, so it could not be paused,
    /// rewound, or have its speed changed with any effect on packets already
    /// scheduled. Here Position is the timeline, we own it, and delivery is
    /// simply "every packet whose Time has been passed".
    ///
    /// Position moves continuously even in reverse, so the scrub bar tracks
    /// smoothly. Deciding how often to actually apply a reverse step is
    /// ClientState's job, because only it can pay the cost of a restore.
    /// </summary>
    public class MoviePlayback : PacketStream
    {
        public readonly TibiaMovieRecording Recording;

        public MoviePlayback(TibiaMovieRecording Recording)
        {
            this.Recording = Recording;
        }

        /// <summary>
        /// How far into the recording the playhead is.
        /// </summary>
        public TimeSpan Position
        {
            get { return _Position; }
        }
        private TimeSpan _Position = TimeSpan.Zero;

        /// <summary>
        /// Index of the next packet to deliver.
        /// </summary>
        public int Cursor
        {
            get { return _Cursor; }
        }
        private int _Cursor = 0;

        /// <summary>
        /// Multiplier on real time. Always positive; Direction carries the sign.
        /// </summary>
        public double Speed = 1.0;

        /// <summary>
        /// +1 plays forwards, -1 plays backwards.
        /// </summary>
        public int Direction = 1;

        public Boolean Playing = true;

        /// <summary>
        /// The earliest position the playhead may reach. ClientState raises this
        /// to the login packet, before which there is no state worth showing and
        /// the parser cannot read a creature at all.
        /// </summary>
        public TimeSpan Floor = TimeSpan.Zero;

        public TimeSpan Duration
        {
            get { return Recording.Duration; }
        }

        public Boolean AtEnd
        {
            get { return _Cursor >= Recording.Packets.Count; }
        }

        public String Name
        {
            get { return Recording.Name; }
        }

        /// <summary>
        /// One message, reused for every packet. See NetworkMessage.Load.
        /// </summary>
        private readonly NetworkMessage Scratch = new NetworkMessage(true);

        /// <summary>
        /// Moves the playhead. Call once per frame, before draining.
        /// </summary>
        public void Advance(GameTime Time)
        {
            if (!Playing || Time == null)
                return;

            _Position += TimeSpan.FromTicks(
                (long)(Time.ElapsedGameTime.Ticks * Speed * Direction));

            if (_Position >= Duration)
            {
                _Position = Duration;
                if (Direction > 0)
                    Playing = false;
            }
            else if (_Position <= Floor)
            {
                _Position = Floor;
                if (Direction < 0)
                    Playing = false;
            }
        }

        /// <summary>
        /// True when the next packet has come due. Side effect free; Advance owns
        /// the clock, so the GameTime handed in here is ignored.
        /// </summary>
        public Boolean Poll(GameTime Time)
        {
            return _Cursor < Recording.Packets.Count
                && Recording.Packets[_Cursor].Time <= _Position;
        }

        /// <summary>
        /// Hands back the next packet in a shared message, invalidated by the
        /// following call.
        /// </summary>
        public NetworkMessage Read(GameTime Time)
        {
            if (_Cursor >= Recording.Packets.Count)
                return null;

            RecordedPacket Packet = Recording.Packets[_Cursor++];
            Scratch.Load(Packet.Data, Packet.Data.Length);
            return Scratch;
        }

        public void Write(NetworkMessage nmsg)
        {
            // A recording is read-only.
        }

        /// <summary>
        /// Moves the playhead without delivering anything, which is only correct
        /// if the client state has been made to match. The seek engine calls this.
        /// </summary>
        public void Rebase(TimeSpan Position, int Cursor)
        {
            _Position = Position;
            _Cursor = Cursor;
        }
    }
}
