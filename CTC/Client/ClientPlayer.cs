using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    public class ClientPlayer : ClientCreature
    {
        public Dictionary<string, int> Skill = new Dictionary<string, int>();
        public Dictionary<string, int> SkillPercent = new Dictionary<string, int>();

        public int Capacity = 0;
        public int Experience = 0;

        public ConditionState Conditions = ConditionState.None;

        public ClientPlayer(UInt32 PlayerID)
            : base(PlayerID)
        {
            ;
        }
    }
}
