using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CTC
{
    public class InventoryPanel : UIFrame
    {
        ClientViewport Viewport;
        GameRenderer Renderer;

        public InventoryPanel()
        {
            Name = "Inventory";
            Bounds = new Rectangle(0, 0, 200, 200);

            for (InventorySlot slot = InventorySlot.First; slot <= InventorySlot.Last; slot++)
            {
                InventoryItemButton InventoryItem = new InventoryItemButton(Viewport, slot)
                {
                    Bounds = GetSlotPosition(slot)
                };
                InventoryItem.Autoresizable = false;
                ContentView.AddSubview(InventoryItem);
            }
        }

        public void OnNewState(ClientState NewState)
        {
            Viewport = NewState.Viewport;
            foreach (InventoryItemButton Button in ContentView.SubviewsOfType<InventoryItemButton>())
                Button.OnNewState(NewState);
        }

        protected Rectangle GetSlotPosition(InventorySlot slot)
        {
            switch (slot)
            {
                case InventorySlot.Head:
                    return new Rectangle(48, 8, 34, 34);
                case InventorySlot.Necklace:
                    return new Rectangle(8, 30, 34, 34);
                case InventorySlot.Backpack:
                    return new Rectangle(88, 30, 34, 34);
                case InventorySlot.Armor:
                    return new Rectangle(48, 50, 34, 34);
                case InventorySlot.Right:
                    return new Rectangle(88, 70, 34, 34);
                case InventorySlot.Left:
                    return new Rectangle(8, 70, 34, 34);
                case InventorySlot.Legs:
                    return new Rectangle(48, 90, 34, 34);
                case InventorySlot.Feet:
                    return new Rectangle(48, 130, 34, 34);
                case InventorySlot.Ring:
                    return new Rectangle(8, 110, 34, 34);
                case InventorySlot.Ammo:
                    return new Rectangle(88, 110, 34, 34);
            }
            return new Rectangle();
        }
    }
}
