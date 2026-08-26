using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    public class ClientOutfit
    {
        public int LookType;
        public int LookItem;
        public int LookHead;
        public int LookBody;
        public int LookLegs;
        public int LookFeet;

        public ClientOutfit Clone()
        {
            ClientOutfit Copy = new ClientOutfit();
            Copy.LookType = LookType;
            Copy.LookItem = LookItem;
            Copy.LookHead = LookHead;
            Copy.LookBody = LookBody;
            Copy.LookLegs = LookLegs;
            Copy.LookFeet = LookFeet;
            return Copy;
        }
    }
}
