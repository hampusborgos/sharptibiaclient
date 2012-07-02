using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    public interface PacketStream
    {
        /// <summary>
        /// Check if there is a new packet available, taking into account the timeout stored in the last packet.
        /// </summary>
        /// <returns>True if there is a packet available.</returns>
        Boolean Poll();

        /// <summary>
        /// Reads the next packet from the stream and returns it.
        /// </summary>
        /// <returns>The next packet, or null if we've reached the end of the stream.</returns>
        NetworkMessage Read();

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
