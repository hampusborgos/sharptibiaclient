using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace CTC
{
    public class DistanceEffect : GameEffect
    {
        public readonly int ID;
        public readonly GameSprite Sprite;
        public int Frame;
        public Vector2 Offset;
        private Vector2 Speed;

        public DistanceEffect(TibiaGameData GameData, int ID, MapPosition From, MapPosition To)
        {
            this.ID = ID;
            Sprite = GameData.GetDistanceEffectSprite(ID);

            int dx = To.X - From.X;
            int dy = To.Y - From.Y;

            /*
             * 0 = NW
             * 1 = N
             * 2 = NE
             * 3 = W
             * 4 = C
             * 5 = E
             * 6 = SW
             * 7 = S
             * 8 = SE
             * */

            double tan = 10;
            if (dx != 0)
                tan = (float)dy / (float)dx;

	        if(Math.Abs(tan) < 0.4142f)
            {
		        if(dx > 0)
			        Frame = 5;// EAST
		        else
                    Frame = 3;// WEST
	        }
	        else if(Math.Abs(tan) < 2.4142f)
            {
		        if(tan > 0)
                {
			        if(dy > 0)
                        Frame = 8;// SOUTH EAST
			        else
                        Frame = 0;// NORTH WEST;
		        }
		        else
                { //tan < 0
			        if(dx > 0)
                        Frame = 2;// NORTH EAST;
			        else
                        Frame = 6;// SOUTH WEST
		        }
	        }
	        else
            {
		        if(dy > 0)
                    Frame = 7;// SOUTH
		        else
                    Frame = 1;// NORTH
            }

            Duration = Math.Sqrt(dx * dx + dy * dy) / 10;
            Offset = new Vector2(
                (From.X - Math.Max(From.X, To.X)) * 32,
                (From.Y - Math.Max(From.Y, To.Y)) * 32
            );
            Speed = new Vector2(Math.Sign(dx) * 160, Math.Sign(dy) * 160);
            //Speed -= Offset;
        }

        public override void Update(GameTime Time)
        {
 	        base.Update(Time);

            Offset.X += (float)(Speed.X * Time.ElapsedGameTime.TotalSeconds);
            Offset.Y += (float)(Speed.Y * Time.ElapsedGameTime.TotalSeconds);
        }
    }
}

