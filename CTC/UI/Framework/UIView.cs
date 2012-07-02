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
    public class UIView
    {
        protected UIContext Context;
        protected UIView Parent;
        protected List<UIView> Children;

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
        public UIView(UIContext Context)
        {
            this.Context = Context;
            Children = new List<UIView>();
        }

        /// <summary>
        /// Base constructor for all UIPanels
        /// </summary>
        /// <param name="parent"></param>
        public UIView(UIView parent)
        {
            Parent = parent;
            UIView superParent = parent;
            while (superParent.Parent != null)
                superParent = superParent.Parent;
            Context = superParent.Context;

            ZOrder = Parent.ZOrder + 1;

            Children = new List<UIView>();

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

        public UIView AddSubview(UIView panel)
        {
            Children.Add(panel);
            return panel;
        }

        public UIView InsertSubview(int index, UIView panel)
        {
            Children.Insert(index, panel);
            return panel;
        }

        public void BringSubviewToFront(UIView view)
        {
            int oldIndex = Children.IndexOf(view);
            Children.RemoveAt(oldIndex);
            Children.Add(view);
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
            // We use a copy so that event handling can modify the list
            List<UIView> SubviewListCopy = new List<UIView>(Children);
            foreach (UIView subview in SubviewListCopy)
            {
                if (subview.AcceptsMouseEvent(mouse))
                    if (subview.MouseLeftClick(mouse))
                        return true;
            }
            return false;
        }

        public Vector2 ClientCoordinate(Vector2 coordiante)
        {
            return new Vector2(coordiante.X - ScreenBounds.X, coordiante.Y - ScreenBounds.Y);
        }

        public Vector2 ClientMouseCoordinate(MouseState mouse)
        {
            return ClientCoordinate(new Vector2(mouse.X, mouse.Y));
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
            foreach (UIView Child in Children)
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
            foreach (UIView panel in Children)
            {
                panel.Draw(CurrentBatch);
            }
        }

        #endregion
    }
}
