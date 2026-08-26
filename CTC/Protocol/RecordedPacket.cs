using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    /// <summary>
    /// One packet out of a recording, with the time it arrives at.
    ///
    /// The .tmv format stores delays between chunks rather than absolute times;
    /// TibiaMovieRecording accumulates them once at load so that seeking can
    /// binary search the timeline instead of walking it.
    ///
    /// Data is exactly the packet's length. That matters: NetworkMessage carries
    /// a 64KB buffer, and a recording holds upwards of twenty thousand packets.
    /// </summary>
    public struct RecordedPacket
    {
        /// <summary>
        /// Time from the start of the recording at which this packet arrives.
        /// </summary>
        public readonly TimeSpan Time;

        /// <summary>
        /// The packet payload, without the length prefix the file stores it behind.
        /// </summary>
        public readonly byte[] Data;

        public RecordedPacket(TimeSpan Time, byte[] Data)
        {
            this.Time = Time;
            this.Data = Data;
        }
    }
}
