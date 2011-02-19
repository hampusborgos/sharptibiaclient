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
    }

    public class ClientContainer : ClientItem
    {
        public ClientContainer(ItemType Data)
            : base(Data, 1)
        {
        }
    }
}
