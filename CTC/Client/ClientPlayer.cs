using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    /// <summary>
    /// One of the skills the player has.
    /// These might be server-side configured in the future?
    /// </summary>
    public class ClientSkill
    {
        public String LongName;
        public int Value = 0;
        public int Percent = 0;

        public ClientSkill(String LongName)
        {
            this.LongName = LongName;
        }
    };

    /// <summary>
    /// 
    /// </summary>
    public class ClientPlayer : ClientCreature
    {
        public Dictionary<string, ClientSkill> Skill = new Dictionary<string, ClientSkill>();

        public int Capacity = 0;
        public int Experience = 0;
        public ClientSkill Level = new ClientSkill("Level");
        public ClientSkill MagicLevel = new ClientSkill("Magic Level");

        public ConditionState Conditions = ConditionState.None;

        public ClientPlayer(UInt32 PlayerID)
            : base(PlayerID)
        {
            Skill["Fist"] = new ClientSkill("Fist Fighting");
            Skill["Club"] = new ClientSkill("Club Fighting");
            Skill["Sword"] = new ClientSkill("Sword Fighting");
            Skill["Axe"] = new ClientSkill("Axe Fighting");
            Skill["Dist"] = new ClientSkill("Distance Fighting");
            Skill["Shield"] = new ClientSkill("Shielding");
            Skill["Fish"] = new ClientSkill("Fishing");
        }
    }
}
