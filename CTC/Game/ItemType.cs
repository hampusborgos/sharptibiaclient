using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    public class ItemType
    {
        public static ItemType NullType = new ItemType(0, null);

        public readonly UInt16 ID;
        public readonly GameSprite Sprite;

        public int WalkSpeed = 100;
        public Boolean DontFallThrough = false;
        public Boolean IsGround = false;
        /// <summary>
        /// 0: Ground
        /// 1-3: TopItems
        /// 4: Creatures
        /// 5: DownItems
        /// </summary>
        public int AlwaysOnTop = 5;
        public Boolean CanSeeThrough = false;
        public Boolean CanPickup = false;
        public Boolean IsContainer = false;
        public Boolean IsStackable = false;
        public Boolean IsUseableWith = false;
        public Boolean IsLadder = false;
        public Boolean IsWriteable = false;
        public Boolean IsReadable = false;
        public int MaxTextLength = 0;
        public Boolean IsFluidContainer = false;
        public Boolean IsSplash = false;
        public Boolean BlockProjectiles = false;
        public Boolean BlockPathfinding = false;
        public Boolean IsSolid = false;
        public Boolean Immoveable = false;
        public int LightLevel = 0;
        public int LightColor = 0;
        public Boolean MakesLight
        {
            get { return LightLevel > 0; }
        }
        public Boolean HasHeight = false;
        public Boolean StaticCorpse = false;
        public Boolean CanTurn = false;
        public Boolean IsFloorChanger = false;
        public Boolean IsWall = false;

        public ItemType(UInt16 id, GameSprite Sprite)
        {
            this.ID = id;
            this.Sprite = Sprite;
        }
    }
}
