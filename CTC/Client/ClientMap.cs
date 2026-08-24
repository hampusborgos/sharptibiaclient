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

        /// <summary>
        /// Every tile currently in memory. Only ever a viewport-sized working set:
        /// Clear(Center) drops everything outside a 19x15 window on each map
        /// description, which is what makes a snapshot of the map cheap.
        /// </summary>
        public IEnumerable<ClientTile> AllTiles
        {
            get { return Tiles.Values; }
        }

        public int TileCount
        {
            get { return Tiles.Count; }
        }

        public ClientMap Clone(CloneContext Context)
        {
            ClientMap Copy = new ClientMap();
            foreach (ClientTile Tile in Tiles.Values)
                Copy.Tiles[Tile.Position] = Tile.Clone(Context);
            return Copy;
        }

        /// <summary>
        /// Replaces the contents in place, so that anything holding this ClientMap
        /// keeps working across a restore.
        /// </summary>
        public void ReplaceAll(IEnumerable<ClientTile> NewTiles)
        {
            Tiles.Clear();
            foreach (ClientTile Tile in NewTiles)
                Tiles[Tile.Position] = Tile;
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
