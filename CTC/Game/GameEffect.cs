using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace CTC
{
    public class GameEffect
    {
        protected double Elapsed = 0;
        protected double Duration = 0;

        public virtual void Update(GameTime Time)
        {
            Elapsed += Time.ElapsedGameTime.TotalSeconds;
        }

        public Boolean Expired
        {
            get
            {
                return Elapsed > Duration;
            }
        }
    }
}
