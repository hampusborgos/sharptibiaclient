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
        public UITab(int Width)
        {
            NormalType = UIElementType.Tab;
            HighlightType = UIElementType.TabHighlight;
            Bounds = new Rectangle(0, 0, Width, 18);
        }
    }

    public class UITabFrame : UIView
    {
        public UITabFrame()
        {
            ElementType = UIElementType.None;
            TabWidth = 100;
        }

        #region Data Members

        public override Margin SkinPadding
        {
            get
            {
                Margin m = base.SkinPadding;
                m.Top += 18;
                return m;
            }
        }

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

            int X = 18;
            foreach (UITab Tab in Tabs)
            {
                Tab.Bounds.X = X;
                X += Tab.Bounds.Width;
            }
        }

        public override void RemoveSubview(UIView Subview)
        {
            if (Subview is UITab)
                Tabs.Remove((UITab)Subview);
            base.RemoveSubview(Subview);
        }

        public UITab AddTab(String Label)
        {
            UITab Tab = new UITab(TabWidth);
            Tab.Label = Label;
            Tabs.Add(Tab);
            AddSubview(Tab);
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
