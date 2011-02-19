using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CTC
{
    public class TibiaGameData
    {
        public readonly int Version = 740;
        UInt32 DatVersion, SprVersion;
        UInt16 EffectCount, DistanceCount, CreatureCount, ItemCount;
        Dictionary<int, GameImage> Images = new Dictionary<int, GameImage>();
        Dictionary<int, GameSprite> Sprites = new Dictionary<int, GameSprite>();
        Dictionary<int, ItemType> Items = new Dictionary<int, ItemType>();
        Stream SpriteFile;

        private UInt32 ReadU32(Stream file)
        {
            byte[] u32 = new byte[4];
            file.Read(u32, 0, 4);
            return BitConverter.ToUInt32(u32, 0);
        }

        private UInt16 ReadU16(Stream file)
        {
            byte[] u16 = new byte[2];
            file.Read(u16, 0, 2);
            return BitConverter.ToUInt16(u16, 0);
        }

        public TibiaGameData(Stream DatFile, Stream SpriteFile)
        {
            this.SpriteFile = SpriteFile;

            SprVersion = ReadU32(SpriteFile);
            DatVersion = ReadU32(DatFile);

	        //get max id
            ItemCount = ReadU16(DatFile);
            CreatureCount = ReadU16(DatFile);
            EffectCount = ReadU16(DatFile);
            DistanceCount = ReadU16(DatFile);

	        UInt16 MinClientID = 100; // tibia.dat start with id 100
	        // We don't load distance/effects, if we would, just add effect_count & distance_count here
	        UInt16 MaxClientID = (UInt16)((int)ItemCount + (int)CreatureCount + (int)EffectCount + (int)DistanceCount);

	        UInt16 ID = MinClientID;
	        // loop through all ItemDatabase until we reach the end of file
	        while(ID <= MaxClientID)
            {
		        GameSprite sType = new GameSprite(ID);
                ItemType iType = new ItemType(ID, sType);
		        Sprites[ID] = sType;
                Items[ID] = iType;
                
                // Is it an effect?
                if (ID >= ItemCount + CreatureCount)
                    sType.AnimationSpeed = 100;
                if (ID >= ItemCount && ID < ItemCount + CreatureCount)
                    ;// sType.RenderOffset = -16;

		        // read the options until we find a 0xff
		        byte lastopt;
		        byte optbyte = 0xff;

		        do
		        {
			        lastopt = optbyte;
			        optbyte = (byte)DatFile.ReadByte();

                    {
				        //7.4
				        switch(optbyte)
				        {
					        case 0x00:
						        //is groundtile
                                iType.IsGround = true;
						        iType.WalkSpeed = ReadU16(DatFile);
                                iType.AlwaysOnTop = 0;
						        break;
					        case 0x01: // all OnTop
                                iType.AlwaysOnTop = 1;
						        break;
					        case 0x02:
                                iType.CanSeeThrough = true;
                                iType.AlwaysOnTop = 2;
						        break;
					        case 0x03:
                                iType.IsContainer = true;
						        break;
					        case 0x04:
						        iType.IsStackable = true;
                                sType.IsStackable = true;
						        break;
					        case 0x05:
						        iType.IsUseableWith = true;
						        break;
					        case 0x06:
                                iType.IsLadder = true;
						        break;
					        case 0x07:
                                iType.IsReadable = true;
						        iType.IsWriteable = true;
                                iType.MaxTextLength = ReadU16(DatFile);
						        break;
					        case 0x08: // writtable objects that can't be edited 
						        iType.IsReadable = true;
						        break;
					        case 0x09: //can contain fluids
                                iType.IsFluidContainer = true;
                                sType.IsFluidContainer = true;
						        break;
                            case 0x0A:
                                iType.IsSplash = true;
                                sType.IsSplash = true;
						        break;
					        case 0x0B:
						        iType.IsSolid = true;
						        break;
					        case 0x0C:
                                iType.Immoveable = true;
						        //Log.Fatal("Unknown attribute 0x0C - " + ID, this);
						        break;
					        case 0x0D: // blocks missiles (walls, magic wall etc)
						        iType.BlockProjectiles = true;
						        break;
					        case 0x0E: // blocks monster movement (flowers, parcels etc)
                                iType.BlockPathfinding = true;
						        break;
					        case 0x0F:
						        iType.CanPickup = true;
						        break;
					        case 0x10:
						        iType.LightLevel = ReadU16(DatFile);
						        iType.LightColor = ReadU16(DatFile);
						        break;
					        case 0x11: // can see what is under (ladder holes, stairs holes etc)
                                sType.FloorTransparent = true;
						        break;
					        case 0x12: // ground tiles that don't cause level change
						        iType.DontFallThrough = true;
						        break;
					        case 0x13: // mostly blocking items, but also items that can pile up in level (boxes, chairs etc)
                                iType.HasHeight = true;
                                sType.RenderHeight = ReadU16(DatFile);
						        break;
					        case 0x14: // player color templates
                                sType.RenderOffset = -8;
                                if (ID >=ItemCount)
                                    sType.ColorTemplate = true;
						        break;
					        case 0x18: // cropses that don't decay
                                iType.StaticCorpse = true;
						        break;
					        case 0x16:
                                // Blocking items, most ground
						        //Log.Debug("Unknown attribute 0x16 - " + ID, this);
						        ReadU16(DatFile);
						        break;
					        case 0x17:  // seems like decorables with 4 states of turning (exception first 4 are unique statues)
                                iType.CanTurn = true;
						        break;
					        case 0x19:  // wall items
                                iType.IsWall = true;
						        break;
					        case 0x1A: 
                                // House walls, horizontal and corner, maybe CIP uses them only in houses?
						        break;
					        case 0x1B:  // walls 2 types of them same material (total 4 pairs)
						        break;
					        case 0x1C:  // Creature related, draw health bar with offset perhaps?
                                sType.HealthOffset = 16;
						        break;
					        case 0x1D:  // line spot ...
						        iType.IsFloorChanger = true;
                                 // Always between 1100 and 1115~
                                ReadU16(DatFile);
						        //Log.Debug("Unknown attribute 0x1D - " + ID + " - " + ReadU16(file), this);
						        break;
					        case 0xFF:
						        break;
					        default:
                                throw new System.IO.IOException("Malformed metadata file.");
				        }
			        }
		        } while(optbyte != 0xFF);

		        // Size and GameSprite data
		        sType.Width = DatFile.ReadByte();
		        sType.Height = DatFile.ReadByte();
		        if((sType.Width > 1) || (sType.Height > 1)){
			        DatFile.ReadByte(); // Pointless?
		        }

		        sType.BlendFrames = DatFile.ReadByte();
		        sType.XDiv = DatFile.ReadByte();
		        sType.YDiv = DatFile.ReadByte();
		        // 7.6 +
			    // sType.ZDiv = file.ReadByte(); // Is this ever used? Maybe it means something else?
		        sType.AnimationLength = DatFile.ReadByte();

		        // Read the sprite ids
		        for(int i = 0; i < sType.SpriteCount; ++i)
                {
			        UInt16 SpriteID = ReadU16(DatFile);
                    GameImage img = GetImage(SpriteID);
			        if(img == null)
                    {
                        img = new GameImage(this, SpriteID);
				        Images[SpriteID] = img;
			        }
                    sType.Images.Add(img);
		        }

                ++ID;
	        }
        }

        public GameImage GetImage(int id)
        {
            GameImage Image;
            if (Images.TryGetValue(id, out Image))
                return Image;
            return null;
        }

        public GameSprite GetItemSprite(int id)
        {
            if (id > ItemCount)
                return null;
            GameSprite Sprite;
            if (Sprites.TryGetValue(id, out Sprite))
                return Sprite;
            return null;
        }

        public GameSprite GetCreatureSprite(int id)
        {
            if (id > CreatureCount)
                return null;
            id = id + ItemCount;
            GameSprite Sprite;
            if (Sprites.TryGetValue(id, out Sprite))
                return Sprite;
            return null;
        }

        public GameSprite GetEffectSprite(int id)
        {
            if (id > EffectCount)
                return null;
            id = id + ItemCount + CreatureCount;
            GameSprite Sprite;
            if (Sprites.TryGetValue(id, out Sprite))
                return Sprite;
            return null;
        }

        public GameSprite GetDistanceEffectSprite(int id)
        {
            if (id > DistanceCount)
                return null;
            id = id + ItemCount + CreatureCount + EffectCount;
            GameSprite Sprite;
            if (Sprites.TryGetValue(id, out Sprite))
                return Sprite;
            return null;
        }

        public ItemType GetItemType(int id)
        {
            if (Items.ContainsKey(id))
                return Items[id];
            return null;
        }


        public Byte[] LoadSpriteDump(int id)
        {
            SpriteFile.Seek(2 + id * 4, SeekOrigin.Begin);
            UInt32 target = ReadU32(SpriteFile);
            SpriteFile.Seek(target + 3, SeekOrigin.Begin);
            Byte[] buffer = new Byte[ReadU16(SpriteFile)];
            SpriteFile.Read(buffer, 0, buffer.Length);
            return buffer;
        }
    }
}
