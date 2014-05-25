using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    public struct MapPosition
    {
        public int X, Y, Z;

        public MapPosition(int x, int y, int z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public override string ToString()
        {
            return "{ x = " + X + ", y = " + Y + ", z = " + Z + "}";
        }

        public override int GetHashCode()
        {
            return (X ^ Y ^ Z).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is MapPosition)
            {
                MapPosition o = (MapPosition)obj;
                return X == o.X && Y == o.Y && Z == o.Z;
            }
            return false;
        }
    }
}
