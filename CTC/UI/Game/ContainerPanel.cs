using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CTC
{
    class ContainerPanel : UIVirtualFrame
    {
        ClientViewport Viewport;
        GameRenderer Renderer;
        public readonly int ContainerID;

        public ContainerPanel(UIView Parent, ClientViewport Viewport, int ContainerID)
            : base(Parent)
        {
            this.Viewport = Viewport;
            this.ContainerID = ContainerID;
            Renderer = new GameRenderer(this.Context, Viewport.GameData);
            Padding = new Rectangle(7, 4, 7, 4);

            if (Viewport.Containers[ContainerID].HasParent)
                AddButton(CreateButton("U"));
        }

        public void OnNewState(ClientState NewState)
        {
            Visible = (NewState.Viewport == Viewport);
        }

        protected Rectangle GetSlotPosition(int Slot)
        {
            // How many slots can fit per row?
            int SlotsPerRow = (ClientBounds.Width - Padding.Width) / 35;
            // Then what row are we on?
            int Col = Slot % SlotsPerRow;
            int Row = Slot / SlotsPerRow;
            return new Rectangle(
                Col * 37, Row * 37,
                34, 34);
        }

        protected override void DrawContent(SpriteBatch Batch)
        {
            if (Viewport != null)
            {
                ClientContainer Container = Viewport.Containers[ContainerID];

                for (int Slot = 0; Slot < Container.MaximumVolume; ++Slot)
                {
                    Rectangle slotPosition = ScreenCoordinate(GetSlotPosition(Slot));
                    Renderer.DrawInventorySlot(Batch, slotPosition);

                    // Draw item if there is one
                    if (Slot < Container.Contents.Count)
                    {
                        ClientItem item = Container.Contents[(int)Slot];
                        Renderer.DrawInventoryItem(Batch, item, slotPosition);
                    }
                }
            }
        }
    }
}
