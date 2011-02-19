using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    public class ChatPanel : UITabFrame
    {
        UIVirtualFrame ChatLog;

        public ChatPanel(UIPanel Parent)
            : base(Parent)
        {
            AddTab("Default");
            AddTab("Game-Chat");
            AddTab("Hemmd");

            ChatLog = new UIVirtualFrame(this);
            ChatLog.ElementType = UIElementType.Window;
            ChatLog.Bounds = new Rectangle(0, 18, 800, 140);
            AddChildPanel(ChatLog);
        }

        #region Data Members

        ClientViewport Viewport;

        #endregion

        protected override void DrawContent(SpriteBatch CurrentBatch)
        {
            ;
        }

        public void OnNewState(ClientState NewState)
        {
            Viewport = NewState.Viewport;
        }
    }
}
