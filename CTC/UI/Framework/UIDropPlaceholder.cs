using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    /// <summary>
    /// A translucent stand-in for a window that is being dragged, sitting in the
    /// slot the window would drop into.
    ///
    /// It is a real view in the layout, not just something painted on top, so the
    /// other windows shuffle around it exactly as they will once the drag ends -
    /// what you see under the cursor is the result, not a guess at it.
    /// </summary>
    public class UIDropPlaceholder : UIView
    {
        /// <summary>
        /// Half opacity. SpriteBatch blends premultiplied, and multiplying a
        /// Color scales every channel including alpha, so this stays correct.
        /// </summary>
        private static readonly Color Ghost = Color.White * 0.5f;

        public UIDropPlaceholder(UIFrame Ghosted)
            : base(null, Ghosted.ElementType)
        {
            Bounds = new Rectangle(0, 0, Ghosted.Bounds.Width, Ghosted.Bounds.Height);
            Margin = Ghosted.Margin;

            // Has to match, or AddSubviewBefore refuses to place it among them.
            ZOrder = Ghosted.ZOrder;
        }

        protected override void DrawBackground(SpriteBatch CurrentBatch)
        {
            UIContext.Skin.DrawBackground(CurrentBatch, ElementType, ScreenBounds, Ghost);
        }

        protected override void DrawBorder(SpriteBatch CurrentBatch)
        {
            UIContext.Skin.DrawBox(CurrentBatch, ElementType, ScreenBounds, Ghost);
        }
    }
}
