using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    public class TopTaskbar : UIPanel
    {
        public TopTaskbar(UIPanel Parent)
            : base(Parent)
        {
            Bounds = new Rectangle(0, 0, 20, 20);

            UIButton StartButton = new UIButton(this);
            StartButton.Bounds = new Rectangle(0, 0, 20, 20);
            AddChildPanel(StartButton);
        }

        public override void Draw(SpriteBatch CurrentBatch)
        {
            DrawChildren(CurrentBatch);
        }
    }
}
