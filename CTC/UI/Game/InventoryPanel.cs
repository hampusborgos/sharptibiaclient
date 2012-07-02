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

        public InventoryPanel(UIView Parent)
            : base(Parent)
        {
            Name = "Inventory";
            Bounds = new Rectangle(0, 0, 200, 200);

            for (InventorySlot slot = InventorySlot.First; slot <= InventorySlot.Last; slot++)
                AddSubview(new InventoryItemButton(this, Viewport, slot)
                {
                    Bounds = GetSlotPosition(slot)
                });
        }

        public void OnNewState(ClientState NewState)
        {
            Viewport = NewState.Viewport;
            foreach (InventoryItemButton Button in SubviewsOfType<InventoryItemButton>())
                Button.OnNewState(NewState);
        }

        protected Rectangle GetSlotPosition(InventorySlot slot)
        {
            switch (slot)
            {
                case InventorySlot.Head:
                    return new Rectangle(SkinPadding.Left + 48, SkinPadding.Top + 8, 34, 34);
                case InventorySlot.Necklace:
                    return new Rectangle(SkinPadding.Left + 8, SkinPadding.Top + 30, 34, 34);
                case InventorySlot.Backpack:
                    return new Rectangle(SkinPadding.Left + 88, SkinPadding.Top + 30, 34, 34);
                case InventorySlot.Armor:
                    return new Rectangle(SkinPadding.Left + 48, SkinPadding.Top + 50, 34, 34);
                case InventorySlot.Right:
                    return new Rectangle(SkinPadding.Left + 88, SkinPadding.Top + 70, 34, 34);
                case InventorySlot.Left:
                    return new Rectangle(SkinPadding.Left + 8,  SkinPadding.Top + 70, 34, 34);
                case InventorySlot.Legs:
                    return new Rectangle(SkinPadding.Left + 48, SkinPadding.Top + 90, 34, 34);
                case InventorySlot.Feet:
                    return new Rectangle(SkinPadding.Left + 48, SkinPadding.Top + 130, 34, 34);
                case InventorySlot.Ring:
                    return new Rectangle(SkinPadding.Left + 8,  SkinPadding.Top + 110, 34, 34);
                case InventorySlot.Ammo:
                    return new Rectangle(SkinPadding.Left + 88, SkinPadding.Top + 110, 34, 34);
            }
            return new Rectangle();
        }
    }
}
