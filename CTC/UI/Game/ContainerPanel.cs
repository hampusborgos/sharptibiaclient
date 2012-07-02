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
        UIButton UpButton;
        public readonly int ContainerID;

        public ContainerPanel(UIView Parent, ClientViewport Viewport, int ContainerID)
            : base(Parent)
        {
            this.Viewport = Viewport;
            this.ContainerID = ContainerID;
            Renderer = new GameRenderer(this.Context, Viewport.GameData);
            Padding = new Rectangle(7, 4, 7, 4);

            Bounds.Width = 176;
            Bounds.Height = 220;

            // Note: We can't store the ClientContainer since it might be replaced
            // instead we subscribe to update for this container id
            // Subscribe to update for this container ID
            Viewport.UpdateContainer += OnUpdateContainer;
            Viewport.CloseContainer += OnCloseContainer;

            // Perform the first update with the received container
            OnUpdateContainer(Viewport, Viewport.Containers[ContainerID]);
        }

        public void OnNewState(ClientState NewState)
        {
            Visible = (NewState.Viewport == Viewport);
        }

        protected void OnUpdateContainer(ClientViewport Viewport, ClientContainer Container)
        {
            if (Container.HasParent)
            {
                UpButton = CreateButton("U");
                AddButton(UpButton);
            }
            else if (UpButton != null)
            {
                UpButton.RemoveFromSuperview();
            }

            // Update the title
            Name = Container.Name;

            // Replace the child views
            Children.RemoveAll(delegate(UIView view) {
                return view.GetType() == typeof(ItemButton);
            });

            for (int Slot = 0; Slot < Container.MaximumVolume; ++Slot)
            {
                ItemButton Button = new ItemButton(this, Renderer, null);
                Button.Bounds = GetSlotPosition(Slot);
                AddSubview(Button);
            }
        }

        protected void OnCloseContainer(ClientViewport Viewport, ClientContainer Container)
        {
            Viewport.UpdateContainer -= OnUpdateContainer;
            Viewport.CloseContainer -= OnCloseContainer;
        }

        protected Rectangle GetSlotPosition(int Slot)
        {
            // How many slots can fit per row?
            int SlotsPerRow = (ClientBounds.Width - Padding.Width) / 35;
            // Then what row are we on?
            int Col = Slot % SlotsPerRow;
            int Row = Slot / SlotsPerRow;
            return new Rectangle(
                Padding.Left + Col * 37, ClientBounds.Top + Padding.Top + Row * 37,
                34, 34);
        }

        protected override void DrawContent(SpriteBatch Batch)
        {
            if (Viewport != null)
            {
                ClientContainer Container = Viewport.Containers[ContainerID];

                int Slot = 0;
                foreach (UIView Subview in Children)
                {
                    if (Subview.GetType() == typeof(ItemButton))
                    {
                        ItemButton Button = (ItemButton)Subview;
                        Button.Item = Container.GetItem(Slot);
                       ++Slot;
                    }
                }
            }
        }
    }
}
