using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CTC
{
    public class GameFrame : UITabFrame
    {
        List<GameCanvas> Canvas = new List<GameCanvas>();

        public GameFrame(UIPanel Parent)
            : base(Parent)
        {
            UITab Tab = AddTab("+");
            Tab.Bounds.Width = 30;
        }

        public void AddClient(ClientState State)
        {
            GameCanvas Canvas = new GameCanvas(this, State);
            Canvas.Bounds.X = 0;
            Canvas.Bounds.Y = 18;
            AddChildPanel(Canvas);

            InsertTab(Tabs.Count - 1, State.Viewport.Player.Name + "(" + State.HostName + ")");
        }
    }
}
