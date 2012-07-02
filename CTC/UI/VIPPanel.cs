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

        public VIPPanel(UIView Parent)
            : base(Parent)
        {
            Name = "VIP List";

            Bounds.Width = 200;
            Bounds.Height = 200;
        }

        public void OnNewState(ClientState NewState)
        {
            Viewport = NewState.Viewport;
        }

        protected override void DrawContent(SpriteBatch CurrentBatch)
        {
            Vector2 pos = new Vector2(ScreenBounds.X + ClientBounds.X + 5, ScreenBounds.Y + ClientBounds.Y + 5);
            foreach (ClientCreature vip in Viewport.VIPList.Values)
            {
                if (vip.Online == false)
                    continue;

                CurrentBatch.DrawString(
                    Context.StandardFont, vip.Name, pos,
                    (vip.Online ? Color.LightGreen : Color.Red),
                    0.0f, new Vector2(0.0f, 0.0f),
                    1.0f, SpriteEffects.None, 0.5f);
                pos.Y += 16;
            }
        }
    }
}
