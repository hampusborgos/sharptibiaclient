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
            FileStream datFile = new FileStream("C:\\Users\\hjn\\Documents\\TibiaRC\\Tibia.dat", FileMode.Open);
            FileStream sprFile = new FileStream("C:\\Users\\hjn\\Documents\\TibiaRC\\Tibia.spr", FileMode.Open);
            GameData = new TibiaGameData(datFile, sprFile);
            Protocol = new TibiaGameProtocol(GameData);
            Viewport = new ClientViewport(GameData, Protocol);

            TibiaMovieStream MS = (TibiaMovieStream)InStream;
            while (MS.Elapsed.TotalMinutes < 20 || MS.Elapsed.Seconds < 20)
                Protocol.parsePacket(InStream.Read());
        }

        public String HostName
        {
            get {
                return InStream.Name;
            }
        }

        private void ReadPackets()
        {
            while (InStream.Poll())
            {
                try
                {
                    NetworkMessage nmsg = InStream.Read();
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

        public void Update(GameTime Time)
        {
            ReadPackets();
        }
    }
}
