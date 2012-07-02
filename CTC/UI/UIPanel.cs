using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace CTC
{
    public class UIPanel
    {
        protected UIContext Context;
        protected UIPanel Parent;
        protected List<UIPanel> Children;

        public UIElementType ElementType = UIElementType.Window;
        protected SpriteBatch Batch;
        private Nullable<Rectangle> OldScissor;
        protected Boolean CropChildren = true;
        public Boolean Visible = true;
        public Boolean InteractionEnabled = true;
        public int ZOrder = 0;


        /// <summary>
        /// Constructor for UIPanel without parent (Only applicable for the top frame)
        /// </summary>
        /// <param name="Context"></param>
        public UIPanel(UIContext Context)
        {
            this.Context = Context;
            Children = new List<UIPanel>();
        }

        /// <summary>
        /// Base constructor for all UIPanels
        /// </summary>
        /// <param name="parent"></param>
        public UIPanel(UIPanel parent)
        {
            Parent = parent;
            UIPanel superParent = parent;
            while (superParent.Parent != null)
                superParent = superParent.Parent;
            Context = superParent.Context;

            ZOrder = Parent.ZOrder + 1;

            Children = new List<UIPanel>();

            Batch = new SpriteBatch(Context.Graphics.GraphicsDevice);
        }

        public virtual Rectangle ClientBounds
        {
            get
            {
                int left = (int)Context.Skin.Measure(ElementType, UISkinOrientation.Left).X;
                int top = (int)Context.Skin.Measure(ElementType, UISkinOrientation.Top).Y;
                int right = (int)Context.Skin.Measure(ElementType, UISkinOrientation.Right).X;
                int bottom = (int)Context.Skin.Measure(ElementType, UISkinOrientation.Bottom).Y;
                return new Rectangle(
                    left,
                    top,
                    Bounds.Width - left - right,
                    Bounds.Height - top - bottom
                );
            }
        }

        public Rectangle ScreenBounds
        {
            get
            {
                Rectangle ParentBounds = new Rectangle(0, 0, 0, 0);
                if (Parent != null)
                    ParentBounds = Parent.ScreenBounds;
                return new Rectangle(ParentBounds.X + Bounds.X, ParentBounds.Y + Bounds.Y, Bounds.Width, Bounds.Height);
            }
        }

        public Rectangle Bounds;

        public virtual Rectangle ScreenClientBounds
        {
            get
            {
                Rectangle sb = this.ScreenBounds;
                Rectangle cb = this.ClientBounds;
                return new Rectangle(
                    sb.X + cb.X,
                    sb.Y + cb.Y,
                    cb.Width,
                    cb.Height
                );
            }
        }

        public UIPanel AddChildPanel(UIPanel panel)
        {
            Children.Add(panel);
            return panel;
        }

        public UIPanel InsertChildPanel(int index, UIPanel panel)
        {
            Children.Insert(index, panel);
            return panel;
        }

        public bool CaptureMouse()
        {
            if (Context.MouseFocusedPanel != null)
            {
                // Ask the old panel to relinquish control
                if (!Context.MouseFocusedPanel.MouseLost())
                    // If it didn't want to, we couldn't capture the mouse
                    return false;
            }
            Context.MouseFocusedPanel = this;
            MouseCaptured();
            return true;
        }

        public void ReleaseMouse()
        {
            // If you call ReleaseMouse, the panel *will* lose mouse focus
            // If it fails to do so, something bad is going on.
            Debug.Assert(MouseLost(), "Did not release mouse when attempting to do so by itself.");
            Context.MouseFocusedPanel = null;
        }

        public virtual void MouseCaptured()
        {
        }

        public virtual bool MouseLost()
        {
            return true;
        }

        public virtual bool MouseMove(MouseState mouse)
        {
            return false;
        }

        public virtual bool MouseLeftClick(MouseState mouse)
        {
            foreach (UIPanel Child in Children)
            {
                if (Child.AcceptsMouseEvent(mouse))
                    if (Child.MouseLeftClick(mouse))
                        return true;
            }

            return false;
        }

        public bool AcceptsMouseEvent(MouseState mouse)
        {
            if (!Visible)
                return false;
            if (!InteractionEnabled)
                return false;
            if (!ScreenBounds.Contains(new Point(mouse.X, mouse.Y)))
                return false;
            return true;
        }

        public virtual void Update(GameTime time)
        {
            foreach (UIPanel Child in Children)
            {
                Child.Update(time);
            }
        }

        #region Drawing

        protected void BeginDraw()
        {
            Batch.Begin(SpriteSortMode.Deferred, null, null, null, Context.Rasterizer);

            Rectangle Screen = Context.Window.ClientBounds;
            Screen.X = 0;
            Screen.Y = 0;
            if (Screen.Intersects(ScreenBounds))
            {
                OldScissor = Batch.GraphicsDevice.ScissorRectangle;
                Rectangle clip = new Rectangle()
                {
                    X = Math.Max(Screen.Left, ScreenBounds.Left),
                    Y = Math.Max(Screen.Top, ScreenBounds.Top),
                    Width = Math.Min(Screen.Right, ScreenBounds.Right),
                    Height = Math.Min(Screen.Bottom, ScreenBounds.Bottom),
                };
                clip.Width -= clip.X;
                clip.Height -= clip.Y;
                if (clip.Right > Screen.Width)
                    clip.Width = Screen.Width - clip.X;
                if (clip.Bottom > Screen.Height)
                    clip.Height = Screen.Height - clip.Y;
                Batch.GraphicsDevice.ScissorRectangle = clip;
            }
            else
                OldScissor = null;
        }

        protected void EndDraw()
        {
            Batch.End();

            if (OldScissor != null)
                Batch.GraphicsDevice.ScissorRectangle = OldScissor.Value;
        }

        /// <summary>
        /// Draws entire content of the panel, including children
        /// </summary>
        /// <param name="CurrentBatch"></param>
        public virtual void Draw(SpriteBatch CurrentBatch)
        {
            BeginDraw();

            DrawBackground(Batch);
            DrawContent(Batch);
            DrawBorder(Batch);

            EndDraw();

            DrawChildren(Batch);
        }

        /// <summary>
        /// Draws the actual content of this panel
        /// </summary>
        /// <param name="CurrentBatch"></param>
        protected virtual void DrawContent(SpriteBatch CurrentBatch)
        {
        }

        /// <summary>
        /// Draws the background of the panel (no borders)
        /// </summary>
        /// <param name="CurrentBatch"></param>
        protected void DrawBackground(SpriteBatch CurrentBatch)
        {
            Context.Skin.DrawBackground(CurrentBatch, ElementType, ScreenBounds);
        }

        /// <summary>
        /// Draws the border of the panel
        /// </summary>
        /// <param name="CurrentBatch"></param>
        protected virtual void DrawBorder(SpriteBatch CurrentBatch)
        {
            Context.Skin.DrawBox(CurrentBatch, ElementType, ScreenBounds);
        }

        /// <summary>
        /// Draws the children of the panel
        /// </summary>
        /// <param name="CurrentBatch"></param>
        protected virtual void DrawChildren(SpriteBatch CurrentBatch)
        {
            foreach (UIPanel panel in Children)
            {
                panel.Draw(CurrentBatch);
            }
        }

        #endregion
    }
}
