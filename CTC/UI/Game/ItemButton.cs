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

            ElementType = UIElementType.InventorySlot;
        }

        public override bool Highlighted
        {
            get
            {
                return base.Highlighted;
            }
            set
            {
                base.Highlighted = value;

                // Reset 'our' element type
                ElementType = UIElementType.InventorySlot;
            }
        }

        public override bool MouseLeftClick(Microsoft.Xna.Framework.Input.MouseState mouse)
        {
            return base.MouseLeftClick(mouse);
        }

        protected override void DrawContent(SpriteBatch Batch)
        {
            Renderer.DrawInventorySlot(Batch, ScreenBounds);

            if (Item != null)
                Renderer.DrawInventoryItem(Batch, Item, ScreenClientBounds);
        }
    }
}
