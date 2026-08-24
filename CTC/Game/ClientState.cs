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
    /// </summary>
    public class ClientState
    {
        public readonly ClientViewport Viewport;
        public readonly TibiaGameData GameData;
        public readonly TibiaGameProtocol Protocol;
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

        public String HostName
        {
            get {
                return InStream.Name;
            }
        }

        private void ReadPackets(GameTime Time)
        {
            while (InStream.Poll(Time))
            {
                try
                {
                    NetworkMessage nmsg = InStream.Read(Time);
                    if (nmsg == null)
                        return;
                    Protocol.parsePacket(nmsg);
                }
                catch (Exception ex)
                {
                    Log.Error("Protocol Error: " + ex.Message);
                }
            }
        }

        public void ForwardTo(TimeSpan Span)
        {
            if (!(InStream is TibiaMovieStream))
                throw new NotSupportedException("Can't fast-forward non-movie streams.");

            TibiaMovieStream Movie = (TibiaMovieStream)InStream;

            while (Movie.Elapsed.TotalSeconds < Span.TotalSeconds)
            {
                NetworkMessage nmsg = Movie.Read(null);
                // Read returns null once the recording is exhausted; without this guard a
                // movie shorter than Span walks off the end into a NullReferenceException.
                if (nmsg == null)
                    break;
                Protocol.parsePacket(nmsg);
            }
        }

        public void Update(GameTime Time)
        {
            ReadPackets(Time);
        }
    }
}
