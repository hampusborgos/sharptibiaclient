using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    public enum InventorySlot
    {
        None = -1,

        First = 1,
        Necklace = 2,
        Backpack = 3,
        Armor = 4,
        Right = 5,
        Left = 6,
        Legs = 7,
        Feet = 8,
        Ring = 9,
        Ammo = 10,
    };

    public enum PlayerStat
    {
        Health,
        HealthMax,
        Capacity,
        Experience,
        Mana,
        ManaMax,
        Soul,
        Stamina
    };

    enum PlayerSkill
    {
        Level,
        Magic,
        Fist,
        Club,
        Sword,
        Axe,
        Distance,
        Shield,
        Fishing,
    };

    public enum Direction
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3,
        NorthEast = 4,
        NorthWest = 5,
        SouthEast = 6,
        SouthWest = 7
    };

    public enum CreatureSkull
    {
        None = 0,
        Yellow = 1,
        Green = 2,
        White = 3,
        Red = 4,
        Black = 5
    };

    public enum PartyShield
    {
        None = 0,
        WhiteYellow = 1,
        WhiteBlue = 2,
        Blue = 3,
        Yellow = 4,
        BlueShared = 5,
        YellowShared = 6,
        BlueSharedBlink = 7,
        YellowSharedBlink = 8,
        BlueNoShared = 9,
        YellowNoShared = 10
    };

    public enum GuildEmblem
    {
        None = 0,
        Green = 1,
        Red = 2,
        Blue = 3
    };

    public enum MessageType
    {
        Say = 0x01,
        Whisper = 0x02,
        Yell = 0x03,

        Private = 0x04, // player to player, before 8.2 this was 0x04, after this is 0x06

        ChannelYellow = 0x05,	//yellow
        ReportViolation = 0x06,
        RuleViolationGameMaster = 0x07,
        RuleViolationPlayer = 0x08,
        Broadcast = 0x09,
        ChannelRed = 0x0A,	//red - #c text
        PrivateRed = 0x0B,	//@name@text
        ChannelOrange = 0x0C,	//orange
        ChannelAnonymousRed = 0x0E,	//red anonymous - #d text

        MonsterSay = 0x10,
        MonsterYell = 0x11,

        Warning = 0x12, /*Red message in game window and in the console*/
        Advance = 0x13, /*White message in game window and in the console*/
        ConsoleNotice = 0x14, /*White message at the bottom of the game window and in the console*/
        ConsoleNotice2 = 0x15, /*White message at the bottom of the game window and in the console*/
        Description = 0x16, /*Green message in game window and in the console*/
        Notice = 0x17, /*White message at the bottom of the game window"*/
        ConsoleBlue = 0x18, /*Blue message in the console*/
        ConsoleRed = 0x19, /*Red message in the console*/



        PrivatePlayerToNPC, // player to npc, from 8.2 onwards
        PrivateNPCToPlayer,  // npc to player, from 8.2 onwards
        ReportRuleViolation, // initial send to ctrl+r channel, 8.2 onwards (?!)
        RuleViolationAnswer, // answer to ctrl+r channel, 8.2 onwards (?!)
        RuleViolationReply, // answering answer to report, 8.2 onwards (?!)

        White // white, 8.4 onwards
    };

    public enum TextColor
    {
        Blue = 5,
        BoldBlue = 23,
        LightBlue = 35,
        LightGreen = 30,
        LightGrey = 129,
        Purple = 131,
        DarkRed = 144,
        DarkOrange = 186,
        Red = 194,
        Orange = 192,
        Yellow = 210,
        White = 251,
        None = 255
    };

    [Flags]
    public enum ConditionState
    {
        None = 0,
        Poison = 1,
        Burn = 2,
        Energy = 4,
        Drunk = 8,
        Manashield = 16,
        Paralyzed = 32,
        Haste = 64,
        InFight = 128,
        Drowning = 256,
        Freezing = 512,
        Dazzled = 1024,
        Cursed = 2048,
        Buffed = 4096,
        PZBlocked = 8192,
        PZ = 16384,
    };

    public struct LightSource
    {
        public int Color;
        public int Level;
    };
}
