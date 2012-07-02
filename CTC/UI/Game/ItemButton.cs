using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    class ItemButton : UIButton
    {
        private GameRenderer Renderer;
        public ClientItem Item;

        public ItemButton(UIView Parent, GameRenderer Renderer, ClientItem Item)
            : base(Parent)
        {
            this.Item = Item;
            this.Renderer = Renderer;

            Bounds.Width = 34;
            Bounds.Height = 34;
        }

        protected override void DrawContent(SpriteBatch Batch)
        {
            Renderer.DrawInventorySlot(Batch, ScreenBounds);

            if (Item != null)
            {
                Rectangle itemRect = ScreenCoordinate(0, 0, 32 ,32);
                Renderer.DrawInventoryItem(Batch, Item, itemRect);
            }
        }
    }
}
