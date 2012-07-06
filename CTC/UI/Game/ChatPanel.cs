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

        public ChatPanel()
        {
            AddTab("Default");
            AddTab("Game-Chat");
            AddTab("Hemmd");

            ChatLog = new UIVirtualFrame();
            ChatLog.ElementType = UIElementType.Window;
            ChatLog.ContentView.ElementType = UIElementType.BorderlessWindow;
            AddSubview(ChatLog);
        }

        #region Data Members

        ClientViewport Viewport;

        #endregion

        public void OnNewState(ClientViewport NewViewport)
        {
            Viewport = NewViewport;
        }

        public override void LayoutSubviews()
        {
            ChatLog.Bounds = ClientBounds;

            base.LayoutSubviews();
        }
    }
}
