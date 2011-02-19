using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{ 
    public class ClientCreature : ClientThing
    {
        public UInt32 ID;
        public int Health = 0, MaxHealth = 100;
        public int Mana = 0, MaxMana = 100;
        public int Speed = 0;

        public Direction Direction = Direction.South;

        /// <summary>
        /// Name of the creature.
        /// </summary>
        public String Name = "";

        /// <summary>
        /// If the other player is a player, is he online (for VIP?)
        /// </summary>
        public Boolean Online = false;

        public ClientOutfit Outfit = new ClientOutfit();
        public LightSource Light;
        public PartyShield Shield = PartyShield.None;
        public CreatureSkull Skull = CreatureSkull.None;

        public ClientCreature(UInt32 CreatureID)
        {
            ID = CreatureID;
        }

        override public int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public override int Order
        {
            get
            {
                return 4;
            }
        }

        public void Move(MapPosition FromPosition, MapPosition ToPosition)
        {
            if (ToPosition.X < FromPosition.X)
                Direction = Direction.West;
            else if (ToPosition.X > FromPosition.X)
                Direction = Direction.East;
            else if (ToPosition.Y < FromPosition.Y)
                Direction = Direction.North;
            else if (ToPosition.Y > FromPosition.Y)
                Direction = Direction.South;
        }
    }
}
