using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    public class ClientContainer : ClientItem
    {
        /// <summary>
        ///  A list of all items inside this container
        /// </summary>
        public List<ClientItem> Contents;

        /// <summary>
        /// The maximum amount of items that fit inside this container.
        /// </summary>
        public int MaximumVolume = 0;

        /// <summary>
        /// If this container has a parent container (ie. should display the little
        /// parent arrow in the UI).
        /// </summary>
        public bool HasParent = false;

        /// <summary>
        /// The name of the container, displayed in the UI.
        /// </summary>
        public String Name;

        /// <summary>
        /// The Container ID used by the server to uniquely identify this container
        /// </summary>
        public readonly int ContainerID;

        /// <summary>
        /// Create a client container representation
        /// </summary>
        /// <param name="Data">The item type of the container.</param>
        /// <param name="ContainerID">The server-side ID of the container.</param>
        public ClientContainer(ItemType Data, int ContainerID)
            : base(Data, 1)
        {
            this.ContainerID = ContainerID;
            // Will be set by viewport handler
            Name = "CID#" + ContainerID.ToString();
        }

        /// <summary>
        /// Unlike a plain item, a container really is aliased - the same object
        /// sits in Viewport.Containers and can be the item in an inventory slot -
        /// so it goes through the identity map.
        /// </summary>
        public override ClientThing Clone(CloneContext Context)
        {
            return Context.Get<ClientContainer>(this,
                delegate()
                {
                    return new ClientContainer(Type, ContainerID);
                },
                delegate(ClientContainer From, ClientContainer To)
                {
                    To.Subtype = From.Subtype;
                    To.Name = From.Name;
                    To.MaximumVolume = From.MaximumVolume;
                    To.HasParent = From.HasParent;

                    if (From.Contents == null)
                    {
                        To.Contents = null;
                        return;
                    }

                    To.Contents = new List<ClientItem>(From.Contents.Count);
                    foreach (ClientItem Item in From.Contents)
                        To.Contents.Add(Item == null ? null : (ClientItem)Item.Clone(Context));
                });
        }

        public ClientItem GetItem(int index)
        {
            if (index >= Contents.Count)
                return null;
            return Contents[index];
        }
    }
}
