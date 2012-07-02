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
        }

        public void OnNewState(ClientState NewState)
        {
            Visible = (NewState.Viewport == Viewport);
        }

        protected Rectangle GetSlotPosition(int Slot)
        {
            int X = Padding.X;
            int Y = Padding.Y;
            // How many slots can fit per row?
            int SlotsPerRow = ClientBounds.Width / 32;
            // Then what row are we on?
            int Col = Slot % SlotsPerRow;
            int Row = Slot / SlotsPerRow;
            return new Rectangle(
                X + Col * 32, Y + Row * 32,
                32, 32);
        }

        protected override void DrawContent(SpriteBatch Batch)
        {
            if (Viewport != null)
            {
                ClientContainer Container = Viewport.Containers[ContainerID];

                for (int Slot = 0; Slot < Container.MaximumVolume; ++Slot)
                {
                    Rectangle slotPosition = GetSlotPosition(Slot);
                    slotPosition = new Rectangle(
                            ScreenBounds.X + slotPosition.X, ScreenBounds.Y + slotPosition.Y,
                            slotPosition.Width, slotPosition.Height);

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
