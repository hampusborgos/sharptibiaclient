using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    public class ClientMap
    {
        private Dictionary<MapPosition, ClientTile> Tiles = new Dictionary<MapPosition, ClientTile>();
        
        public ClientMap()
        {
        }

        /// <summary>
        /// Removes all tiles currently in memory
        /// </summary>
        public void Clear()
        {
            Tiles.Clear();
        }

        /// <summary>
        /// Removes all tiles outside the viewport.
        /// </summary>
        /// <param name="Center">Center of the Viewport</param>
        public void Clear(MapPosition Center)
        {
            List<MapPosition> ToRemove = new List<MapPosition>();

            foreach (MapPosition Position in Tiles.Keys)
            {
                int ZAdjustment = Center.Z - Position.Z;
                if (Position.X < Center.X - 9 + ZAdjustment ||
                    Position.X > Center.X + 9 + ZAdjustment ||
                    Position.Y < Center.Y - 7 + ZAdjustment ||
                    Position.Y > Center.Y + 7 + ZAdjustment)
                {
                    ToRemove.Add(Position);
                }
            }

            foreach (MapPosition Position in ToRemove)
                Tiles.Remove(Position);
        }

        public ClientTile this [MapPosition Position]
        {
            get
            {
                ClientTile Tile = null;
                if (Tiles.TryGetValue(Position, out Tile))
                    return Tile;
                return null;
            }
            set
            {
                Tiles[value.Position] = value;
            }
        }
    }
}
