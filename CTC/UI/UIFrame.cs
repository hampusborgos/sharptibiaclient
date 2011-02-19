using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    public class UIFrame : UIPanel
    {
        public UIFrame(UIPanel Parent)
            : base(Parent)
        {
            ElementType = UIElementType.Frame;
            Name = "UIFrame";

            AddDefultButtons();
        }

        protected List<UIButton> Buttons = new List<UIButton>();
        public String Name;

        public void AddButton(UIButton Button)
        {
            Buttons.Add(Button);
        }

        public override void Update(GameTime time)
        {
            base.Update(time);

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

        protected override void DrawBorder(SpriteBatch CurrentBatch)
        {
            base.DrawBorder(CurrentBatch);

            Vector2 pos = new Vector2(ScreenBounds.X + 5, ScreenBounds.Y + 2);

            CurrentBatch.DrawString(
                Context.StandardFont, Name, pos,
                Color.LightGray,
                0.0f, new Vector2(0.0f, 0.0f),
                1.0f, SpriteEffects.None, 0.5f);
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

        #region Creation and Loading

        protected UIButton CreateButton()
        {
            UIButton Button = new UIButton(this);
            Button.Bounds.Width = 12;
            Button.Bounds.Height = 12;
            return Button;
        }

        protected void AddDefultButtons()
        {
            UIButton CloseButton = CreateButton();
            AddButton(CloseButton);

            UIButton MinimizeButton = CreateButton();
            AddButton(MinimizeButton);
        }

        #endregion
    }
}
