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

        /// <summary>
        /// One vertical stack per column. A window belongs to exactly one of
        /// these, which is what lets it be dragged from one column to another:
        /// with a single flowing stack, which column a window ended up in was
        /// only ever a side effect of its position in the list.
        /// </summary>
        private List<UIStackView> ColumnViews = new List<UIStackView>();

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
            
            CreateMenu();
        }

        /// <summary>
        /// Every window in the sidebar, left column first, top to bottom.
        /// </summary>
        public List<T> WindowsOfType<T>() where T : UIFrame
        {
            List<T> Found = new List<T>();
            foreach (UIStackView Column in ColumnViews)
                Found.AddRange(Column.SubviewsOfType<T>());
            return Found;
        }

        private UIStackView NewColumn()
        {
            UIStackView Column = new UIStackView(UIStackDirection.Vertical);
            Column.ZOrder = -1;

            // The sidebar sizes its columns itself. Left false, UIStackView would
            // shrink Bounds down to the content size once it had laid out, and
            // since UIView.Update re-lays out any view with NeedsLayout set -
            // without the sidebar getting a chance to restore the size first -
            // the next pass would be working from the wrong bounds.
            Column.Autoresizable = true;
            return Column;
        }

        /// <summary>
        /// Adds or drops column stacks to match the current width. Windows in a
        /// column that no longer exists move to the last surviving one rather
        /// than disappearing with it.
        /// </summary>
        private void EnsureColumns(int Wanted)
        {
            while (ColumnViews.Count > Wanted && ColumnViews.Count > 1)
            {
                UIStackView Dropped = ColumnViews[ColumnViews.Count - 1];
                ColumnViews.RemoveAt(ColumnViews.Count - 1);

                UIStackView Survivor = ColumnViews[ColumnViews.Count - 1];
                foreach (UIFrame Window in Dropped.SubviewsOfType<UIFrame>())
                {
                    Dropped.RemoveSubview(Window);
                    Survivor.AddSubview(Window);
                }

                RemoveSubview(Dropped);
            }

            while (ColumnViews.Count < Wanted)
            {
                UIStackView Column = NewColumn();
                ColumnViews.Add(Column);
                AddSubview(Column);
            }
        }

        /// <summary>
        /// The column a window currently lives in, or null if it is being dragged.
        /// </summary>
        private UIStackView ColumnOf(UIFrame Window)
        {
            foreach (UIStackView Column in ColumnViews)
                if (Column.SubviewsOfType<UIFrame>().Contains(Window))
                    return Column;
            return null;
        }

        /// <summary>
        /// Total height of the windows stacked in a column.
        /// </summary>
        private int ColumnContentHeight(UIStackView Column)
        {
            int Height = 0;
            foreach (UIFrame Window in Column.SubviewsOfType<UIFrame>())
                Height += Window.FullBounds.Height;
            return Height;
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

            EnsureColumns(Math.Max(1, Columns));

            // Drop it into whichever column is currently the emptiest, so opening
            // several windows spreads them out instead of piling up on the left.
            UIStackView Target = ColumnViews[0];
            foreach (UIStackView Column in ColumnViews)
                if (ColumnContentHeight(Column) < ColumnContentHeight(Target))
                    Target = Column;

            Target.AddSubview(Window);
        }

        /// <summary>
        /// Where the column the drag started in sits, so the dragged window can be
        /// kept under the cursor while it is parented to the sidebar instead.
        /// </summary>
        private Point DragOrigin = Point.Zero;

        /// <summary>
        /// Translucent stand-in showing where the dragged window will land.
        /// </summary>
        private UIDropPlaceholder Placeholder = null;

        private void WindowStartedToMove(UIFrame MovedFrame)
        {
            // Take it out of its column and float it on the sidebar itself, so it
            // draws above the other windows and is not laid out while dragging.
            UIStackView Column = ColumnOf(MovedFrame);
            if (Column != null)
            {
                DragOrigin = new Point(Column.Bounds.Left, Column.Bounds.Top);

                // Swap the window for a ghost of it in the very same slot, so
                // nothing moves until the drag actually reaches somewhere else.
                int Slot = Column.Subviews.IndexOf(MovedFrame);
                Placeholder = new UIDropPlaceholder(MovedFrame);

                Column.RemoveSubview(MovedFrame);
                InsertAt(Column, Placeholder, Slot);
            }

            AddSubview(MovedFrame);
            NeedsLayout = false;

            // Move it into the sidebar's coordinates, drag anchor included, so it
            // stays under the cursor and can be dragged freely in any direction.
            MovedFrame.OffsetWhileDragging(DragOrigin.X, DragOrigin.Y);
        }

        private void WindowMoved(UIFrame MovedFrame)
        {
            MovePlaceholderUnder(MovedFrame);
        }

        private void WindowStoppedMoving(UIFrame MovedFrame)
        {
            // UIFrame raises this from MouseLost, which also fires when focus is
            // simply taken by another view, so there is not always a drag to end.
            if (Placeholder == null)
                return;

            // Put it back in this view
            RemoveSubview(MovedFrame);

            MovePlaceholderUnder(MovedFrame);

            // Drop it exactly where the ghost has been showing it would go.
            UIStackView Target = (UIStackView)Placeholder.Parent;
            int Slot = Target.Subviews.IndexOf(Placeholder);

            Target.RemoveSubview(Placeholder);
            Placeholder = null;

            InsertAt(Target, MovedFrame, Slot);
        }

        /// <summary>
        /// Puts the ghost in the slot the window would drop into right now. The
        /// column comes from the middle of the dragged window, the position
        /// within it from comparing against the midpoint of each window there.
        /// </summary>
        private void MovePlaceholderUnder(UIFrame MovedFrame)
        {
            if (Placeholder == null)
                return;

            EnsureColumns(Math.Max(1, Columns));
            UIStackView Target = ColumnViews[ColumnIndexAt(MovedFrame)];

            // Take the ghost out first, so the slots counted below are the ones
            // the remaining windows actually occupy.
            if (Placeholder.Parent != null)
                Placeholder.Parent.RemoveSubview(Placeholder);

            int Slot = 0;
            foreach (UIView Window in Target.Subviews)
            {
                int MiddleOfWindow = Target.Bounds.Top + Window.Bounds.Y + Window.Bounds.Height / 2;
                if (MovedFrame.Bounds.Y < MiddleOfWindow)
                    break;
                ++Slot;
            }

            InsertAt(Target, Placeholder, Slot);
        }

        /// <summary>
        /// Adds a view to a column at a given position in the stack.
        /// </summary>
        private void InsertAt(UIView Column, UIView View, int Slot)
        {
            List<UIView> Order = Column.Subviews;
            if (Slot >= 0 && Slot < Order.Count)
                Column.AddSubviewBefore(View, Order[Slot]);
            else
                Column.AddSubview(View);
        }

        /// <summary>
        /// Which column a dropped window landed on. Measured from the middle of
        /// the window, so one straddling a boundary goes where most of it is.
        /// </summary>
        private int ColumnIndexAt(UIFrame MovedFrame)
        {
            int Middle = MovedFrame.Bounds.X + MovedFrame.Bounds.Width / 2 - ClientBounds.Left;
            int Index = Middle / ColumnWidth;
            return (int)MathHelper.Clamp(Index, 0, ColumnViews.Count - 1);
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

            // Lay the columns out side by side under the menu. Each is exactly one
            // window wide, which is what makes the drop target unambiguous.
            EnsureColumns(Columns);
            for (int i = 0; i < ColumnViews.Count; ++i)
            {
                ColumnViews[i].Bounds = new Rectangle
                {
                    X = cb.Left + i * ColumnWidth,
                    Y = cb.Top + MenuHeight,
                    Width = ColumnWidth,
                    Height = cb.Height - MenuHeight
                };
            }

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
