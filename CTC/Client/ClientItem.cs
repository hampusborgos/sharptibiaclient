using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    public class ClientItem : ClientThing
    {
        public ClientItem(ItemType Type, int Subtype)
        {
            this.Type = Type;
            this.Subtype = Subtype;
        }

        public readonly ItemType Type;
        public int ID
        {
            get
            {
                return Type.ID;
            }
        }
        public int Subtype;

        public GameSprite Sprite
        {
            get
            {
                return Type.Sprite;
            }
        }

        public override int Order
        {
            get
            {
                return Type.AlwaysOnTop;
            }
        }

        /// <summary>
        /// Type is shared, never copied: it is an immutable singleton owned by
        /// TibiaGameData and reaches a live Texture2D through its GameSprite.
        /// Plain items are not aliased anywhere, so they skip the identity map.
        /// </summary>
        public override ClientThing Clone(CloneContext Context)
        {
            return new ClientItem(Type, Subtype);
        }
    }
}
