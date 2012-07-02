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
        /// <summary>
        ///  A list of all items inside this container
        /// </summary>
        public List<ClientItem> Contents;

        /// <summary>
        /// The maximum amount of items that fit inside this container.
        /// </summary>
        public int MaximumVolume;

        /// <summary>
        /// If this container has a parent container (ie. should display the little
        /// parent arrow in the UI).
        /// </summary>
        public bool HasParent;

        /// <summary>
        /// The name of the container, displayed in the UI.
        /// </summary>
        public String Name;

        /// <summary>
        /// The Container ID used by the server to uniquely identify this container
        /// </summary>
        public readonly int ContainerID;

        public ClientContainer(ItemType Data, int ContainerID)
            : base(Data, 1)
        {
            this.ContainerID = ContainerID;
            MaximumVolume = 0;
            HasParent = false;
            Name = "Unknown container";
        }
    }
}
