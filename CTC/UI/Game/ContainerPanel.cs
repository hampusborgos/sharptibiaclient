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

        public ContainerPanel(ClientViewport Viewport, int ContainerID)
        {
            this.Viewport = Viewport;
            this.ContainerID = ContainerID;
            Padding = new Margin
            {
                Top = 4,
                Left = 7,
                Bottom = 4,
                Right = 0
            };
            ((UIStackView)ContentView).StackDirection = UIStackDirection.Horizontal;
            ((UIStackView)ContentView).Overflow = true;

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
            // Ignore updates for other containers
            if (Container.ContainerID != ContainerID)
                return;

            GetSubviewWithTag("_ContainerUpButton").Visible = false;

            // Update the title
            Name = Container.Name;

            List<ItemButton> ItemButtons = ContentView.SubviewsOfType<ItemButton>();

            // Check if the volume has changed, then we need to recreate the subviews
            if (ItemButtons.Count != Container.MaximumVolume)
            {
                // Replace the child views
                ContentView.RemoveSubviewsMatching(delegate(UIView view)
                {
                    return view is ItemButton;
                });

                for (int ButtonIndex = 0; ButtonIndex < Container.MaximumVolume; ++ButtonIndex)
                {
                    ContentView.AddSubview(new ItemButton(Renderer, null)
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

                ItemButtons = ContentView.SubviewsOfType<ItemButton>();
            }

            // Update the item buttons with the new contents
            int Slot = 0;
            foreach (ItemButton Button in ItemButtons)
                Button.Item = Container.GetItem(Slot++);

            NeedsLayout = true;
        }

        protected void OnCloseContainer(ClientViewport Viewport, ClientContainer Container)
        {
            // Ignore updates for other containers
            if (Container.ContainerID != ContainerID)
                return;

            Viewport.UpdateContainer -= OnUpdateContainer;
            Viewport.CloseContainer -= OnCloseContainer;
        }

        #region Drawing

        protected override void BeginDraw()
        {
            // Create the renderer if required (and propagate it)
            if (Renderer == null)
            {
                Renderer = new GameRenderer(Context, Viewport.GameData);

                foreach (ItemButton Button in ContentView.SubviewsOfType<ItemButton>())
                    Button.Renderer = Renderer;
            }

            base.BeginDraw();
        }

        #endregion
    }
}
