using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    public class InventoryPanel : UIFrame
    {
        ClientViewport Viewport;

        public InventoryPanel(UIPanel Parent)
            : base(Parent)
        {
            Name = "Inventory";
            Bounds = new Rectangle(0, 0, 200, 100);
        }

        public void OnNewState(ClientState NewState)
        {
            Viewport = NewState.Viewport;
        }

        public override void Draw(SpriteBatch CurrentBatch)
        {
            base.Draw(CurrentBatch);
        }
    }
}
