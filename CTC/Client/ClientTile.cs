using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    public class ClientTile
    {
        public ClientTile(MapPosition Position)
        {
            this.Position = Position;
        }

        public readonly MapPosition Position;

        public ClientItem Ground = null;
        public List<ClientThing> Objects = new List<ClientThing>();

        public void Add(ClientThing Thing, bool Push = false)
        {
            if (Thing is ClientItem)
            {
                if (((ClientItem)Thing).Type.IsGround)
                {
                    Ground = (ClientItem)Thing;
                    return;
                }
            }

            if (Objects.Count >= 10)
                Objects.Remove(GetByIndex(9));

            int index = 0;
            for (index = 0; index < Objects.Count; ++index)
            {
                if (Push)
                {
                    if (Objects[index].Order >= Thing.Order)
                        break;
                }
                else if (Objects[index].Order > Thing.Order)
                    break;
            }
            Objects.Insert(index, Thing);
        }

        public bool Remove(ClientThing Thing)
        {
            if (Thing == Ground)
            {
                Ground = null;
                return true;
            }
            return Objects.Remove(Thing);
        }

        public bool Remove(int index)
        {
            return Remove(GetByIndex(index));
        }

        public void Replace(int index, ClientThing NewThing)
        {
            if (index == 0)
                Ground = (ClientItem)NewThing;
            else
                Objects[index - 1] = NewThing;
        }

        public ClientThing GetByIndex(int index)
        {
            if (index == 0)
                return Ground;
            return Objects[index - 1];
        }

        public int CreatureCount
        {
            get
            {
                int n = 0;
                foreach (ClientThing Thing in Objects)
                    if (Thing is ClientCreature)
                        ++n;
                return n;
            }
        }

        public IEnumerable<ClientThing> ObjectsByDrawOrder
        {
            get
            {
                // First items
                for (int idx = 0; idx < Objects.Count; ++idx)
                {
                    ClientThing Thing = Objects[idx];
                    if (Thing.Order == 4)
                        continue;
                    yield return Thing;
                }

                // Then creatures
                for (int idx = 0; idx < Objects.Count; ++idx)
                {
                    ClientThing Thing = Objects[idx];
                    if (Thing.Order == 4)
                        yield return Thing;
                }
            }
        }
    }
}
