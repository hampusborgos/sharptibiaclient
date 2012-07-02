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
        UIStackView ContentView;
        UIButton UpButton;
        public readonly int ContainerID;

        public ContainerPanel(UIView Parent, ClientViewport Viewport, int ContainerID)
            : base(Parent)
        {
            this.Viewport = Viewport;
            this.ContainerID = ContainerID;
            Renderer = new GameRenderer(this.Context, Viewport.GameData);
            Padding = new Margin(7, 4, 7, 4);
            ContentView = new UIStackView(this, UIStackDirection.HorizontalThenVertical);
            ContentView.Bounds.Width = 156;
            ContentView.Bounds.Height = 220;
            AddSubview(ContentView);

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

        public override List<UIView> Subviews
        {
            get
            {
                return ContentView.Subviews;
            }
        }

        public override void LayoutSubviews()
        {
            ContentView.Bounds.X = ClientBounds.Left;
            ContentView.Bounds.Y = ClientBounds.Top;

            ContentView.LayoutSubviews();
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
            ContentView.Subviews.RemoveAll(delegate(UIView view) {
                return view.GetType() == typeof(ItemButton);
            });

            for (int Slot = 0; Slot < Container.MaximumVolume; ++Slot)
                ContentView.AddSubview(new ItemButton(ContentView, Renderer, null));

            LayoutSubviews();
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
                foreach (UIView Subview in Subviews)
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
