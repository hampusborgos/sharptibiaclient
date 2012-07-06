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

        public int Columns = 1;

        public GameSidebar()
        {
            ElementType = UIElementType.Window;
            
            ContentView = new UIStackView(UIStackDirection.Vertical);
            ContentView.ZOrder = -1;

            CreateMenu();

            AddSubview(ContentView);
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
            
            UIButton SkillToggle = new UIButton("Skills");
            SkillToggle.Bounds = new Rectangle(0, 0, 44, 15);
            Menu.AddSubview(SkillToggle);

            UIButton BattleToggle = new UIButton("Battle");
            BattleToggle.Bounds = new Rectangle(44, 0, 44, 15);
            Menu.AddSubview(BattleToggle);

            UIButton VIPToggle = new UIButton("VIP");
            VIPToggle.Bounds = new Rectangle(88, 0, 44, 15);
            Menu.AddSubview(VIPToggle);

            UIButton MapToggle = new UIButton("Map");
            MapToggle.Bounds = new Rectangle(132, 0, 44, 15);
            Menu.AddSubview(MapToggle);

            AddSubview(Menu);
        }
    }
}
