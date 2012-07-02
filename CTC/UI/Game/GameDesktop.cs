using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CTC
{
    public delegate void StateChangedEventHandler(ClientState NewState);

    class GameDesktop : UIView
    {
        public GameDesktop(GameWindow Window, GraphicsDeviceManager Graphics, ContentManager Content)
            : base(new UIContext(Window, Graphics, Content))
        {
            // Store window size
            Bounds.X = 0;
            Bounds.Y = 0;
            Bounds.Width = Window.ClientBounds.Width;
            Bounds.Height = Window.ClientBounds.Height;

            // Listener when window changes size
            Window.ClientSizeChanged += new EventHandler<EventArgs>(OnResize);

            // Create all the panels we need
            CreatePanels();
        }

        #region Data Members and Properties

        List<ClientState> Clients = new List<ClientState>();

        TopTaskbar Taskbar;
        SkillPanel Skills;
        VIPPanel VIPs;
        InventoryPanel Inventory;
        ChatPanel Chat;
        GameFrame Frame;

        SpriteBatch ForegroundBatch;

        protected ClientState ActiveClient
        {
            get
            {
                return Clients[0];
            }
            set
            {
                ActiveStateChanged(value);
            }
        }

        Queue<long> LFPS = new Queue<long>();
        Queue<long> GFPS = new Queue<long>(); 

        #endregion

        #region Event Slots

        public event StateChangedEventHandler ActiveStateChanged;

        #endregion

        // Methods
        public void AddClient(ClientState State)
        {
            Clients.Add(State);
            if (Clients.Count == 1)
                ActiveClient = State;
            Frame.AddClient(State);
        }

        #region Event Handlers

        // Event handlers
        void OnResize(object o, EventArgs args)
        {
            if (Context.Window.ClientBounds.Height > 0 && Context.Window.ClientBounds.Width > 0)
            {
                Bounds.Width = Context.Window.ClientBounds.Width;
                Bounds.Height = Context.Window.ClientBounds.Height;
            }
        }

        /// <summary>
        /// We override this to handle captured devices
        /// </summary>
        /// <param name="mouse"></param>
        /// <returns></returns>
        public override bool MouseLeftClick(MouseState mouse)
        {
            if (Context.MouseFocusedPanel != null)
                return Context.MouseFocusedPanel.MouseLeftClick(mouse);

            // We use a copy so that event handling can modify the list
            List<UIView> SubviewListCopy = new List<UIView>(Children);
            foreach (UIView subview in SubviewListCopy)
            {
                if (subview.AcceptsMouseEvent(mouse))
                    if (subview.MouseLeftClick(mouse))
                        return true;
            }

            return false;
        }

        public override bool MouseMove(MouseState mouse)
        {
            if (Context.MouseFocusedPanel != null)
                return Context.MouseFocusedPanel.MouseMove(mouse);
            return false;
        }

        #endregion

        public override void Update(GameTime Time)
        {
            Context.GameTime = Time;

            LFPS.Enqueue(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
            while (LFPS.Count > 0 && LFPS.First() < DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - 1000)
                LFPS.Dequeue();

            foreach (ClientState State in Clients)
                State.Update(Time);

            base.Update(Time);
        }

        #region Drawing Code

        public override void Draw(SpriteBatch NullBatch)
        {
            ForegroundBatch.Begin();

            string o = "";

            GFPS.Enqueue(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
            while (GFPS.Count > 0 && GFPS.First() < DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - 1000)
                GFPS.Dequeue();

            o += " LFPS: " + LFPS.Count;
            o += " GFPS: " + GFPS.Count;
            o += " RCTC";

            // Find the center of the string
            Vector2 FontOrigin = Context.StandardFont.MeasureString(o);
            FontOrigin.X = Context.Window.ClientBounds.Width - FontOrigin.X - 4;
            FontOrigin.Y = 4;

            // Draw the string
            ForegroundBatch.DrawString(
                Context.StandardFont, o, FontOrigin,
                Color.LightGreen, 0.0f, new Vector2(0.0f, 0.0f),
                1.0f, SpriteEffects.None, 0.5f);

            // Draw UI
            DrawChildren(ForegroundBatch);
            // End draw UI

            ForegroundBatch.End();
        }

        #endregion


        #region Loading Code

        public void Load()
        {
            Context.Load();

            ForegroundBatch = new SpriteBatch(Context.Graphics.GraphicsDevice);
        }

        private void CreatePanels()
        {
            Taskbar = new TopTaskbar(this);
            AddSubview(Taskbar);

            Frame = new GameFrame(this);
            Frame.Bounds.X = 10;
            Frame.Bounds.Y = 20;
            Frame.ZOrder = -1;
            AddSubview(Frame);

            Skills = new SkillPanel(this);
            Skills.Bounds.X = Bounds.Width - Skills.Bounds.Width - 50;
            Skills.Bounds.Y = 300;
            Skills.ZOrder = 1;
            AddSubview(Skills);

            VIPs = new VIPPanel(this);
            VIPs.Bounds.X = Bounds.Width - VIPs.Bounds.Width - 50;
            VIPs.Bounds.Y = 500;
            VIPs.ZOrder = 1;
            AddSubview(VIPs);

            Inventory = new InventoryPanel(this);
            Inventory.Bounds.X = Bounds.Width - Inventory.Bounds.Width - 50;
            Inventory.Bounds.Y = 100;
            Inventory.ZOrder = 1;
            AddSubview(Inventory);

            Chat = new ChatPanel(this);
            Chat.Bounds.X = 10;
            Chat.Bounds.Y = 640;
            Chat.Bounds.Height = 150;
            Chat.Bounds.Width = 800;
            Chat.ZOrder = 0;
            AddSubview(Chat);

            // Register listeners
            ActiveStateChanged += Skills.OnNewState;
            ActiveStateChanged += VIPs.OnNewState;
            ActiveStateChanged += Inventory.OnNewState;
            ActiveStateChanged += Chat.OnNewState;
        }

        #endregion
    }
}
