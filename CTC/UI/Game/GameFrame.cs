using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace CTC
{
    public class GameFrame : UITabFrame
    {
        List<GameCanvas> Canvas = new List<GameCanvas>();
        GameCanvas ActiveCanvas = null;

        public GameFrame()
        {
            TabWidth = 150;
            UITab Tab = AddTab("+");
            Tab.Bounds.Width = 30;
        }

        public void AddClient(ClientState State)
        {
            GameCanvas Canvas = new GameCanvas(State);
            AddSubview(Canvas);
            ActiveCanvas = Canvas;

            InsertTab(Tabs.Count - 1, State.Viewport.Player.Name + "(" + State.HostName + ")");
        }

        /// <summary>
        /// The game view is a fixed 480x352 window onto the map, so the height
        /// the frame wants is dictated by how wide it is allowed to be. The
        /// desktop uses this to hand the frame exactly enough room for the view
        /// to reach the sidebar without leaving bars down either side.
        /// </summary>
        public int PreferredHeightForWidth(int Width)
        {
            // Whatever the skin and tab bar take off the top and sides.
            int ChromeWidth = Bounds.Width - ClientBounds.Width;
            int ChromeHeight = Bounds.Height - ClientBounds.Height;

            int CanvasWidth = Math.Max(1, Width - ChromeWidth);
            return (int)Math.Ceiling(CanvasWidth * 352.0 / 480.0) + ChromeHeight;
        }

        public override void LayoutSubviews()
        {
            if (ActiveCanvas != null)
            {
                // Fill the width, so the view runs right up to the sidebar.
                // Only fall back to fitting the height if the frame is too
                // short to show the whole view at that scale.
                double Scale = ClientBounds.Width / 480f;
                if (352 * Scale > ClientBounds.Height)
                    Scale = ClientBounds.Height / 352f;

                ActiveCanvas.Bounds = new Rectangle
                {
                    Width = (int)(480 * Scale),
                    Height = (int)(352 * Scale)
                };
                ActiveCanvas.Bounds.X = ClientBounds.Left
                    + (ClientBounds.Width - ActiveCanvas.Bounds.Width) / 2;
                ActiveCanvas.Bounds.Y = ClientBounds.Top;
            }
            base.LayoutSubviews();
        }
    }
}
