using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace CTC
{
    public interface PacketStream
    {
        /// <summary>
        /// Check if there is a new packet available, taking into account the timeout stored in the last packet.
        /// </summary>
        /// <returns>True if there is a packet available.</returns>
        Boolean Poll(GameTime Time);

        /// <summary>
        /// Reads the next packet from the stream and returns it.
        ///
        /// The message is only valid until the next call to Read: an implementation
        /// is free to hand back one buffer it reuses, and MoviePlayback does.
        /// </summary>
        /// <returns>The next packet, or null if we've reached the end of the stream.</returns>
        NetworkMessage Read(GameTime Time);

        /// <summary>
        /// The name of the stream (filename or hostname)
        /// </summary>
        /// <returns>Either a filename (the last part) or a host specifier (ip:host)</returns>
        String Name
        {
            get;
        }

        /// <summary>
        /// Writes a packet to the stream
        /// </summary>
        /// <param name="nmsg"></param>
        void Write(NetworkMessage nmsg);
    }
}
