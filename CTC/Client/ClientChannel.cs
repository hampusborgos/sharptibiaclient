using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    public class ClientMessage
    {
        public MessageType Type;
        public DateTime Time;
        public String Text;
        public String Speaker;

        public ClientMessage()
        {
            Time = DateTime.Now;
        }

        public ClientMessage(MessageType type, DateTime time, String text)
        {
            Type = type;
            Time = time;
            Speaker = "";
            Text = text;
        }

        public ClientMessage(MessageType type, DateTime time, String speaker, String text)
        {
            Type = type;
            Time = time;
            Speaker = speaker;
            Text = text;
        }

        public String Message
        {
            get
            {
                return Time.Hour + ":" + Time.Minute + " " + (Speaker != "" ? Speaker + ": " : "") + Text;
            }
        }
    }

    public class ClientChannel
    {
        public UInt16 ID;
        public String Name;

        public List<ClientMessage> Messages = new List<ClientMessage>();

        public ClientChannel(UInt16 ChannelID, String ChannelName)
        {
            ID = ChannelID;
            Name = ChannelName;
        }

        public void Add(ClientMessage Message)
        {
            Messages.Add(Message);
        }
    }
}
