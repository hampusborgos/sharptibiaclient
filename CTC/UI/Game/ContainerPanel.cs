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
            Padding = new Margin(4, 7);
            ContentView = new UIStackView(this, UIStackDirection.HorizontalThenVertical);

            UIButton UpButton = CreateButton("U");
            UpButton.Tag = "_ContainerUpButton";
            AddFrameButton(UpButton);

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
            GetSubviewWithTag("_ContainerUpButton").Visible = false;

            // Update the title
            Name = Container.Name;

            // Replace the child views
            ContentView.Subviews.RemoveAll(delegate(UIView view) {
                return view.GetType() == typeof(ItemButton);
            });

            for (int Slot = 0; Slot < Container.MaximumVolume; ++Slot)
            {
                ContentView.AddSubview(new ItemButton(ContentView, Renderer, null)
                {
                    Margin = new Margin
                    {
                        Top = 0,
                        Right = 0,
                        Bottom = 3,
                        Left = 3,
                    }
                });
            }

            NeedsLayout = true;
        }

        protected void OnCloseContainer(ClientViewport Viewport, ClientContainer Container)
        {
            Viewport.UpdateContainer -= OnUpdateContainer;
            Viewport.CloseContainer -= OnCloseContainer;
        }

        protected override void DrawContent(SpriteBatch Batch)
        {
            if (Viewport != null)
            {
                ClientContainer Container = Viewport.Containers[ContainerID];

                int Slot = 0;
                foreach (UIView Subview in ContentView.Subviews)
                {
                    if (Subview.GetType() == typeof(ItemButton))
                    {
                        ItemButton Button = (ItemButton)Subview;
                        Button.Item = Container.GetItem(Slot);
                       ++Slot;
                    }
                }
            }

            base.DrawContent(Batch);
        }
    }
}
