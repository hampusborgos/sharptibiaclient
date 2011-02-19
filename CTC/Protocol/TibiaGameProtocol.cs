using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace CTC
{
    public class ProtocolException : Exception
    {
        public ProtocolException(string Message)
            : base(Message)
        {
        }
    }

    public class TibiaGameProtocol
    {
        public Boolean SendFirst;

        protected TibiaGamePacketParserFactory Factory;
        protected Dictionary<int, PacketParser> PacketParsers = new Dictionary<int, PacketParser>();
        protected Dictionary<int, ProtocolEvent> PacketHandlers = new Dictionary<int, ProtocolEvent>();
        
        /// <summary>
        /// A handler for a protocol event
        /// </summary>
        /// <param name="props"></param>
        public class ProtocolEvent
        {
            public delegate void Handler(Packet props);
            public event Handler evt;

            public ProtocolEvent()
            {
            }

            public void Invoke(Packet props)
            {
                if (evt != null)
                    evt(props);
            }

            public void Add(Handler handler)
            {
                evt += handler;
            }

            public void Remove(Handler handler)
            {
                evt -= handler;
            }
        }

        public ProtocolEvent ErrorMessage = new ProtocolEvent();
        public ProtocolEvent MOTD = new ProtocolEvent();
        public ProtocolEvent Ping = new ProtocolEvent();

        public ProtocolEvent CanReport = new ProtocolEvent();
        public ProtocolEvent Death = new ProtocolEvent();

        public ProtocolEvent UpdateInventory = new ProtocolEvent();
        public ProtocolEvent ClearInventory = new ProtocolEvent();
        public ProtocolEvent OpenContainer = new ProtocolEvent();
        public ProtocolEvent CloseContainer = new ProtocolEvent();
        public ProtocolEvent ContainerAddItem = new ProtocolEvent();
        public ProtocolEvent ContainerTransformItem = new ProtocolEvent();
        public ProtocolEvent ContainerRemoveItem = new ProtocolEvent();

        public ProtocolEvent PlayerLogin = new ProtocolEvent();
        public ProtocolEvent UpdateStats = new ProtocolEvent();
        public ProtocolEvent UpdateSkills = new ProtocolEvent();
        public ProtocolEvent PlayerIcons = new ProtocolEvent();
        public ProtocolEvent CancelAttack = new ProtocolEvent();
        public ProtocolEvent CancelWalk = new ProtocolEvent();

        public ProtocolEvent CreatureMove = new ProtocolEvent();
        public ProtocolEvent CreatureTurn = new ProtocolEvent();
        public ProtocolEvent CreatureHealth = new ProtocolEvent();
        public ProtocolEvent CreatureLight = new ProtocolEvent();
        public ProtocolEvent CreatureRefresh = new ProtocolEvent();
        public ProtocolEvent CreatureSpeed = new ProtocolEvent();
        public ProtocolEvent CreatureSkull = new ProtocolEvent();
        public ProtocolEvent CreatureShield = new ProtocolEvent();

        public ProtocolEvent CreatureSpeak = new ProtocolEvent();
        public ProtocolEvent ChannelList = new ProtocolEvent();
        public ProtocolEvent OpenChannel = new ProtocolEvent();
        public ProtocolEvent OpenPrivateChat = new ProtocolEvent();
        public ProtocolEvent TextMessage = new ProtocolEvent();

        public ProtocolEvent VIPState = new ProtocolEvent();
        public ProtocolEvent VIPLogin = new ProtocolEvent();
        public ProtocolEvent VIPLogout = new ProtocolEvent();

        public ProtocolEvent WorldLight = new ProtocolEvent();
        public ProtocolEvent Effect = new ProtocolEvent();
        public ProtocolEvent AnimatedText = new ProtocolEvent();
        public ProtocolEvent ShootEffect = new ProtocolEvent();
        public ProtocolEvent SquareEffect = new ProtocolEvent();

        public ProtocolEvent MoveNorth = new ProtocolEvent();
        public ProtocolEvent MoveSouth = new ProtocolEvent();
        public ProtocolEvent MoveEast = new ProtocolEvent();
        public ProtocolEvent MoveWest = new ProtocolEvent();
        public ProtocolEvent FloorUp = new ProtocolEvent();
        public ProtocolEvent FloorDown = new ProtocolEvent();

        public ProtocolEvent MapDescription = new ProtocolEvent();
        public ProtocolEvent RefreshTile = new ProtocolEvent();
        public ProtocolEvent AddThing = new ProtocolEvent();
        public ProtocolEvent TransformThing = new ProtocolEvent();
        public ProtocolEvent RemoveThing = new ProtocolEvent();

        public ProtocolEvent RuleViolationChannel = new ProtocolEvent();
        public ProtocolEvent RuleViolationRemove = new ProtocolEvent();
        public ProtocolEvent RuleViolationCancel = new ProtocolEvent();
        public ProtocolEvent RuleViolationLock = new ProtocolEvent();

        /// <summary>
        /// Create a Protocol given a Game Data object (which contains the version.
        /// </summary>
        /// <param name="data"></param>
        public TibiaGameProtocol(TibiaGameData data)
        {
            Stream f = Assembly.GetExecutingAssembly().GetManifestResourceStream("CTC.TibiaProtocolMap.xml");
            Factory = new TibiaGamePacketParserFactory(f, data);

            AddPacketHandler("ErrorMessage", ErrorMessage);
            AddPacketHandler("MOTD", MOTD);
            AddPacketHandler("Ping", Ping);

            AddPacketHandler("CanReport", CanReport);
            AddPacketHandler("Death", Death);

            AddPacketHandler("UpdateInventory", UpdateInventory);
            AddPacketHandler("ClearInventory", ClearInventory);
            AddPacketHandler("OpenContainer", OpenContainer);
            AddPacketHandler("CloseContainer", CloseContainer);
            AddPacketHandler("ContainerAddItem", ContainerAddItem);
            AddPacketHandler("ContainerTransformItem", ContainerTransformItem);
            AddPacketHandler("ContainerRemoveItem", ContainerRemoveItem);

            AddPacketHandler("PlayerLogin", PlayerLogin);
            AddPacketHandler("UpdateStats", UpdateStats);
            AddPacketHandler("UpdateSkills", UpdateSkills);
            AddPacketHandler("PlayerIcons", PlayerIcons);
            AddPacketHandler("CancelAttack", CancelAttack);
            AddPacketHandler("CancelWalk", CancelWalk);

            AddPacketHandler("CreatureMove", CreatureMove);
            AddPacketHandler("CreatureHealth", CreatureHealth);
            AddPacketHandler("CreatureLight", CreatureLight);
            AddPacketHandler("CreatureRefresh", CreatureRefresh);
            AddPacketHandler("CreatureSpeed", CreatureSpeed);
            AddPacketHandler("CreatureSkull", CreatureSkull);
            AddPacketHandler("CreatureShield", CreatureShield);

            AddPacketHandler("CreatureSpeak", CreatureSpeak);
            AddPacketHandler("ChannelList", ChannelList);
            AddPacketHandler("OpenChannel", OpenChannel);
            AddPacketHandler("OpenPrivateChat", OpenPrivateChat);
            AddPacketHandler("TextMessage", TextMessage);

            AddPacketHandler("VIPState", VIPState);
            AddPacketHandler("VIPLogin", VIPLogin);
            AddPacketHandler("VIPLogout", VIPLogout);

            AddPacketHandler("WorldLight", WorldLight);
            AddPacketHandler("Effect", Effect);
            AddPacketHandler("AnimatedText", AnimatedText);
            AddPacketHandler("ShootEffect", ShootEffect);
            AddPacketHandler("SquareEffect", SquareEffect);

            AddPacketHandler("MoveNorth", MoveNorth);
            AddPacketHandler("MoveSouth", MoveSouth);
            AddPacketHandler("MoveEast", MoveEast);
            AddPacketHandler("MoveWest", MoveWest);
            AddPacketHandler("FloorUp", FloorUp);
            AddPacketHandler("FloorDown", FloorDown);

            AddPacketHandler("MapDescription", MapDescription);
            AddPacketHandler("RefreshTile", RefreshTile);
            AddPacketHandler("AddThing", AddThing);
            AddPacketHandler("TransformThing", TransformThing);
            AddPacketHandler("RemoveThing", RemoveThing);

            AddPacketHandler("RuleViolationChannel", RuleViolationChannel);
            AddPacketHandler("RuleViolationRemove", RuleViolationRemove);
            AddPacketHandler("RuleViolationCancel", RuleViolationCancel);
            AddPacketHandler("RuleViolationLock", RuleViolationLock);
        }

        private void AddPacketHandler(string PacketName, ProtocolEvent handler)
        {
            PacketParser pp = Factory.CreatePacketHandler(PacketName);
            PacketParsers.Add(pp.type, pp);
            PacketHandlers.Add(pp.type, handler);
        }

        public void parsePacket(NetworkMessage nmsg)
        {
            int packetType = nmsg.ReadByte();

            if (PacketParsers.ContainsKey(packetType))
            {
                PacketParser pp = PacketParsers[packetType];
                Packet p = pp.parser(nmsg);
                ProtocolEvent handler = PacketHandlers[packetType];
                if (handler != null && p != null)
                    handler.Invoke(p);
                /*
                string m = "Parsing packet 0x" + packetType.ToString("X2") + " '" + ph.name + "'";
                if (p != null)
                    m += "<pre>" + p.ToString() + "</pre>";
                Log.Debug(m);
                 */
                if (!nmsg.ReadAllData())
                    parsePacket(nmsg);
            }
            else
            {
                Log.Warning("Unknown packet type parsed 0x" + packetType.ToString("X2"), this);
            }
        }
    }
}
