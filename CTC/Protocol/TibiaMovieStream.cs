using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CTC
{
    /// <summary>
    /// Reads a Tibia packet stream from the disk, rather than a network connection.
    /// Will deliver packets in a timely fashion if instructed, or send all available in a burst.
    /// </summary>
    class TibiaMovieStream : PacketStream
    {
        /// <summary>
        /// Stream to read data from, whoever loads us is responsible from creating it from gzip archive or w/e
        /// </summary>
        private Stream File;

        /// <summary>
        /// Time next packet will arrive.
        /// </summary>
        public readonly String FileName;

        /// <summary>
        /// Time next packet will arrive.
        /// </summary>
        private long NextTime = 0;

        /// <summary>
        /// Packets read from file but not passed yet.
        /// </summary>
        private Queue<NetworkMessage> PendingPackets = new Queue<NetworkMessage>();

        /// <summary>
        /// Duration of the movie, in milliseconds
        /// </summary>
        public TimeSpan Duration = new TimeSpan(0);

        /// <summary>
        /// How much of the movie has played out so far.
        /// Note that this update in increments with each packet received,
        /// so it's not an absolute measure
        /// </summary>
        public TimeSpan Elapsed = new TimeSpan(0);

        /// <summary>
        /// 
        /// </summary>
        public double PlaybackSpeed = 1;

        /// <summary>
        /// Construct a TibiaMovieStream from a normal stream
        /// </summary>
        /// <param name="file">The stream to read from, this can be any type of stream. TibiaMovieStream assumes ownership of it.</param>
        public TibiaMovieStream(Stream file, String fileName)
        {
            File = file;
            FileName = fileName;

            /*
            inputFile = parent.inputFileName.OpenRead();
            if (parent.inputFileName.Extension == ".utmv")
                inStream = inputFile;
            else
                inStream = new System.IO.Compression.GZipStream(inputFile, System.IO.Compression.CompressionMode.Decompress);
            */

            // This ONLY handles 7.40 files so far, which is BAD

            // Skip TibiaMovie version
            File.ReadByte();
            File.ReadByte();

            // Skip Tibia version
            File.ReadByte();
            File.ReadByte();

            // Read duration
            byte[] bytesDuration = new byte[4];
            File.Read(bytesDuration, 0, 4);
            Duration = new TimeSpan(TimeSpan.TicksPerSecond * BitConverter.ToInt32(bytesDuration, 0));

            // File pointer is now where we want it, at the start of the stream
        }

        public String Name
        {
            get
            {
                return FileName;
            }
        }

        public Boolean Poll()
        {
            return PendingPackets.Count > 0 || DateTime.Now.Ticks > NextTime;
        }

        public NetworkMessage Read()
        {
            if (PendingPackets.Count > 0)
                return PendingPackets.Dequeue();

            while (true)
            {
                int chunkType = File.ReadByte();

                if (chunkType == 1)
                {
                    // Marker, skip
                    continue;
                }
                else
                {
                    // Read delay
                    byte[] bytesDelay = new byte[4];
                    File.Read(bytesDelay, 0, 4);
                    int delay = System.BitConverter.ToInt32(bytesDelay, 0);
                    Elapsed = new TimeSpan(Elapsed.Ticks + delay * TimeSpan.TicksPerMillisecond);

                    byte[] bytesChunkLength = new byte[2];
                    File.Read(bytesChunkLength, 0, 2);
                    int chunkLength = System.BitConverter.ToUInt16(bytesChunkLength, 0);

                    int bytesRead = 0;
                    while (bytesRead < chunkLength)
                    {
                        // Data chunk
                        byte[] bytesPacketLength = new byte[2];
                        File.Read(bytesPacketLength, 0, 2);
                        int packetLength = BitConverter.ToUInt16(bytesPacketLength, 0);
                        bytesRead += 2;

                        NetworkMessage msg = new NetworkMessage(true);
                        for (int i = 0; i < packetLength; ++i)
                            msg.AddByte(File.ReadByte());
                        bytesRead += packetLength;

                        PendingPackets.Enqueue(msg);
                    }

                    // Schedule next
                    NextTime = DateTime.Now.Ticks + (int)(delay * TimeSpan.TicksPerMillisecond / PlaybackSpeed);
                    if (PendingPackets.Count == 0)
                        return null;
                    return PendingPackets.Dequeue();
                }
            }
        }

        public void Write(NetworkMessage nmsg)
        {
            // do nothing
        }
    }
}
