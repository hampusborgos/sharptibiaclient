using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    public class GameFrame : UITabFrame
    {
        List<GameCanvas> Canvas = new List<GameCanvas>();

        public GameFrame()
        {
            TabWidth = 150;
            UITab Tab = AddTab("+");
            Tab.Bounds.Width = 30;
        }

        public void AddClient(ClientState State)
        {
            GameCanvas Canvas = new GameCanvas(State);
            Canvas.Bounds.X = 0;
            Canvas.Bounds.Y = 18;
            AddSubview(Canvas);

            InsertTab(Tabs.Count - 1, State.Viewport.Player.Name + "(" + State.HostName + ")");
        }
    }
}
