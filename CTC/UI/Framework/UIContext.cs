using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    public static class UIContext
    {
        /// <summary>
        /// The OS window of the game (as provided by XNA)
        /// </summary>
        public static GameWindow Window;

        /// <summary>
        /// The size of the OS window the game is contained in.
        /// We use this value since it's 'safe'. It is updated when we receive
        /// OnResize from the OS, which means the OS window has been safely resized,
        /// if we read the values directly from the GameWindow, it might be that
        /// are actually larger than the window.
        /// </summary>
        public static Rectangle GameWindowSize;

        public static GraphicsDeviceManager Graphics;
        public static ContentManager Content;
        public static RasterizerState Rasterizer;

        /// <summary>
        /// The time elapsed in the game
        /// </summary>
        public static GameTime GameTime;

        public static UIView MouseFocusedPanel;

        public static Boolean SkinChanged;
        public static UISkin Skin;
        public static SpriteFont StandardFont;

        public static Stack<Rectangle> ScissorStack = new Stack<Rectangle>();

        public static void Initialize(GameWindow Window, GraphicsDeviceManager Graphics, ContentManager Content)
        {
            UIContext.Window = Window;
            UIContext.Graphics = Graphics;
            UIContext.Content = Content;

            Rasterizer = new RasterizerState()
            {
                ScissorTestEnable = true
            };
        }

        public static void Load()
        {
            StandardFont = Content.Load<SpriteFont>("StandardFont");
            Skin = new UISkin();
            Skin.Load(null);
            SkinChanged = true;
        }

        public static void Update(GameTime Time)
        {
            GameTime = Time;
        }
    }
}
