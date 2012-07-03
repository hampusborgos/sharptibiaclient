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


            this.Padding = new Margin
            {
                Top = 5,
                Left = 5
            };

            Bounds.Width = 200;
            Bounds.Height = 200;
        }

        public void OnNewState(ClientState NewState)
        {
            Viewport = NewState.Viewport;
        }

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
    }
}
