using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    public class MagicEffect : GameEffect
    {
        public readonly int ID;
        public readonly GameSprite Sprite;
        public int Frame;

        public MagicEffect(TibiaGameData GameData, int ID)
        {
            this.ID = ID;
            Sprite = GameData.GetEffectSprite(ID);
            this.Duration = Sprite.AnimationLength / 10.0;
        }

        public override void  Update(GameTime Time)
        {
 	        base.Update(Time);

            Frame = (int)(Elapsed * 10);
            if (Frame >= Sprite.AnimationLength)
                Frame = Sprite.AnimationLength;
        }
    }
}
