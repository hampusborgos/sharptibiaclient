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
        public float HealthPercent
        {
            get
            {
                return 1f * Health / MaxHealth;
            }
        }
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

        /// <summary>
        /// Creatures are aliased three ways - the tile they stand on,
        /// ClientViewport.Creatures, and the parser's KnownCreatures - so they
        /// always go through the identity map.
        /// </summary>
        public override ClientThing Clone(CloneContext Context)
        {
            return Context.Get<ClientCreature>(this,
                delegate()
                {
                    return Allocate();
                },
                delegate(ClientCreature From, ClientCreature To)
                {
                    From.CopyInto(To);
                });
        }

        /// <summary>
        /// An empty creature of this creature's own type, for Clone to fill in.
        /// </summary>
        protected virtual ClientCreature Allocate()
        {
            return new ClientCreature(ID);
        }

        /// <summary>
        /// Writes every mutable field into Target.
        ///
        /// This lives directly under the fields on purpose: a field added above
        /// and not copied here is a silent desync that only shows up after a
        /// seek, and nothing in the compiler will catch it.
        /// </summary>
        protected virtual void CopyInto(ClientCreature Target)
        {
            Target.ID = ID;
            Target.Health = Health;
            Target.MaxHealth = MaxHealth;
            Target.Mana = Mana;
            Target.MaxMana = MaxMana;
            Target.Speed = Speed;
            Target.Direction = Direction;
            Target.Name = Name;
            Target.Online = Online;
            Target.Outfit = Outfit == null ? null : Outfit.Clone();
            Target.Light = Light;
            Target.Shield = Shield;
            Target.Skull = Skull;
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
