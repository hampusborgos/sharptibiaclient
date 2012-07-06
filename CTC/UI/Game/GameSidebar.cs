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
        public UIView Menu;
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
        public int Columns = 1;

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
            Menu.Bounds = new Rectangle
            {
                X = cb.Left,
                Y = cb.Top,
                Width = cb.Width,
                Height = 15
            };
            ContentView.Bounds = new Rectangle
            {
                X = cb.Left,
                Y = cb.Top + Menu.Bounds.Height,
                Width = cb.Width,
                Height = cb.Height - Menu.Bounds.Height
            };

            base.LayoutSubviews();
        }

        private void CreateMenu()
        {
            Menu = new UIView(null, UIElementType.BorderlessWindow);
            
            UIToggleButton SkillToggle = new UIToggleButton("Skills");
            SkillToggle.Bounds = new Rectangle(0, 0, 44, 15);
            SkillToggle.ButtonToggled += delegate(UIToggleButton Button, MouseState mouse)
            {
                if (Button.On && SkillsView == null)
                {
                    SkillsView = new SkillPanel(Desktop);
                    AddWindow(SkillsView);
                }
                else if (!Button.On && SkillsView != null)
                {
                    SkillsView.RemoveFromSuperview();
                    SkillsView = null;
                }
            };
            Menu.AddSubview(SkillToggle);

            UIToggleButton BattleToggle = new UIToggleButton("Battle");
            BattleToggle.Bounds = new Rectangle(44, 0, 44, 15);
            Menu.AddSubview(BattleToggle);

            UIToggleButton VIPToggle = new UIToggleButton("VIP");
            VIPToggle.Bounds = new Rectangle(88, 0, 44, 15);
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
            Menu.AddSubview(VIPToggle);

            UIButton MapToggle = new UIButton("Map");
            MapToggle.Bounds = new Rectangle(132, 0, 44, 15);
            Menu.AddSubview(MapToggle);

            AddSubview(Menu);
        }
    }
}
