using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    public class VIPPanel : UIVirtualFrame
    {
        ClientViewport Viewport;

        public VIPPanel()
        {
            Name = "VIP List";
            Padding = new Margin(5);
            ContentView = new UIStackView(UIStackDirection.Vertical);
            
            Bounds.Width = 200;
            Bounds.Height = 200;
        }

        public void OnNewState(ClientState NewState)
        {
            if (Viewport != null)
                Viewport.VIPStatusChanged -= VIPStatusChanged;

            Viewport = NewState.Viewport;
            UpdateList();

            Viewport.VIPStatusChanged += VIPStatusChanged;
        }

        private void VIPStatusChanged(ClientViewport Viewport, ClientCreature Creature)
        {
            if (Viewport != this.Viewport)
                return;

            UpdateList();
        }

        private void UpdateList()
        {
            ContentView.RemoveAllSubviews();

            List<String> OfflineNames = new List<String>();
            List<String> OnlineNames = new List<String>();

            foreach (ClientCreature VIP in Viewport.VIPList.Values)
            {
                if (VIP.Online)
                    OnlineNames.Add(VIP.Name);
                else
                    OfflineNames.Add(VIP.Name);
            }

            foreach (String VIP in OnlineNames)
            {
                UILabel Label = new UILabel(VIP);
                Label.TextColor = Color.LightGreen;
                ContentView.AddSubview(Label);
            }

            foreach (String VIP in OfflineNames)
            {
                UILabel Label = new UILabel(VIP);
                Label.TextColor = Color.Red;
                ContentView.AddSubview(Label);
            }
        }
        
        /*
        protected override void DrawContent(SpriteBatch CurrentBatch)
        {
            Vector2 pos = new Vector2(0, 0);
            foreach (ClientCreature vip in Viewport.VIPList.Values)
            {
                if (vip.Online == false)
                    continue;

                CurrentBatch.DrawString(
                    Context.StandardFont, vip.Name, ScreenCoordinate(pos),
                    (vip.Online ? Color.LightGreen : Color.Red),
                    0.0f, new Vector2(0.0f, 0.0f),
                    1.0f, SpriteEffects.None, 0.5f
                );

                pos.Y += 16;
            }
        }
        */
    }
}
