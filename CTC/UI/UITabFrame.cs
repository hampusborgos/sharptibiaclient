using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    public class UITab : UIButton
    {
        public UITab(UIPanel Parent, int Width)
            : base(Parent)
        {
            ElementType = UIElementType.Tab;
            Bounds = new Rectangle(0, 0, Width, 18);
        }
    }

    public class UITabFrame : UIPanel
    {
        public UITabFrame(UIPanel Parent)
            : base(Parent)
        {
            ElementType = UIElementType.None;
            TabWidth = 100;
        }

        #region Data Members

        protected List<UITab> Tabs = new List<UITab>();

        private int _TabWidth;
        public int TabWidth
        {
            get { return _TabWidth; }
            set
            {
                _TabWidth = value;
                foreach (UITab Tab in Tabs)
                {
                    Tab.Bounds = new Rectangle(Tab.Bounds.X, Tab.Bounds.Y, value, Tab.Bounds.Height);
                }
            }
        }

        #endregion

        public override void Update(GameTime time)
        {
            base.Update(time);

            int X = 20;
            foreach (UITab Tab in Tabs)
            {
                Tab.Bounds.X = X;
                X += Tab.Bounds.Width;
            }
        }

        protected override void DrawChildren(SpriteBatch CurrentBatch)
        {
            foreach (UITab Tab in Tabs)
            {
                Tab.Draw(CurrentBatch);
            }
            base.DrawChildren(CurrentBatch);
        }

        public UITab AddTab(String Label)
        {
            UITab Tab = new UITab(this, TabWidth);
            Tab.Label = Label;
            Tabs.Add(Tab);
            return Tab;
        }

        public UITab InsertTab(int index, String Label)
        {
            UITab Tab = AddTab(Label);
            Tabs.Remove(Tab);
            Tabs.Insert(index, Tab);
            return Tab;
        }
    }
}
