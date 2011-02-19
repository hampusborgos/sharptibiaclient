using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    public class GameSprite
    {
        private UInt16 id;
        public UInt16 ID {
            get {return id;}
        }
        public Boolean IsStackable = false;
        public Boolean IsFluidContainer = false;
        public Boolean IsSplash = false;
        public Boolean FloorTransparent = false;
        public Boolean ColorTemplate = false;

        public int RenderHeight = 0;
        public int RenderOffset = 0;
        public int HealthOffset = 0;
        public int AnimationSpeed = 500;

        public int Height = 1;
        public int Width = 1;
        public int BlendFrames = 1;
        public int XDiv = 1;
        public int YDiv = 1;
        public int ZDiv = 1;
        public int AnimationLength = 0;

        public int SpriteCount
        {
            get
            {
                return Height * Width * BlendFrames * XDiv * YDiv * ZDiv * AnimationLength;
            }
        }

        public List<GameImage> Images = new List<GameImage>();

        public GameSprite(UInt16 id)
        {
            this.id = id;
        }

        public GameImage GetImage(int X, int Y, int Frame, int Count, int XDiv, int YDiv, int ZDiv, int Time)
        {
            int v;
            if (Count >= 0 && this.Height <= 1 && this.Width <= 1)
                v = Count;
            else
                v = ((((((Time % this.AnimationLength) * this.YDiv + YDiv) * this.XDiv + XDiv) * this.BlendFrames + Frame) * this.Height + Y) * this.Width + X);
            
            if (v >= this.Images.Count)
            {
                if (this.Images.Count == 1)
                {
                    v = 0;
                }
                else
                {
                    v %= this.Images.Count;
                }
            }
            return this.Images[v];
        }

        public GameImage GetImage(int X, int Y, Direction Direction, ClientOutfit Outfit, int Time)
        {
            int v;
            v = (((((int)Direction) * this.BlendFrames) * this.Height + Y) * this.Width + X);
            if(v >= this.Images.Count)
            {
                if(this.Images.Count == 1)
                    v = 0;
                else
                    v %= this.Images.Count;
            }

            /*
            if(frames > 1)
            { // Template
                TemplateImage* img = getTemplateImage(v, _outfit);
                return img->getHardwareID();
            }
             * */
            return this.Images[v];
        }
    }
}
