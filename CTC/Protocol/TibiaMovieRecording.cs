using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CTC
{
    /// <summary>
    /// A .tmv recording, parsed into memory in one pass.
    ///
    /// The file itself is a forward-only append log read through a GZipStream:
    /// no index, no keyframes, only relative delays. Nothing in it can be seeked.
    /// So we read the whole thing up front and keep an array of packets with
    /// absolute timestamps, which is what makes a scrub bar possible at all.
    ///
    /// The cost is trivial for the recordings this client sees: the largest is
    /// 2.1MB uncompressed and 21891 packets.
    ///
    /// The instance is immutable once loaded. MoviePlayback owns the cursor.
    /// </summary>
    public class TibiaMovieRecording
    {
        /// <summary>
        /// File name of the recording, reported as the "host" it is playing from.
        /// </summary>
        public readonly String Name;

        /// <summary>
        /// TibiaMovie container version, from the file header.
        /// </summary>
        public readonly int MovieVersion;

        /// <summary>
        /// Tibia protocol version the recording was made against (740 here).
        /// </summary>
        public readonly int TibiaVersion;

        /// <summary>
        /// How long the recording runs for.
        /// </summary>
        public readonly TimeSpan Duration;

        /// <summary>
        /// Every packet in the recording, in order, with absolute timestamps.
        /// </summary>
        public readonly List<RecordedPacket> Packets;

        private TibiaMovieRecording(
            String Name, int MovieVersion, int TibiaVersion,
            TimeSpan Duration, List<RecordedPacket> Packets)
        {
            this.Name = Name;
            this.MovieVersion = MovieVersion;
            this.TibiaVersion = TibiaVersion;
            this.Duration = Duration;
            this.Packets = Packets;
        }

        /// <summary>
        /// Index of the first packet arriving after Position, which is also the
        /// cursor value meaning "everything up to and including Position has been
        /// delivered".
        /// </summary>
        public int CursorAt(TimeSpan Position)
        {
            int Low = 0;
            int High = Packets.Count;
            while (Low < High)
            {
                int Middle = Low + (High - Low) / 2;
                if (Packets[Middle].Time <= Position)
                    Low = Middle + 1;
                else
                    High = Middle;
            }
            return Low;
        }

        /// <summary>
        /// Time of the packet at Index, or Duration once the recording runs out.
        /// </summary>
        public TimeSpan TimeAt(int Index)
        {
            if (Index < 0)
                return TimeSpan.Zero;
            if (Index >= Packets.Count)
                return Duration;
            return Packets[Index].Time;
        }

        /// <summary>
        /// Reads an entire movie. Assumes ownership of File and closes it.
        /// </summary>
        public static TibiaMovieRecording Load(Stream File, String Name)
        {
            using (File)
            {
                // Header: TibiaMovie version, Tibia version, duration.
                // This ONLY handles 7.40 files so far, which is BAD.
                int MovieVersion = ReadU16(File);
                int TibiaVersion = ReadU16(File);

                byte[] DurationBytes = new byte[4];
                if (!ReadExact(File, DurationBytes, 4))
                    throw new EndOfStreamException("Truncated movie header in " + Name);

                // The header field is MILLISECONDS. The original code multiplied it
                // by TicksPerSecond, making every duration a thousand times too long -
                // which went unnoticed because nothing ever read it. Verified against
                // the bundled recordings: the field equals the sum of the chunk delays
                // exactly (Kolskagg 2084693ms, Xyvero 7965997ms).
                TimeSpan Duration = TimeSpan.FromMilliseconds(
                    BitConverter.ToInt32(DurationBytes, 0));

                List<RecordedPacket> Packets = new List<RecordedPacket>();
                TimeSpan Elapsed = TimeSpan.Zero;

                while (true)
                {
                    int ChunkType = File.ReadByte();
                    if (ChunkType < 0)
                        break;

                    if (ChunkType == 1)
                    {
                        // Marker, skip
                        continue;
                    }

                    byte[] DelayBytes = new byte[4];
                    if (!ReadExact(File, DelayBytes, 4))
                        break;
                    Elapsed += TimeSpan.FromMilliseconds(BitConverter.ToInt32(DelayBytes, 0));

                    byte[] ChunkLengthBytes = new byte[2];
                    if (!ReadExact(File, ChunkLengthBytes, 2))
                        break;
                    int ChunkLength = BitConverter.ToUInt16(ChunkLengthBytes, 0);

                    int BytesRead = 0;
                    Boolean Truncated = false;
                    while (BytesRead < ChunkLength)
                    {
                        byte[] PacketLengthBytes = new byte[2];
                        if (!ReadExact(File, PacketLengthBytes, 2))
                        {
                            Truncated = true;
                            break;
                        }
                        int PacketLength = BitConverter.ToUInt16(PacketLengthBytes, 0);
                        BytesRead += 2;

                        byte[] PacketBytes = new byte[PacketLength];
                        if (!ReadExact(File, PacketBytes, PacketLength))
                        {
                            Truncated = true;
                            break;
                        }
                        BytesRead += PacketLength;

                        Packets.Add(new RecordedPacket(Elapsed, PacketBytes));
                    }

                    if (Truncated)
                        break;
                }

                // A truncated recording keeps whatever was read, but its header
                // duration is then a lie the scrub bar would render as dead space.
                if (Packets.Count > 0 && Packets[Packets.Count - 1].Time > Duration)
                {
                    Log.Warning(
                        "Movie header claims " + Duration + " but packets run to " +
                        Packets[Packets.Count - 1].Time + "; trusting the packets.");
                    Duration = Packets[Packets.Count - 1].Time;
                }

                return new TibiaMovieRecording(
                    Name, MovieVersion, TibiaVersion, Duration, Packets);
            }
        }

        private static int ReadU16(Stream File)
        {
            int Low = File.ReadByte();
            int High = File.ReadByte();
            if (Low < 0 || High < 0)
                throw new EndOfStreamException("Truncated movie header");
            return (High << 8) | Low;
        }

        /// <summary>
        /// Fills Buffer with exactly Count bytes, returning false if the stream ends first.
        ///
        /// Stream.Read is only ever obliged to return *some* bytes, and the movie files are
        /// read through a GZipStream, which routinely returns short reads at its internal
        /// buffer boundaries. The original code ignored the return value entirely, which
        /// happened to work on .NET Framework 4.0 but silently corrupts the packet stream
        /// on modern .NET.
        /// </summary>
        private static Boolean ReadExact(Stream File, byte[] Buffer, int Count)
        {
            int Offset = 0;
            while (Offset < Count)
            {
                int Read = File.Read(Buffer, Offset, Count - Offset);
                if (Read <= 0)
                    return false;
                Offset += Read;
            }
            return true;
        }
    }
}
