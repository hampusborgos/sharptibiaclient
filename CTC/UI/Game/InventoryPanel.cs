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

        /// <summary>
        /// Need a reference to be able to unsubscribe to state change events.
        /// </summary>
        protected GameDesktop Desktop;
        

        public InventoryPanel(GameDesktop Desktop)
        {
            Name = "Inventory";
            Bounds = new Rectangle(0, 0, 176, 200);

            for (InventorySlot slot = InventorySlot.First; slot <= InventorySlot.Last; slot++)
            {
                InventoryItemButton InventoryItem = new InventoryItemButton(Viewport, slot)
                {
                    Bounds = GetSlotPosition(slot)
                };
                InventoryItem.Autoresizable = false;
                ContentView.AddSubview(InventoryItem);
            }

            this.Desktop = Desktop;
            Desktop.ActiveViewportChanged += ViewportChanged;
            ViewportChanged(Desktop.ActiveViewport);
        }

        public void ViewportChanged(ClientViewport NewViewport)
        {
            Viewport = NewViewport;
            foreach (InventoryItemButton Button in ContentView.SubviewsOfType<InventoryItemButton>())
                Button.ViewportChanged(NewViewport);
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
