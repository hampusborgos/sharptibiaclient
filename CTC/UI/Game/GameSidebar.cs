using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CTC
{
    class GameSidebar : UIView
    {
        public UIStackView MenuView;
        public UIStackView ContentView;

        /// <summary>
        /// The current viewport that is displayed in the sidebar.
        /// </summary>
        ClientViewport Viewport;

        /// <summary>
        /// The panel that displays skills
        /// </summary>
        SkillPanel SkillsView;

        /// <summary>
        /// The panel that displays VIPs
        /// </summary>
        VIPPanel VIPView;

        /// <summary>
        /// The panel that displays the player's current inventory
        /// </summary>
        InventoryPanel InventoryView;

        /// <summary>
        /// How many columns are in this sidebar
        /// </summary>
        public int Columns = 2;

        /// <summary>
        /// Need a reference to be able to unsubscribe to state change events.
        /// </summary>
        protected GameDesktop Desktop;


        public GameSidebar(GameDesktop Desktop)
            : base(null, UIElementType.Window)
        {
            this.Desktop = Desktop;
            this.Viewport = Desktop.ActiveViewport;
            Desktop.ActiveViewportChanged += ViewportChanged;
            
            ContentView = new UIStackView(UIStackDirection.Vertical);
            ContentView.ZOrder = -1;

            CreateMenu();

            AddSubview(ContentView);
        }

        protected void ViewportChanged(ClientViewport Viewport)
        {
            this.Viewport = Viewport;
        }

        public void AddWindow(UIFrame Window)
        {
            Window.FrameStartedToMove += WindowStartedToMove;
            Window.FrameMoved += WindowMoved;
            Window.FrameStoppedMoving += WindowStoppedMoving;

            ContentView.AddSubview(Window);
        }

        private void WindowStartedToMove(UIFrame MovedFrame)
        {
            // Remove it from content view and add it as a subview to this view
            ContentView.RemoveSubview(MovedFrame);
            ContentView.NeedsLayout = false;
            AddSubview(MovedFrame);
            NeedsLayout = false;

            // Offset it's position so it's still under the cursor
            MovedFrame.Bounds.X += ContentView.Bounds.Left;
            MovedFrame.Bounds.Y += ContentView.Bounds.Top;
        }

        private void WindowMoved(UIFrame MovedFrame)
        {
            // Offset it's position so it's still under the cursor
            MovedFrame.Bounds.Y += ContentView.Bounds.Top;
        }

        private void WindowStoppedMoving(UIFrame MovedFrame)
        {
            // Put it back in this view
            RemoveSubview(MovedFrame);

            UIFrame LastWindowAbove = null;
            foreach (UIFrame Window in ContentView.SubviewsOfType<UIFrame>())
            {
                // Check if dropped above another window
                int TopOfWindow = ContentView.ClientBounds.Top + Window.Bounds.Y + Window.Bounds.Height / 2;
                if (MovedFrame.Bounds.Y < TopOfWindow)
                {
                    LastWindowAbove = Window;
                    break;
                }
            }

            // Add it to the content view again
            if (LastWindowAbove != null)
                ContentView.AddSubviewBefore(MovedFrame, LastWindowAbove);
            else
                ContentView.AddSubview(MovedFrame);
        }

        public override void LayoutSubviews()
        {
            Bounds.Width = 176 * Columns + SkinPadding.TotalWidth;

            Rectangle cb = ClientBounds;
            MenuView.Bounds = new Rectangle
            {
                X = cb.Left,
                Y = cb.Top,
                Width = cb.Width,
                Height = 15
            };
            ContentView.Bounds = new Rectangle
            {
                X = cb.Left,
                Y = cb.Top + MenuView.Bounds.Height,
                Width = cb.Width,
                Height = cb.Height - MenuView.Bounds.Height
            };

            base.LayoutSubviews();
        }

        private void CreateMenu()
        {
            Rectangle ButtonSize = new Rectangle(0, 0, 58, 15);
            MenuView = new UIStackView(UIStackDirection.Horizontal, true);

            UIToggleButton InventoryToggle = new UIToggleButton("Player");
            InventoryToggle.Bounds = ButtonSize;
            InventoryToggle.ButtonToggled += delegate(UIToggleButton Button, MouseState mouse)
            {
                if (Button.On && InventoryView == null)
                {
                    InventoryView = new InventoryPanel(Desktop);
                    AddWindow(InventoryView);
                }
                else if (!Button.On && InventoryView != null)
                {
                    InventoryView.RemoveFromSuperview();
                    InventoryView = null;
                }
            };
            MenuView.AddSubview(InventoryToggle);

            UIToggleButton SkillToggle = new UIToggleButton("Skills");
            SkillToggle.Bounds = ButtonSize;
            SkillToggle.ButtonToggled += delegate(UIToggleButton Button, MouseState mouse)
            {
                if (Button.On && SkillsView == null)
                {
                    SkillsView = new SkillPanel(Desktop);
                    SkillsView.Bounds.Width = 176;
                    SkillsView.Bounds.Height = 180;
                    AddWindow(SkillsView);
                }
                else if (!Button.On && SkillsView != null)
                {
                    SkillsView.RemoveFromSuperview();
                    SkillsView = null;
                }
            };
            MenuView.AddSubview(SkillToggle);

            UIToggleButton BattleToggle = new UIToggleButton("Battle");
            BattleToggle.Bounds = ButtonSize;
            MenuView.AddSubview(BattleToggle);

            UIToggleButton VIPToggle = new UIToggleButton("VIP");
            VIPToggle.Bounds = ButtonSize;
            VIPToggle.ButtonToggled += delegate(UIToggleButton Button, MouseState mouse)
            {
                if (Button.On && VIPView == null)
                {
                    VIPView = new VIPPanel(Desktop);
                    AddWindow(VIPView);
                }
                else if (!Button.On && VIPView != null)
                {
                    VIPView.RemoveFromSuperview();
                    VIPView = null;
                }
            };
            MenuView.AddSubview(VIPToggle);

            UIButton MapToggle = new UIButton("Map");
            MapToggle.Bounds = ButtonSize;
            MenuView.AddSubview(MapToggle);

            UIButton MenuToggle = new UIButton("Menu");
            MenuToggle.Bounds = ButtonSize;
            MenuView.AddSubview(MenuToggle);

            AddSubview(MenuView);
        }
    }
}
