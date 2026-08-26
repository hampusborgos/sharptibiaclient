using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace CTC
{
    /// <summary>
    /// A restorable point in a recording: everything mutable in the ClientViewport
    /// plus the parser's own session state, deep copied through one CloneContext
    /// so all the aliasing between them survives.
    ///
    /// Both directions copy. Capture copies live to snapshot, so packets arriving
    /// afterwards cannot reach back and change it; Restore copies snapshot to
    /// live, so the same snapshot can be restored any number of times.
    ///
    /// A snapshot belongs to the ClientState that took it. ItemType singletons
    /// are owned per TibiaGameData, and each ClientState loads its own.
    /// </summary>
    public class ClientSnapshot
    {
        /// <summary>
        /// Position on the recording's timeline this state represents.
        /// </summary>
        public readonly TimeSpan Position;

        /// <summary>
        /// Index of the next packet to deliver, so restoring and replaying
        /// reproduces exactly the sequence that was seen the first time.
        /// </summary>
        public readonly int Cursor;

        private readonly ClientState Owner;

        private struct ChannelMark
        {
            public UInt16 ID;
            public String Name;
            public List<ClientMessage> Messages;
        }

        // Viewport state
        private readonly Dictionary<UInt32, ClientCreature> Creatures;
        private readonly Dictionary<UInt32, ClientCreature> VIPList;
        private readonly List<ClientItem> Inventory;
        private readonly Dictionary<int, ClientContainer> Containers;
        private readonly ClientPlayer Player;
        private readonly MapPosition ViewPosition;
        private readonly ClientMap Map;
        private readonly LightSource WorldLight;
        private readonly List<ChannelMark> Channels;
        private readonly UInt16 DefaultChannelID;

        // Parser state
        private readonly TibiaGamePacketParserFactory.ParserSession Session;

        private ClientSnapshot(ClientState State)
        {
            Owner = State;
            Cursor = State.Playback.Cursor;

            // The state is whatever the packets up to Cursor made it, so it is
            // valid from the last one delivered until the next one arrives. Label
            // it with the start of that window: a snapshot can then be used for
            // any target at or after its Position.
            Position = State.Playback.Recording.TimeAt(Cursor - 1);

            ClientViewport Viewport = State.Viewport;
            CloneContext Context = new CloneContext();

            // The parser first, so Player and every known creature are already in
            // the identity map before the tile walk reaches them. Either order is
            // correct - that is the whole point of the map - but this one reads
            // as what it is.
            Session = State.Protocol.CaptureSession(Context);

            Player = Viewport.Player == null
                ? null
                : (ClientPlayer)Viewport.Player.Clone(Context);

            Map = Viewport.Map.Clone(Context);

            Creatures = new Dictionary<UInt32, ClientCreature>(Viewport.Creatures.Count);
            foreach (KeyValuePair<UInt32, ClientCreature> Entry in Viewport.Creatures)
                Creatures[Entry.Key] = (ClientCreature)Entry.Value.Clone(Context);

            VIPList = new Dictionary<UInt32, ClientCreature>(Viewport.VIPList.Count);
            foreach (KeyValuePair<UInt32, ClientCreature> Entry in Viewport.VIPList)
                VIPList[Entry.Key] = (ClientCreature)Entry.Value.Clone(Context);

            Inventory = new List<ClientItem>(Viewport.Inventory.Count);
            foreach (ClientItem Item in Viewport.Inventory)
                Inventory.Add(Item == null ? null : (ClientItem)Item.Clone(Context));

            Containers = new Dictionary<int, ClientContainer>(Viewport.Containers.Count);
            foreach (KeyValuePair<int, ClientContainer> Entry in Viewport.Containers)
                Containers[Entry.Key] = (ClientContainer)Entry.Value.Clone(Context);

            ViewPosition = Viewport.ViewPosition;
            WorldLight = Viewport.WorldLight;

            // ClientMessage is never mutated after it is built, so the list is
            // copied and the messages in it are shared.
            Channels = new List<ChannelMark>(Viewport.Channels.Count);
            foreach (KeyValuePair<UInt16, ClientChannel> Entry in Viewport.Channels)
            {
                ChannelMark Mark = new ChannelMark();
                Mark.ID = Entry.Key;
                Mark.Name = Entry.Value.Name;
                Mark.Messages = new List<ClientMessage>(Entry.Value.Messages);
                Channels.Add(Mark);
            }
            DefaultChannelID = Viewport.DefaultChannel == null ? (UInt16)0 : Viewport.DefaultChannel.ID;
        }

        public static ClientSnapshot Capture(ClientState State)
        {
            return new ClientSnapshot(State);
        }

        /// <summary>
        /// Writes this state back over the live one.
        ///
        /// Everything is mutated in place rather than replaced. Every panel in the
        /// sidebar holds a reference to the ClientViewport, several hold references
        /// to objects inside it, and swapping any of them out would leave those
        /// panels pointed at a state nothing updates any more.
        /// </summary>
        /// <returns>True when the restore swapped in a different player.</returns>
        public Boolean RestoreInto(ClientState State)
        {
            if (!ReferenceEquals(State, Owner))
                throw new InvalidOperationException(
                    "Snapshot belongs to a different ClientState; its ItemTypes come " +
                    "from a different TibiaGameData.");

            ClientViewport Viewport = State.Viewport;
            CloneContext Context = new CloneContext();

            // Keep the player object itself if it is still the same player, so
            // that SkillPanel's cached ClientSkill references stay live.
            Boolean SamePlayer = Viewport.Player != null && Player != null
                && Viewport.Player.ID == Player.ID;
            if (SamePlayer)
                Context.Adopt<ClientCreature>(Player, Viewport.Player);

            State.Protocol.RestoreSession(Session, Context);

            Viewport.Player = Player == null ? null : (ClientPlayer)Player.Clone(Context);
            Viewport.ViewPosition = ViewPosition;
            Viewport.WorldLight = WorldLight;

            List<ClientTile> Tiles = new List<ClientTile>();
            foreach (ClientTile Tile in Map.AllTiles)
                Tiles.Add(Tile.Clone(Context));
            Viewport.Map.ReplaceAll(Tiles);

            Viewport.Creatures.Clear();
            foreach (KeyValuePair<UInt32, ClientCreature> Entry in Creatures)
                Viewport.Creatures[Entry.Key] = (ClientCreature)Entry.Value.Clone(Context);

            Viewport.VIPList.Clear();
            foreach (KeyValuePair<UInt32, ClientCreature> Entry in VIPList)
                Viewport.VIPList[Entry.Key] = (ClientCreature)Entry.Value.Clone(Context);

            for (int Slot = 0; Slot < Viewport.Inventory.Count; ++Slot)
                Viewport.Inventory[Slot] = Slot < Inventory.Count && Inventory[Slot] != null
                    ? (ClientItem)Inventory[Slot].Clone(Context)
                    : null;

            RestoreChannels(Viewport);
            RestoreContainers(Viewport, Context);

            Viewport.RaiseVIPStatusChanged(null);

            Boolean IdentityChanged = !SamePlayer;
            if (IdentityChanged && Viewport.Player != null)
                Viewport.RaiseLogin();
            return IdentityChanged;
        }

        /// <summary>
        /// Channels are reused by ID where they still exist, because a panel may
        /// be holding one; only the message list and name are written.
        /// </summary>
        private void RestoreChannels(ClientViewport Viewport)
        {
            HashSet<UInt16> Wanted = new HashSet<UInt16>();
            foreach (ChannelMark Mark in Channels)
                Wanted.Add(Mark.ID);

            List<UInt16> Gone = new List<UInt16>();
            foreach (UInt16 ID in Viewport.Channels.Keys)
                if (!Wanted.Contains(ID))
                    Gone.Add(ID);
            foreach (UInt16 ID in Gone)
                Viewport.Channels.Remove(ID);

            foreach (ChannelMark Mark in Channels)
            {
                ClientChannel Channel;
                if (!Viewport.Channels.TryGetValue(Mark.ID, out Channel))
                {
                    Channel = new ClientChannel(Mark.ID, Mark.Name);
                    Viewport.Channels[Mark.ID] = Channel;
                }

                Channel.Name = Mark.Name;
                Channel.Messages.Clear();
                Channel.Messages.AddRange(Mark.Messages);
            }

            ClientChannel Default;
            if (Viewport.Channels.TryGetValue(DefaultChannelID, out Default))
                Viewport.DefaultChannel = Default;
        }

        /// <summary>
        /// Containers, in the order the panels need to hear about it.
        ///
        /// The diff is taken against the keys that were open *before* the
        /// dictionary is rewritten, and the opens come last, because
        /// GameDesktop builds a ContainerPanel in response to OpenContainer and
        /// that panel's constructor reads Viewport.Containers[ID] straight away.
        /// Firing an open for a container that already has a panel would leave two
        /// of them subscribed to the same ID.
        /// </summary>
        private void RestoreContainers(ClientViewport Viewport, CloneContext Context)
        {
            List<int> Before = new List<int>(Viewport.Containers.Keys);
            Dictionary<int, ClientContainer> WasOpen =
                new Dictionary<int, ClientContainer>(Viewport.Containers);

            Viewport.Containers.Clear();
            foreach (KeyValuePair<int, ClientContainer> Entry in Containers)
                Viewport.Containers[Entry.Key] = (ClientContainer)Entry.Value.Clone(Context);

            foreach (int ID in Before)
                if (!Viewport.Containers.ContainsKey(ID))
                    Viewport.RaiseCloseContainer(WasOpen[ID]);

            foreach (int ID in Before)
                if (Viewport.Containers.ContainsKey(ID))
                    Viewport.RaiseUpdateContainer(Viewport.Containers[ID]);

            foreach (KeyValuePair<int, ClientContainer> Entry in Viewport.Containers)
                if (!WasOpen.ContainsKey(Entry.Key))
                    Viewport.RaiseOpenContainer(Entry.Value);
        }
    }
}
