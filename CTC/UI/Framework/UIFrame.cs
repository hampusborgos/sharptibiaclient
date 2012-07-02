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

        public bool BeingDragged
        {
            get { return BeingDraggedFrom != null; }
        }

        public UIFrame(UIView Parent)
            : base(Parent)
        {
            ElementType = UIElementType.Frame;
            Name = "UIFrame";

            AddDefultButtons();
        }

        protected List<UIButton> Buttons = new List<UIButton>();
        public String Name;

        #region Methods

        public void AddButton(UIButton Button)
        {;
            Buttons.Add(Button);
            Recalculate();
        }

        protected void Recalculate()
        {
            int X = Bounds.Width - 2;

            foreach (UIButton Button in Buttons)
            {
                X -= Button.Bounds.Width + 1;
                Button.Bounds.X = X;
                Button.Bounds.Y = 3;
            }
        }

        #endregion

        public override bool MouseLeftClick(MouseState mouse)
        {
            if (base.MouseLeftClick(mouse))
                return true;

            foreach (UIButton Button in Buttons)
                if (Button.AcceptsMouseEvent(mouse))
                    if (Button.MouseLeftClick(mouse))
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

        public override void Update(GameTime time)
        {
            base.Update(time);

            Recalculate();
        }

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

        protected override void DrawChildren(SpriteBatch CurrentBatch)
        {
            foreach (UIButton Button in Buttons)
            {
                if (Button.Visible)
                {
                    Button.Draw(CurrentBatch);
                }
            }

            base.DrawChildren(CurrentBatch);
        }

        #endregion

        #region Creation and Loading

        protected UIButton CreateButton(String Label)
        {
            UIButton Button = new UIButton(this);
            Button.Bounds.Width = 12;
            Button.Bounds.Height = 12;
            Button.Label = Label;
            return Button;
        }

        protected void AddDefultButtons()
        {
            UIButton CloseButton = CreateButton("X");
            AddButton(CloseButton);

            UIButton MinimizeButton = CreateButton("-");
            AddButton(MinimizeButton);
        }

        #endregion
    }
}
