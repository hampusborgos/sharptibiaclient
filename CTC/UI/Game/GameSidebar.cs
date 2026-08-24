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
        /// Width of a single window column. Every panel in the sidebar is built
        /// to this width, so it is also the smallest the sidebar can be dragged.
        /// </summary>
        public const int ColumnWidth = 176;

        /// <summary>
        /// How wide the grab strip along the left edge is.
        /// </summary>
        private const int ResizeHandleWidth = 6;

        /// <summary>
        /// How many columns of windows currently fit. Derived from the width.
        /// </summary>
        public int Columns { get; private set; }

        /// <summary>
        /// The width the user has dragged the sidebar to. The actual width snaps
        /// to a whole number of columns, so windows never leave a dead strip.
        /// </summary>
        private int RequestedWidth = ColumnWidth * 2;

        /// <summary>
        /// Where the drag started, while the left edge is being dragged.
        /// </summary>
        private Vector2? ResizingFrom = null;
        private int ResizeStartWidth = 0;

        /// <summary>
        /// The sidebar always keeps room for at least one column of windows.
        /// </summary>
        public int MinimumWidth
        {
            get { return ColumnWidth + SkinPadding.TotalWidth; }
        }

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
            
            ContentView = new UIStackView(UIStackDirection.Vertical, true);
            ContentView.ZOrder = -1;

            // The sidebar sizes the content view itself. Leaving this false would
            // let UIStackView shrink Bounds.Height down to the content height once
            // it has laid out, and since UIView.Update re-lays out any view with
            // NeedsLayout set - without the sidebar getting a chance to restore the
            // height first - the next pass would think it had only a couple of
            // hundred pixels to work with and wrap into a new column immediately.
            ContentView.Autoresizable = true;

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

        /// <summary>
        /// Settles how wide the sidebar is, snapped to a whole number of columns
        /// and never wider than half the space it has been given.
        ///
        /// The desktop calls this before laying out, because it needs the final
        /// width to know how much room is left for the game view.
        /// </summary>
        public void ResolveWidth(int AvailableWidth)
        {
            int MaxColumns = Math.Max(1, (AvailableWidth / 2) / ColumnWidth);

            int DesiredClientWidth = RequestedWidth - SkinPadding.TotalWidth;
            int WantedColumns = (DesiredClientWidth + ColumnWidth / 2) / ColumnWidth;

            Columns = (int)MathHelper.Clamp(WantedColumns, 1, MaxColumns);
            Bounds.Width = ColumnWidth * Columns + SkinPadding.TotalWidth;
        }

        /// <summary>
        /// True if the given screen coordinate is on the drag strip.
        /// </summary>
        private bool OverResizeHandle(MouseState mouse)
        {
            Rectangle Screen = ScreenBounds;
            return mouse.X >= Screen.Left
                && mouse.X < Screen.Left + ResizeHandleWidth
                && mouse.Y >= Screen.Top
                && mouse.Y < Screen.Bottom;
        }

        public override bool MouseLeftClick(MouseState mouse)
        {
            if (mouse.LeftButton == ButtonState.Pressed)
            {
                if (ResizingFrom == null && OverResizeHandle(mouse) && CaptureMouse())
                {
                    ResizingFrom = new Vector2(mouse.X, mouse.Y);
                    ResizeStartWidth = Bounds.Width;
                    return true;
                }
            }
            else if (ResizingFrom != null)
            {
                ResizingFrom = null;
                ReleaseMouse();
                return true;
            }

            return base.MouseLeftClick(mouse);
        }

        public override bool MouseMove(MouseState mouse)
        {
            if (ResizingFrom != null)
            {
                // The sidebar is anchored to the right, so dragging the left
                // edge further left makes it wider.
                int Delta = (int)(ResizingFrom.Value.X - mouse.X);
                RequestedWidth = Math.Max(MinimumWidth, ResizeStartWidth + Delta);

                // The game view has to give up the space, so relayout from the top.
                Desktop.NeedsLayout = true;
                return true;
            }

            return base.MouseMove(mouse);
        }

        public override bool MouseLost()
        {
            ResizingFrom = null;
            return base.MouseLost();
        }

        public override void LayoutSubviews()
        {
            ResolveWidth(Parent != null ? Parent.ClientBounds.Width : Bounds.Width * 2);

            Rectangle cb = ClientBounds;
            MenuView.Bounds = new Rectangle
            {
                X = cb.Left,
                Y = cb.Top,
                Width = cb.Width,
                Height = MenuView.Bounds.Height
            };

            // The menu is a wrapping horizontal stack, so at narrow widths the
            // toggles spill onto extra rows. Lay it out now and read back the
            // height it actually needs, otherwise the windows below would be
            // tucked underneath the second row of buttons.
            MenuView.LayoutSubviews();

            int MenuHeight = MenuView.Bounds.Height;
            ContentView.Bounds = new Rectangle
            {
                X = cb.Left,
                Y = cb.Top + MenuHeight,
                Width = cb.Width,
                Height = cb.Height - MenuHeight
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
