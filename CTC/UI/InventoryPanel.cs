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
        GameRenderer Renderer;

        public InventoryPanel(UIPanel Parent)
            : base(Parent)
        {
            Name = "Inventory";
            Bounds = new Rectangle(0, 0, 200, 200);
        }

        public void OnNewState(ClientState NewState)
        {
            Viewport = NewState.Viewport;
            Renderer = new GameRenderer(this.Context, Viewport.GameData);
        }

        protected Rectangle GetSlotPosition(InventorySlot slot)
        {
            switch (slot)
            {
                case InventorySlot.Necklace:
                    return new Rectangle(ScreenClientBounds.Left + 8, ScreenClientBounds.Top + 8, 32, 32);
                case InventorySlot.Backpack:
                    return new Rectangle(ScreenClientBounds.Left + 80, ScreenClientBounds.Top + 20, 32, 32);
                case InventorySlot.Armor:
                    return new Rectangle(ScreenClientBounds.Left + 40, ScreenClientBounds.Top + 40, 32, 32);
                case InventorySlot.Right:
                    return new Rectangle(ScreenClientBounds.Left + 80, ScreenClientBounds.Top + 60, 32, 32);
                case InventorySlot.Left:
                    return new Rectangle(ScreenClientBounds.Left + 8,  ScreenClientBounds.Top + 60, 32, 32);
                case InventorySlot.Legs:
                    return new Rectangle(ScreenClientBounds.Left + 40, ScreenClientBounds.Top + 80, 32, 32);
                case InventorySlot.Feet:
                    return new Rectangle(ScreenClientBounds.Left + 40, ScreenClientBounds.Top + 120, 32, 32);
                case InventorySlot.Ring:
                    return new Rectangle(ScreenClientBounds.Left + 8,  ScreenClientBounds.Top + 100, 32, 32);
                case InventorySlot.Ammo:
                    return new Rectangle(ScreenClientBounds.Left + 80, ScreenClientBounds.Top + 100, 32, 32);
            }
            return new Rectangle();
        }

        protected override void DrawContent(SpriteBatch Batch)
        {
            if (Viewport != null)
            {
                for (InventorySlot slot = InventorySlot.First; slot <= InventorySlot.Last; slot++)
                {
                    Rectangle slotPosition = GetSlotPosition(slot);
                    Renderer.DrawInventorySlot(Batch, slotPosition);
                    ClientItem item = Viewport.Inventory[(int)slot];
                    if (item != null)
                        Renderer.DrawInventoryItem(Batch, item, slotPosition);
                }
            }
        }
    }
}
