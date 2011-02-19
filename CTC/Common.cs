using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    public class Common
    {
        public static DateTime UnixTime(long ticks)
        {
            DateTime Origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Origin.AddSeconds(ticks); ;
        }
    }

    public interface ICleanupable
    {
        void Cleanup();
    }
}
