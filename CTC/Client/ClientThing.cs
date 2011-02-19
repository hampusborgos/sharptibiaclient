using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    /// <summary>
    /// A "Thing" to the client, that is, either a creature or an item, which appears on the map or in the inventory.
    /// </summary>
    public class ClientThing
    {
        public ClientThing()
        {
        }

        public virtual int Order
        {
            get
            {
                return 0;
            }
        }
    }
}
