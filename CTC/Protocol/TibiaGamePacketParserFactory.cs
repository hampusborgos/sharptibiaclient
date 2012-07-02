using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace CTC
{

    public class Packet
    {
        private Dictionary<string, object> Data = new Dictionary<string, object>();

        public Packet(string name)
        {
            Data["PacketType"] = name;
        }

        public Boolean Has(string key)
        {
            return Data.ContainsKey(key);
        }

        public object this[string key]
        {
            get {
                if (Data.ContainsKey(key))
                    return Data[key];
                return null;
            }
            set { Data[key] = value; }
        }

        override public string ToString()
        {
            string desc = "";
            foreach (KeyValuePair<string, object> p in Data)
                if (p.Value is Packet)
                    desc += p.Key + " = {<pre>" + p.Value.ToString() + "</pre>}\n";
                else
                    desc += p.Key + " = " + p.Value.ToString() + "\n";
            return desc.Remove(desc.Length - 1);
        }
    }

    /// <summary>
    /// A template for a function that takes a NetworkMessage and produces an object as a result (to be handled by the protocol)
    /// </summary>
    /// <param name="nmsg">The message to parse, there may be data left over after the initial parsing.</param>
    /// <returns></returns>
    public delegate Packet GamePacketParser(NetworkMessage nmsg);

    public class PacketParser
    {
        public int type;
        public string name;
        public GamePacketParser parser;
    }

    class CreatureTurn2Exception : ProtocolException
    {
        public CreatureTurn2Exception()
            : base("CreatureTurn2 not expected in this packet")
        {
        }
    }

    public class TibiaGamePacketParserFactory
    {
        private TibiaGameData GameData;
        private List<PacketParser> AllHandlers = new List<PacketParser>();

        /// <summary>
        /// We need to track the current map position in the protocol, since some packets depend on where we are.
        /// </summary>
        private MapPosition CurrentPosition = new MapPosition(0, 0, 0);

        /// <summary>
        /// We need to track the player so we can handle it correctly
        /// </summary>
        private ClientPlayer Player;

        private Dictionary<uint, ClientCreature> KnownCreatures = new Dictionary<uint, ClientCreature>();

        /// <summary>
        /// Loads all mapped handlers from the file.
        /// </summary>
        /// <param name="File"></param>
        public TibiaGamePacketParserFactory(Stream File, TibiaGameData data)
        {
            GameData = data;
            try
            {
                System.Xml.XmlDocument XMLData = new System.Xml.XmlDocument();
                XMLData.Load(File);

                System.Xml.XmlNodeList Versions = XMLData.GetElementsByTagName("TibiaVersion");
                foreach (System.Xml.XmlNode versionNode in Versions)
                {
                    int version = int.Parse(versionNode.Attributes["id"].InnerText);
                    if (version != GameData.Version)
                        continue;

                    foreach (System.Xml.XmlNode packetNode in versionNode.ChildNodes)
                    {
                        if (packetNode.LocalName == "Packet")
                        {
                            PacketParser ph = new PacketParser();
                            string n = packetNode.Attributes["type"].InnerText;
                            if (!int.TryParse(packetNode.Attributes["type"].InnerText.Substring(2), 
                                    System.Globalization.NumberStyles.HexNumber,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    out ph.type))
                                continue;

                            ph.name = packetNode.Attributes["parser"].InnerText;
                            ph.parser = GetPacketParser(ph.name);
                            AllHandlers.Add(ph);
                        }
                    }
                }
            }
            catch (System.Xml.XmlException xmlException)
            {
                Log.Error("XML error when parsing internal version table: " + xmlException.Message);
            }
            catch (System.ArgumentException argException)
            {
                Log.Error("Conversion error when parsing internal version table: " + argException.Message);
            }
        }

        /// <summary>
        /// Given a packet type and the protocol version, returns the handler that will parse the packet.
        /// This function is quite slow, so the value should be cached.
        /// </summary>
        /// <param name="packetName">Name of packet to parse, for example "Login"</param>
        /// <returns></returns>
        public PacketParser CreatePacketHandler(String packetName)
        {
            foreach (PacketParser pp in AllHandlers)
            {
                if (packetName == pp.name)
                    return pp;
            }

            throw new System.ArgumentException("Unknown packet parser " + packetName + ".");
        }

        /// <summary>
        /// Internal use only, fetches the correct handler for a given handler name. One packet name may map to multiple handler names.
        /// </summary>
        /// <param name="parserName">The packet handler to fetch</param>
        /// <returns></returns>
        private GamePacketParser GetPacketParser(String parserName)
        {
            if (parserName == "ErrorMessage")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    props["Message"] = nmsg.ReadString();
                    return props;
                };
            else if (parserName == "MOTD")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    props["Message"] = nmsg.ReadString();
                    return props;
                };
            else if (parserName == "Ping")
                return delegate(NetworkMessage nmsg)
                {
                    return new Packet(parserName);
                };

            else if (parserName == "PlayerLogin")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    Player = new ClientPlayer(nmsg.ReadU32());
                    props["DrawSpeed"] = nmsg.ReadU16();
                    props["CanReportBugs"] = nmsg.ReadByte() != 0;
                    props["Player"] = Player;
                    KnownCreatures[Player.ID] = Player;

                    return props;
                };
            else if (parserName == "MapDescription")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    CurrentPosition = ReadMapPosition(nmsg);
                    props["Center"] = CurrentPosition;
                    props["Tiles"] = ReadMapDescription(nmsg, CurrentPosition.X - 8, CurrentPosition.Y - 6, CurrentPosition.Z, 18, 14);
                    return props;
                };

            else if (parserName == "UpdateInventory")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    props["Slot"] = (int)nmsg.ReadByte();
                    props["Thing"] = ReadThing(nmsg);
                    return props;
                };
            else if (parserName == "ClearInventory")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    props["Slot"] = (int)nmsg.ReadByte();
                    return props;
                };
            else if (parserName == "OpenContainer")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    props["ContainerID"] = (int)nmsg.ReadByte();
                    int ClientID = nmsg.ReadU16();
                    ItemType it = GameData.GetItemType(ClientID);
                    if (it == null)
                    {
                        Log.Warning("OpenContainer contains unrecognized item type (" + ClientID.ToString() + ").", this);
                        it = ItemType.NullType;
                    }
                    props["Thing"] = new ClientContainer(it);
                    props["Name"] = nmsg.ReadString();
                    props["Volume"] = (int)nmsg.ReadByte();
                    props["IsChild"] = nmsg.ReadByte() != 0;
                    int ItemCount = nmsg.ReadByte();
                    props["ItemCount"] = (int)ItemCount;
                    List<ClientItem> contents = new List<ClientItem>();
                    for (int i = 0; i < ItemCount; ++i)
                        contents.Add((ClientItem)ReadThing(nmsg));
                    props["Contents"] = contents;
                    return props;
                };
            else if (parserName == "CloseContainer")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    props["ContainerID"] = (int)nmsg.ReadByte();
                    return props;
                };
            else if (parserName == "ContainerAddItem")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    props["ContainerID"] = (int)nmsg.ReadByte();
                    props["Item"] = ReadItem(nmsg);
                    return props;
                };
            else if (parserName == "ContainerTransformItem")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    props["ContainerID"] = (int)nmsg.ReadByte();
                    props["Slot"] = (int)nmsg.ReadByte();
                    props["Item"] = ReadItem(nmsg);
                    return props;
                };
            else if (parserName == "ContainerRemoveItem")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    props["ContainerID"] = (int)nmsg.ReadByte();
                    props["Slot"] = nmsg.ReadByte();
                    return props;
                };

            else if (parserName == "UpdateStats")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    props["Health"] = (int)nmsg.ReadU16();
                    props["MaxHealth"] = (int)nmsg.ReadU16();
                    props["Capacity"] = (int)nmsg.ReadU16();
                    props["Experience"] = (int)nmsg.ReadU32();
                    props["Level"] = (int)nmsg.ReadByte();
                    props["LevelPercent"] = (int)nmsg.ReadByte();
                    props["Mana"] = (int)nmsg.ReadU16();
                    props["MaxMana"] = (int)nmsg.ReadU16();
                    props["MagicLevel"] = (int)nmsg.ReadByte();
                    props["MagicLevelPercent"] = (int)nmsg.ReadByte();
                    // TODO: Stamina for some versions
                    //int soul = nmsg.ReadByte();
                    //int staminaMinutes = nmsg.ReadU16();

                    return props;
                };
            else if (parserName == "UpdateSkills")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    props["Fist"] = (int)nmsg.ReadByte();
                    props["FistPercent"] = (int)nmsg.ReadByte();
                    props["Club"] = (int)nmsg.ReadByte();
                    props["ClubPercent"] = (int)nmsg.ReadByte();
                    props["Sword"] = (int)nmsg.ReadByte();
                    props["SwordPercent"] = (int)nmsg.ReadByte();
                    props["Axe"] = (int)nmsg.ReadByte();
                    props["AxePercent"] = (int)nmsg.ReadByte();
                    props["Dist"] = (int)nmsg.ReadByte();
                    props["DistPercent"] = (int)nmsg.ReadByte();
                    props["Shield"] = (int)nmsg.ReadByte();
                    props["ShieldPercent"] = (int)nmsg.ReadByte();
                    props["Fish"] = (int)nmsg.ReadByte();
                    props["FishPercent"] = (int)nmsg.ReadByte();

                    return props;
                };
            else if (parserName == "PlayerIcons")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    props["ConditionState"] = (ConditionState)nmsg.ReadByte();
                    return props;
                };
            else if (parserName == "CancelAttack")
                return delegate(NetworkMessage nmsg)
                {
                    return new Packet(parserName);
                };
            else if (parserName == "CancelWalk")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    props["Direction"] = (Direction)nmsg.ReadByte();
                    return props;
                };
            else if (parserName == "Death")
                return delegate(NetworkMessage nmsg)
                {
                    return new Packet(parserName);
                };
            else if (parserName == "CanReport")
                return delegate(NetworkMessage nmsg)
                {
                    return new Packet(parserName);
                };
            else if (parserName == "MoveNorth")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    CurrentPosition.Y--;
                    props["Center"] = CurrentPosition;
                    props["Tiles"] = ReadMapDescription(nmsg, CurrentPosition.X - 8, CurrentPosition.Y - 6, CurrentPosition.Z, 18, 1);
                    return props;
                };
            else if (parserName == "MoveSouth")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    CurrentPosition.Y++;
                    props["Center"] = CurrentPosition;
                    props["Tiles"] = ReadMapDescription(nmsg, CurrentPosition.X - 8, CurrentPosition.Y + 7, CurrentPosition.Z, 18, 1);
                    return props;
                };
            else if (parserName == "MoveWest")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    CurrentPosition.X--;
                    props["Center"] = CurrentPosition;
                    props["Tiles"] = ReadMapDescription(nmsg, CurrentPosition.X - 8, CurrentPosition.Y - 6, CurrentPosition.Z, 1, 14);
                    return props;
                };
            else if (parserName == "MoveEast")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    CurrentPosition.X++;
                    props["Center"] = CurrentPosition;
                    props["Tiles"] = ReadMapDescription(nmsg, CurrentPosition.X + 9, CurrentPosition.Y - 6, CurrentPosition.Z, 1, 14);
                    return props;
                };
            else if (parserName == "FloorUp")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    List<ClientTile> Tiles = new List<ClientTile>();

                    CurrentPosition.Z--;
                    if (CurrentPosition.Z == 7)
                    {
                        for (int i = 5; i >= 0; i--)
                        {
                            Tiles.AddRange(ReadFloorDescription(nmsg, CurrentPosition.X - 8, CurrentPosition.Y - 6, i, 18, 14, 8 - i));
                        }
                    }
                    else if (CurrentPosition.Z > 7)
                    {
                        Tiles.AddRange(ReadFloorDescription(nmsg, CurrentPosition.X - 8, CurrentPosition.Y - 6, CurrentPosition.Z - 2, 18, 14, 3));
                    }
                    CurrentPosition.X++;
                    CurrentPosition.Y++;

                    props["Tiles"] = Tiles;
                    props["Center"] = CurrentPosition;
                    return props;
                };
            else if (parserName == "FloorDown")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    List<ClientTile> Tiles = new List<ClientTile>();

                    CurrentPosition.Z++;
                    if (CurrentPosition.Z == 8)
                    {
                        int j = -1;
                        for (int i = CurrentPosition.Z; i < CurrentPosition.Z + 3; ++i, --j)
                        {
                            Tiles.AddRange(ReadFloorDescription(nmsg, CurrentPosition.X - 8, CurrentPosition.Y - 6, i, 18, 14, j));
                        }
                    }
                    else if (CurrentPosition.Z > 8 && CurrentPosition.Z < 14)
                    {
                        Tiles.AddRange(ReadFloorDescription(nmsg, CurrentPosition.X - 8, CurrentPosition.Y - 6, CurrentPosition.Z + 2, 18, 14, -3));
                    }
                    CurrentPosition.X--;
                    CurrentPosition.Y--;

                    props["Tiles"] = Tiles;
                    props["Center"] = CurrentPosition;
                    return props;
                };

            else if (parserName == "RefreshTile")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    MapPosition pos = ReadMapPosition(nmsg);
                    props["Position"] = pos;
                    props["Clear"] = false;
                    if (nmsg.PeekU16() == 0xFF01)
                    {
                        nmsg.ReadU16();
                        props["Clear"] = true;
                        return props;
                    }
                    else
                    {
                        props["NewTile"] = ReadTileDescription(nmsg, pos);
                        // Skip extra bytes after data (it signifies the end of data)
                        nmsg.ReadU16();
                        return props;
                    }
                };
            else if (parserName == "AddThing")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    props["Position"] = ReadMapPosition(nmsg);
                    props["Thing"] = ReadThing(nmsg);
                    props["Push"] = true;
                    // TODO: >= 8.53, push thing should be false
                    return props;
                };
            else if (parserName == "TransformThing")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    props["Position"] = ReadMapPosition(nmsg);
                    props["StackIndex"] = (int)nmsg.ReadByte();

                    if (nmsg.PeekU16() == 0x63)
                    {
                        nmsg.Skip(2);
                        // CreatureTurn2
                        props["Thing"] = KnownCreatures[nmsg.ReadU32()];
                        props["Direction"] = (Direction)nmsg.ReadByte();
                    }
                    else
                    {
                        props["Thing"] = ReadThing(nmsg);
                    }
                    return props;
                };
            else if (parserName == "RemoveThing")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    props["Position"] = ReadMapPosition(nmsg);
                    props["StackIndex"] = (int)nmsg.ReadByte();

                    return props;
                };
            else if (parserName == "CreatureMove")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    props["OldPosition"] = ReadMapPosition(nmsg);
                    props["OldStackIndex"] = (int)nmsg.ReadByte();
                    props["Position"] = ReadMapPosition(nmsg);

                    if ((int)props["OldStackIndex"] > 9)
                        Log.Warning("CreatureMove - Old stack pos out of range.", this);

                    props["Push"] = true;
                    // TODO: >= 8.53, pushThing = false
                    return props;
                };

            else if (parserName == "WorldLight")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    props["Level"] = (int)nmsg.ReadByte();
                    props["Color"] = (int)nmsg.ReadByte();
                    return props;
                };
            else if (parserName == "Effect")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    props["Position"] = ReadMapPosition(nmsg);
                    props["Effect"] = 1 + (int)nmsg.ReadByte();
                    return props;
                };
            else if (parserName == "AnimatedText")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    props["Position"] = ReadMapPosition(nmsg);
                    props["Color"] = (int)nmsg.ReadByte();
                    props["Text"] = nmsg.ReadString();
                    return props;
                };
            else if (parserName == "ShootEffect")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    props["From"] = ReadMapPosition(nmsg);
                    props["To"] = ReadMapPosition(nmsg);
                    props["Effect"] = (int)nmsg.ReadByte();
                    return props;
                };
            else if (parserName == "SquareEffect")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    props["Creature"] = KnownCreatures[nmsg.ReadU32()];
                    props["Color"] = nmsg.ReadByte();
                    return props;
                };

            else if (parserName == "CreatureHealth")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    props["Creature"] = KnownCreatures[nmsg.ReadU32()];
                    props["HealthPercent"] = nmsg.ReadByte();
                    return props;
                };
            else if (parserName == "CreatureLight")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    props["Creature"] = KnownCreatures[nmsg.ReadU32()];
                    props["Level"] = (int)nmsg.ReadByte();
                    props["Color"] = (int)nmsg.ReadByte();
                    return props;
                };
            else if (parserName == "CreatureRefresh")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    props["Creature"] = KnownCreatures[nmsg.ReadU32()];
                    return props;
                };
            else if (parserName == "CreatureTurn")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    props["Creature"] = KnownCreatures[nmsg.ReadU32()];
                    props["Direction"] = (Direction)nmsg.ReadByte();
                    return props;
                };
            else if (parserName == "CreatureSpeed")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    props["Creature"] = KnownCreatures[nmsg.ReadU32()];
                    props["Speed"] = (int)nmsg.ReadU16();
                    return props;
                };
            else if (parserName == "CreatureSkull")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    props["Creature"] = KnownCreatures[nmsg.ReadU32()];
                    props["Skull"] = (CreatureSkull)nmsg.ReadByte();
                    return props;
                };
            else if (parserName == "CreatureShield")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    props["Creature"] = KnownCreatures[nmsg.ReadU32()];
                    props["PartyShield"] = (PartyShield)nmsg.ReadByte();
                    return props;
                };

            else if (parserName == "CreatureSpeak")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    ClientMessage Message = new ClientMessage();

                    //props["Creature"] = KnownCreatures[nmsg.ReadU32()];
                    Message.Speaker = nmsg.ReadString();
                    //props["Level"] = nmsg.ReadU16();
                    Message.Type = (MessageType)nmsg.ReadByte();

                    switch (Message.Type)
                    {
                        case MessageType.Say:
                        case MessageType.Whisper:
                        case MessageType.Yell:
                        case MessageType.MonsterSay:
                        case MessageType.MonsterYell:
                        case MessageType.PrivateNPCToPlayer:
                        case MessageType.PrivatePlayerToNPC:
                            {
                                props["Position"] = ReadMapPosition(nmsg);
                                Message.Text = nmsg.ReadString();
                                break;
                            }
                        case MessageType.ChannelAnonymousRed:
                        case MessageType.ChannelOrange:
                        case MessageType.ChannelRed:
                        case MessageType.ChannelYellow:
                            {
                                props["ChannelID"] = nmsg.ReadU16();
                                Message.Text = nmsg.ReadString();
                                break;
                            }
                        case MessageType.Private:
                        case MessageType.Broadcast:
                        case MessageType.PrivateRed:
                            {
                                Message.Text = nmsg.ReadString();
                                break;
                            }
                        case MessageType.ReportRuleViolation:
                            {
                                Message.Time = Common.UnixTime(nmsg.ReadU32()); // time
                                Message.Text = nmsg.ReadString();
                                break;
                            }
                        case MessageType.RuleViolationGameMaster:
                        case MessageType.RuleViolationPlayer:
                            {
                                Message.Text = nmsg.ReadString();
                                break;
                            }
                        default:
                            Log.Warning("Unknown speak class " + (int)props["MessageType"], this);
                            return null;
                    }
                    props["Message"] = Message;
                    return props;
                };
            else if (parserName == "ChannelList")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    int count = nmsg.ReadByte();
                    props["ChannelCount"] = count;
                    while (count > 0)
                    {
                        int channelID = nmsg.ReadU16();
                        string channelName = nmsg.ReadString();
                        --count;
                    }
                    return null;
                };
            else if (parserName == "OpenChannel")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    props["ChannelID"] = nmsg.ReadU16();
                    props["ChannelName"] = nmsg.ReadString();
                    return props;
                };
            else if (parserName == "OpenPrivateChat")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    props["PlayerName"] = nmsg.ReadString();
                    return props;
                };
            else if (parserName == "TextMessage")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    MessageType Type = (MessageType)nmsg.ReadByte();
                    String Text = nmsg.ReadString();
                    props["Message"] = new ClientMessage(Type, DateTime.Now, Text);
                    return props;
                };

            else if (parserName == "VIPState")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);

                    props["CreatureID"] = nmsg.ReadU32();
                    props["Name"] = nmsg.ReadString();
                    props["Online"] = nmsg.ReadByte() != 0;
                    return props;
                };
            else if (parserName == "VIPLogin")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    props["CreatureID"] = nmsg.ReadU32();
                    return props;
                };
            else if (parserName == "VIPLogout")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    props["CreatureID"] = nmsg.ReadU32();
                    return props;
                };
            else if (parserName == "RuleViolationChannel")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    return props;
                };
            else if (parserName == "RuleViolationRemove")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    return props;
                };
            else if (parserName == "RuleViolationCancel")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    return props;
                };
            else if (parserName == "RuleViolationLock")
                return delegate(NetworkMessage nmsg)
                {
                    Packet props = new Packet(parserName);
                    return props;
                };



            throw new System.ArgumentException("Unknown packet handler.");
        }

        /// <summary>
        /// Reads a position (x, y, z) from the message
        /// </summary>
        /// <param name="nmsg">The message to read from</param>
        /// <returns></returns>
        private MapPosition ReadMapPosition(NetworkMessage nmsg)
        {
            int x = nmsg.ReadU16();
            int y = nmsg.ReadU16();
            int z = nmsg.ReadByte();
            return new MapPosition(x, y, z);
        }

        /// <summary>
        /// Reads a map description (ie. a list of tiles) from the message.
        /// </summary>
        /// <param name="nmsg">The message to read from.</param>
        /// <param name="startX"></param>
        /// <param name="startY"></param>
        /// <param name="z"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private object ReadMapDescription(NetworkMessage nmsg, int startX, int startY, int z, int width, int height)
        {
            int startZ = z, endZ = 0, stepZ = 0;
            if (startZ > 7)
            {
                startZ = z - 2;
                endZ = Math.Min(16, z + 2);
                stepZ = 1;
            }
            else
            {
                startZ = 7;
                endZ = 0;
                stepZ = -1;
            }

            int skipTiles = 0;
            List<ClientTile> tiles = new List<ClientTile>();
            for (int curZ = startZ; curZ != endZ + stepZ; curZ += stepZ)
                tiles.AddRange(ReadFloorDescription(nmsg, skipTiles, out skipTiles, startX, startY, curZ, width, height, z - curZ));

            return tiles;
        }

        private List<ClientTile> ReadFloorDescription(NetworkMessage nmsg, int startX, int startY, int Z, int width, int height, int offset)
        {
            int s = 0;
            return ReadFloorDescription(nmsg, s, out s, startX, startY, Z, width, height, offset);
        }

        private List<ClientTile> ReadFloorDescription(NetworkMessage nmsg, int skipTiles, out int skipTilesOut, int startX, int startY, int Z, int width, int height, int offset)
        {
            List<ClientTile> tiles = new List<ClientTile>();
            for (int x = 0; x < width; ++x)
            {
                for (int y = 0; y < height; ++y)
                {
                    if (skipTiles > 0)
                        skipTiles--;
                    else
                    {
                        MapPosition pos = new MapPosition(startX + x + offset, startY + y + offset, Z);
                        ClientTile Tile = ReadTileDescription(nmsg, pos);
                        skipTiles = nmsg.ReadByte();
                        if (nmsg.ReadByte() != 0xFF)
                            Log.Warning("Server did not follow Tile skip by 0xFF");
                        tiles.Add(Tile);
                    }
                }
            }

            skipTilesOut = skipTiles;
            return tiles;
        }

        private ClientTile ReadTileDescription(NetworkMessage nmsg, MapPosition pos)
        {
            ClientTile Tile = new ClientTile(pos);
            int nStuff = 0;
            while (nStuff < 10)
            {
                if (nmsg.PeekU16() >= 0xFF00)
                {
                    return Tile;
                }
                else
                {
                    Tile.Add(ReadThing(nmsg));
                }
                ++nStuff;
            }

            return Tile;
        }

        private ClientThing ReadThing(NetworkMessage nmsg)
        {
            UInt16 clientID = nmsg.ReadU16();
            if (clientID == 0x62)
            {
                // Known creature
                //Packet props = new Packet("KnownCreature");
                UInt32 creatureID = nmsg.ReadU32();
                //props["Creature"] = KnownCreatures[creatureID];

                return ReadCreature(nmsg, creatureID);
            }
            else if (clientID == 0x61)
            {
                // New creature
                //Packet props = new Packet("NewCreature");

                // Creature ID to kill
                UInt32 ForgetID = nmsg.ReadU32();
                UInt32 creatureID = nmsg.ReadU32();
                if (ForgetID != 0)
                    KnownCreatures.Remove(ForgetID);

                //props["ForgetCreatureID"] = ForgetID;
                String name = nmsg.ReadString();

                ClientCreature creature = ReadCreature(nmsg, creatureID);
                //props["Creature"] = creature;
                creature.Name = name;

                return creature;
            }
            else if (clientID == 0x63)
            {
                throw new CreatureTurn2Exception();
            }
            else
            {
                return ReadItem(nmsg, clientID);
            }
        }

        /// <summary>
        /// Read an Item and returns it.
        /// </summary>
        /// <param name="nmsg">Packet to read from.</param>
        /// <param name="clientID">If not 0, the function will not read client ID from the packet but use this one instead.</param>
        /// <returns></returns>
        private ClientItem ReadItem(NetworkMessage nmsg, UInt16 clientID = 0)
        {
            if (clientID == 0)
                clientID = nmsg.ReadU16();
            int subtype = -1;
            ItemType it = GameData.GetItemType(clientID);
            if (it == null)
            {
                Log.Warning("Item packet contains unrecognizabe item (" + clientID + ")");
                it = ItemType.NullType;
            }

            if (it.IsStackable || it.IsFluidContainer || it.IsSplash)
                subtype = nmsg.ReadByte();

            return new ClientItem(it, subtype);
        }

        /// <summary>
        /// Reads a Creature and returns it. This includes Outfit & Light etc.
        /// </summary>
        /// <param name="nmsg">Message to read from.</param>
        /// <param name="props">The read properties will be inserted into this property object.</param>
        /// <returns></returns>
        private ClientCreature ReadCreature(NetworkMessage nmsg, UInt32 CreatureID)
        {
            ClientCreature Creature = null;
            if (CreatureID == Player.ID)
                Creature = Player;
            else if (!KnownCreatures.TryGetValue(CreatureID, out Creature))
            {
                Creature = new ClientCreature(CreatureID);
                KnownCreatures.Add(CreatureID, Creature);
            }
            else
            {
                ;
            }

            Creature.Health = nmsg.ReadByte();
            Creature.Direction = (Direction)nmsg.ReadByte();

            // TODO: This is U16 for later versions
            Creature.Outfit.LookType = (int)nmsg.ReadByte();
            if (Creature.Outfit.LookType == 0)
            {
                // looktypeEx
                Creature.Outfit.LookItem = nmsg.ReadU16();
            }
            else
            {
                Creature.Outfit.LookHead = nmsg.ReadByte();
                Creature.Outfit.LookBody = nmsg.ReadByte();
                Creature.Outfit.LookLegs = nmsg.ReadByte();
                Creature.Outfit.LookFeet = nmsg.ReadByte();
            }

            Creature.Light.Level = nmsg.ReadByte();
            Creature.Light.Color = nmsg.ReadByte();

            Creature.Speed = nmsg.ReadU16();

            Creature.Skull = (CreatureSkull)nmsg.ReadByte();
            Creature.Shield = (PartyShield)nmsg.ReadByte();

            return Creature;
        }
    }
}
