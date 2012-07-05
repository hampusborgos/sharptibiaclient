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
        public GameDesktop()
        {
            // Store window size
            Bounds.X = 0;
            Bounds.Y = 0;
            Bounds.Width = UIContext.Window.ClientBounds.Width;
            Bounds.Height = UIContext.Window.ClientBounds.Height;

            UIContext.GameWindowSize = Bounds;

            // Listener when window changes size
            UIContext.Window.ClientSizeChanged += new EventHandler<EventArgs>(OnResize);
        }

        #region Data Members and Properties

        List<ClientState> Clients = new List<ClientState>();

        TopTaskbar Taskbar;
        GameSidebar Sidebar;
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

            // Read in some state (in case the game was fast-forwarded)
            foreach (ClientContainer Container in State.Viewport.Containers.Values)
                OnOpenContainer(State.Viewport, Container);

            // Hook up handlers for some events
            State.Viewport.OpenContainer += OnOpenContainer;
            State.Viewport.CloseContainer += OnCloseContainer;
            Frame.AddClient(State);
        }

        #region Event Handlers

        /// <summary>
        /// The game window was resized
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        void OnResize(object o, EventArgs args)
        {
            System.Console.WriteLine("Game Window was resized!");
            if (UIContext.Window.ClientBounds.Height > 0 && UIContext.Window.ClientBounds.Width > 0)
            {
                // Update the context size
                UIContext.GameWindowSize = UIContext.Window.ClientBounds;

                NeedsLayout = true;
            }
        }

        /// <summary>
        /// We override this to handle captured devices
        /// </summary>
        /// <param name="mouse"></param>
        /// <returns></returns>
        public override bool MouseLeftClick(MouseState mouse)
        {
            if (UIContext.MouseFocusedPanel != null)
                return UIContext.MouseFocusedPanel.MouseLeftClick(mouse);

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
            // To get mouse move events you must capture the mouse first
            if (UIContext.MouseFocusedPanel != null)
                return UIContext.MouseFocusedPanel.MouseMove(mouse);
            return false;
        }

        protected void OnOpenContainer(ClientViewport Viewport, ClientContainer Container)
        {
            ContainerPanel Panel = new ContainerPanel(Viewport, Container.ContainerID);
            Panel.Bounds.Height = 100;
            Sidebar.AddWindow(Panel);
        }

        protected void OnCloseContainer(ClientViewport Viewport, ClientContainer Container)
        {
            foreach (ContainerPanel CPanel in Sidebar.SubviewsOfType<ContainerPanel>())
                if (CPanel.ContainerID == Container.ContainerID)
                    CPanel.RemoveFromSuperview();
        }

        #endregion

        public override void LayoutSubviews()
        {
            // Change the size of this view
            Bounds.Width = UIContext.GameWindowSize.Width;
            Bounds.Height = UIContext.GameWindowSize.Height;

            // Resize the sidebar to fit
            Sidebar.Bounds = new Rectangle
            {
                X = ClientBounds.Width - Sidebar.FullBounds.Width,
                Y = ClientBounds.Top,
                Height = ClientBounds.Height,
                Width = Sidebar.FullBounds.Width
            }.Subtract(Sidebar.Margin);

            Chat.Bounds = new Rectangle
            {
                X = ClientBounds.Top,
                Y = ClientBounds.Height - Chat.FullBounds.Height,
                Width = ClientBounds.Width - Sidebar.FullBounds.Width,
                Height = Chat.Bounds.Height
            }.Subtract(Chat.Margin);

            Frame.Bounds = new Rectangle
            {
                X = ClientBounds.Top,
                Y = ClientBounds.Left,
                Width = ClientBounds.Width - Sidebar.FullBounds.Width,
                Height = ClientBounds.Height - Chat.Bounds.Height
            }.Subtract(Frame.Margin);

            base.LayoutSubviews();
        }

        public override void Update(GameTime Time)
        {
            UIContext.Update(Time);

            LFPS.Enqueue(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond);
            // Remove ticks older than one second
            while (LFPS.Count > 0 && LFPS.First() < DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond - 1000)
                LFPS.Dequeue();

            foreach (ClientState State in Clients)
                State.Update(Time);

            base.Update(Time);
        }

        #region Drawing Code

        public override void Draw(SpriteBatch NullBatch, Rectangle BoundingBox)
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
            Vector2 FontOrigin = UIContext.StandardFont.MeasureString(o);
            FontOrigin.X = UIContext.Window.ClientBounds.Width - FontOrigin.X - 4;
            FontOrigin.Y = 4;

            // Draw the string
            ForegroundBatch.DrawString(
                UIContext.StandardFont, o, FontOrigin,
                Color.LightGreen, 0.0f, new Vector2(0.0f, 0.0f),
                1.0f, SpriteEffects.None, 0.5f);

            // Draw UI
            DrawBackgroundChildren(ForegroundBatch, Bounds);
            DrawForegroundChildren(ForegroundBatch, Bounds);
            // End draw UI

            ForegroundBatch.End();
        }

        #endregion


        #region Loading Code

        public void Load()
        {
            ForegroundBatch = new SpriteBatch(UIContext.Graphics.GraphicsDevice);
        }

        public void CreatePanels()
        {
            Taskbar = new TopTaskbar();
            AddSubview(Taskbar);

            Frame = new GameFrame();
            Frame.Bounds.X = 10;
            Frame.Bounds.Y = 20;
            Frame.Bounds.Width = 800;
            Frame.Bounds.Height = 600;
            Frame.ZOrder = -1;
            AddSubview(Frame);

            Sidebar = new GameSidebar();

            SkillPanel Skills = new SkillPanel();
            Skills.Bounds.X = 4;
            Skills.Bounds.Y = Sidebar.ClientBounds.Top;
            Skills.Bounds.Height = 200;
            Sidebar.AddWindow(Skills);

            VIPPanel VIPs = new VIPPanel();
            VIPs.Bounds.X = 4;
            VIPs.Bounds.Y = 210;
            Sidebar.AddWindow(VIPs);

            InventoryPanel Inventory = new InventoryPanel();
            Inventory.Bounds.X = 4;
            Inventory.Bounds.Y = 410;
            Sidebar.AddWindow(Inventory);

            AddSubview(Sidebar);

            Chat = new ChatPanel();
            Chat.Bounds.Height = 180;
            // Chat.Margin.Right = 10;
            AddSubview(Chat);

            // Register listeners
            ActiveStateChanged += Skills.OnNewState;
            ActiveStateChanged += VIPs.OnNewState;
            ActiveStateChanged += Inventory.OnNewState;
            ActiveStateChanged += Chat.OnNewState;
            ActiveStateChanged += Chat.OnNewState;
        }

        #endregion
    }
}
