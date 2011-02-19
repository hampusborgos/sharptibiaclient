using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    public class UIContext
    {
        public GameWindow Window;
        public GraphicsDeviceManager Graphics;
        public ContentManager Content;
        public RasterizerState Rasterizer;
        public GameTime GameTime;

        public UISkin Skin;
        public SpriteFont StandardFont;

        public UIContext(GameWindow Window, GraphicsDeviceManager Graphics, ContentManager Content)
        {
            this.Window = Window;
            this.Graphics = Graphics;
            this.Content = Content;

            Rasterizer = new RasterizerState()
            {
                ScissorTestEnable = true
            };
        }

        public void Load()
        {
            StandardFont = Content.Load<SpriteFont>("StandardFont");
            Skin = new UISkin(this);
            Skin.Load(null);
        }

        public void Update(GameTime Time)
        {
            GameTime = Time;
        }
    }
}
