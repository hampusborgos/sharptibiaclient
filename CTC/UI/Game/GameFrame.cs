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

        public override void LayoutSubviews()
        {
            if (ActiveCanvas != null)
            {
                double Scale;
                if (ClientBounds.Width < ClientBounds.Height * 3 / 4)
                    Scale = ClientBounds.Width / 480f;
                else
                    Scale = ClientBounds.Height / 352f;

                ActiveCanvas.Bounds = new Rectangle
                {
                    Width = (int)(480 * Scale),
                    Height = (int)(352 * Scale)
                };
                ActiveCanvas.Bounds.X = (ClientBounds.Width - ActiveCanvas.Bounds.Width) / 2;
                ActiveCanvas.Bounds.Y = ClientBounds.Top;
            }
            base.LayoutSubviews();
        }
    }
}
