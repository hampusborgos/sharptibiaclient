using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CTC
{
    public class UIFrame : UIView
    {
        private Vector2? BeingDraggedFrom;
        private Vector2? DraggedFromPosition;
        public String Name;

        private UIView _ContentView = null;
        public UIView ContentView
        {
            get
            {
                return _ContentView;
            }
            set
            {
                if (_ContentView != null)
                    RemoveSubview(_ContentView);
                _ContentView = value;
                AddSubview(value);
            }
        }

        public bool BeingDragged
        {
            get { return BeingDraggedFrom != null; }
        }

        public UIFrame()
        {
            ElementType = UIElementType.Frame;
            Name = "UIFrame";
            ContentView = new UIView(null, UIElementType.None);

            AddDefaultButtons();
        }

        public override bool MouseLeftClick(MouseState mouse)
        {
            if (base.MouseLeftClick(mouse))
                return true;

            // Check if the frame was grabbed
            if (mouse.LeftButton == ButtonState.Pressed)
            {
                if (ClientMouseCoordinate(mouse).Y < 0)
                {
                    // Start dragging!
                    if (CaptureMouse())
                    {
                        DraggedFromPosition = new Vector2(Bounds.X, Bounds.Y);
                        BeingDraggedFrom = new Vector2(mouse.X, mouse.Y);
                        Parent.BringSubviewToFront(this);
                    }
                }
                else
                {
                    // Put us ontop
                    Parent.BringSubviewToFront(this);
                }
            }
            else
            {
                BeingDraggedFrom = null;
                DraggedFromPosition = null;
                ReleaseMouse();
            }

            return false;
        }

        public override bool MouseMove(MouseState mouse)
        {
            if (BeingDraggedFrom != null)
            {
                float dx = BeingDraggedFrom.Value.X - mouse.X;
                float dy = BeingDraggedFrom.Value.Y - mouse.Y;
                Bounds.X = (int)(DraggedFromPosition.Value.X - dx);
                Bounds.Y = (int)(DraggedFromPosition.Value.Y - dy);

                Rectangle ParentBounds = Parent.ClientBounds;
                if (Bounds.X < ParentBounds.Left)
                    Bounds.X = ParentBounds.Left;
                if (Bounds.Y < ParentBounds.Top)
                    Bounds.Y = ParentBounds.Top;
                if (Bounds.Right > ParentBounds.Right)
                    Bounds.X = ParentBounds.Right - Bounds.Width;
                if (Bounds.Bottom > ParentBounds.Bottom)
                    Bounds.Y = ParentBounds.Bottom - Bounds.Height;
                return true;
            }
            return false;
        }

        public override void LayoutSubviews()
        {
            // Make the content fill up the frame
            ContentView.Bounds = ClientBounds.Subtract(ContentView.Padding);

            base.LayoutSubviews();

            // Layout the buttons
            int N = 1;
            foreach (UIButton Button in FrameButtons)
            {
                Button.Position = new Vector2(
                    Bounds.Width - (Button.Bounds.Width + 1) * N - 3,
                    SkinPadding.Top - Button.Bounds.Height - 1
                );
                ++N;
            }
        }

        #region Button management

        protected List<UIButton> FrameButtons = new List<UIButton>();

        public override void RemoveSubview(UIView Subview)
        {
            if (Subview is UIButton)
                FrameButtons.Remove((UIButton)Subview);
            base.RemoveSubview(Subview);
        }

        public void AddFrameButton(UIButton Button)
        {
            AddSubview(Button);
            FrameButtons.Add(Button);
        }

        #endregion

        #region Drawing

        protected override void DrawBorder(SpriteBatch CurrentBatch)
        {
            base.DrawBorder(CurrentBatch);

            Vector2 pos = new Vector2(ScreenBounds.X + 5, ScreenBounds.Y + 2);
            
            CurrentBatch.DrawString(
                Context.StandardFont, Name, pos,
                Color.LightGray,
                0.0f, new Vector2(0.0f, 0.0f),
                1.0f, SpriteEffects.None, 0.5f
            );
        }

        #endregion

        #region Creation and Loading

        protected UIButton CreateButton(String Label)
        {
            UIButton Button = new UIButton();
            Button.Bounds.Width = 12;
            Button.Bounds.Height = 12;
            Button.Label = Label;
            Button.Margin.Top = -10;
            Button.ZOrder = 1;
            return Button;
        }

        protected void AddDefaultButtons()
        {
            UIButton CloseButton = CreateButton("X");
            AddFrameButton(CloseButton);

            UIButton MinimizeButton = CreateButton("-");
            AddFrameButton(MinimizeButton);
        }

        #endregion
    }
}
