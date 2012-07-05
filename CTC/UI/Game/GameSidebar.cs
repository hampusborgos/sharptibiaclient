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
        public UIStackView ContentView;

        public GameSidebar()
        {
            ElementType = UIElementType.Window;
            ContentView = new UIStackView(UIStackDirection.Vertical);
            ContentView.ZOrder = -1;
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
            AddSubview(MovedFrame);
            // Offset it's position so it's still under the cursor
            MovedFrame.Bounds.X += ContentView.Bounds.Left;
            MovedFrame.Bounds.Y += ContentView.Bounds.Top;
        }

        private void WindowMoved(UIFrame MovedFrame)
        {
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
            Bounds = new Rectangle
            {
                X = Context.Window.ClientBounds.Width - 200 - 50,
                Y = 10,
                Width = 208,
                Height = Context.Window.ClientBounds.Height - 20
            };

            ContentView.Bounds = ClientBounds;

            base.LayoutSubviews();
        }
    }
}
