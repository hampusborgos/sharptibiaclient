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
        private UIContext _Context;
        protected UIContext Context
        {
            get { return _Context; }
            set
            {
                _Context = value;
                foreach (UIView Subview in Children)
                    Subview.Context = value;

                NeedsLayout = true;
            }
        }
        protected List<UIView> Children = new List<UIView>();

        /// <summary>
        /// The superview of this view (ie. the view above it in the view hierarchy).
        /// </summary>
        public UIView Parent
        {
            get { return _Parent; }
        }
        private UIView _Parent;

        public UIElementType ElementType;
        protected SpriteBatch Batch;
        private Nullable<Rectangle> OldScissor;
        protected Boolean CropChildren = true;
        public Boolean Visible = true;
        public Boolean InteractionEnabled = true;
        public int ZOrder = 0;
        public String Tag;
        public Boolean NeedsLayout = true;
        public Boolean ClipsSubviews = false;

        /// <summary>
        /// User-customizable padding inside this view.
        /// </summary>
        public Margin Padding = new Margin();

        /// <summary>
        /// How much float is left around this view.
        /// </summary>
        public Margin Margin = new Margin();

        /// <summary>
        /// The position and size of this view inside it's parent view
        /// This is without any padding added by the skin / Padding property.
        /// It does not include margins.
        /// </summary>
        public Rectangle Bounds;

        /// <summary>
        /// The position part of the bounds.
        /// </summary>
        public Vector2 Position
        {
            get { return new Vector2(Bounds.X, Bounds.Y); }
            set { Bounds.X = (int)value.X; Bounds.Y = (int)value.Y; }
        }

        /// <summary>
        /// The size part of the bounds.
        /// </summary>
        public Vector2 Size
        {
            get { return new Vector2(Bounds.Width, Bounds.Height); }
            set { Bounds.Width = (int)value.X; Bounds.Height = (int)value.Y; }
        }

        /// <summary>
        /// This is how much padding the skin adds to the internal bounds of this view.
        /// </summary>
        public virtual Margin SkinPadding
        {
            get
            {
                if (Context == null)
                    return new Margin(0);

                return new Margin
                {
                    Top = (int)Context.Skin.Measure(ElementType, UISkinOrientation.Top).Y,
                    Left = (int)Context.Skin.Measure(ElementType, UISkinOrientation.Left).X,
                    Bottom = (int)Context.Skin.Measure(ElementType, UISkinOrientation.Bottom).Y,
                    Right = (int)Context.Skin.Measure(ElementType, UISkinOrientation.Right).X
                };
            }
        }

        /// <summary>
        /// A rectangle that represents that's available for content inside this view.
        /// </summary>
        public virtual Rectangle ClientBounds
        {
            get
            {
                Margin fromSkin = SkinPadding;
                return new Rectangle(
                    fromSkin.Left + Padding.Left,
                    fromSkin.Top + Padding.Top,
                    Bounds.Width - fromSkin.TotalWidth - Padding.TotalWidth,
                    Bounds.Height - fromSkin.TotalHeight - Padding.TotalHeight
                );
            }
        }

        /// <summary>
        /// The bounds of the view including the float area around it.
        /// (ie. the margin)
        /// </summary>
        public virtual Rectangle FullBounds
        {
            get
            {
                return new Rectangle(
                    Bounds.X + Margin.Left,
                    Bounds.Y + Margin.Top,
                    Bounds.Width + Margin.TotalWidth,
                    Bounds.Height + Margin.TotalHeight
                );
            }
        }

        /// <summary>
        /// The position and size of this view on the actual screen.
        /// </summary>
        public Rectangle ScreenBounds
        {
            get
            {
                Rectangle ParentBounds = new Rectangle(0, 0, 0, 0);
                if (Parent != null)
                    ParentBounds = Parent.ScreenBounds;
                return new Rectangle(
                    ParentBounds.X + Bounds.X,
                    ParentBounds.Y + Bounds.Y,
                    Bounds.Width,
                    Bounds.Height
                );
            }
        }

        /// <summary>
        /// The ClientBounds in the global coordinate space.
        /// This is the area available inside this view for content,
        /// that is, with padding and skin calculated into it.
        /// </summary>
        public virtual Rectangle ScreenClientBounds
        {
            get
            {
                Rectangle sb = this.ScreenBounds;
                Rectangle cb = this.ClientBounds;
                return new Rectangle(
                    sb.X + cb.Left,
                    sb.Y + cb.Top,
                    cb.Width,
                    cb.Height
                );
            }
        }


        #region Creation

        /// <summary>
        /// Constructor for UIPanel without parent (Only applicable for the top frame)
        /// </summary>
        /// <param name="Context"></param>
        public UIView(UIContext Context)
        {
            ElementType = UIElementType.Window;
            this.Context = Context;
        }

        /// <summary>
        /// Base constructor for all UIPanels
        /// </summary>
        /// <param name="Frame">The size and position of the element </param>
        /// <param name="ElementType">What style type this element should use.</param>
        public UIView(Rectangle? Frame = null, UIElementType ElementType = UIElementType.Window)
        {
            Bounds = Frame ?? new Rectangle(0, 0, 0, 0);
            this.ElementType = ElementType;
        }

        #endregion

        #region Subview Management

        public virtual List<UIView> Subviews
        {
            get
            {
                return new List<UIView>(Children);
            }
        }
        
        public List<T> SubviewsOfType<T>() where T : UIView
        {
            List<T> views = new List<T>();
            foreach (UIView Subview in Subviews)
                if (Subview.GetType() == typeof(T))
                    views.Add((T)Subview);

            return views;
        }

        public virtual UIView GetSubviewWithTag(String SearchTag)
        {
            foreach (UIView Subview in Subviews)
                if (Subview.Tag == SearchTag)
                    return Subview;
            return null;
        }

        private void AddSubviewInternal(UIView newView)
        {
            int i = Children.FindIndex(delegate(UIView subview)
            {
                return subview.ZOrder > newView.ZOrder;
            });
            if (i == -1)
                Children.Add(newView);
            else
                Children.Insert(i, newView);

            // Inform it it's a parent of us
            newView._Parent = this;
            newView.Context = Context;

            // We will need re-layout now...
            NeedsLayout = true;
        }

        public virtual UIView AddSubview(UIView newView)
        {
            if (newView.Parent != null)
                throw new UIException("Remove the view from it's own superview first.");

            AddSubviewInternal(newView);

            return newView;
        }

        public virtual void RemoveSubview(UIView subview)
        {
            Children.Remove(subview);
        }

        public void RemoveSubviewsMatching(Predicate<UIView> match)
        {
            List<UIView> matches = Children.FindAll(match);
            foreach (UIView view in matches)
                RemoveSubview(view);
        }

        public void RemoveAllSubviews()
        {
            foreach (UIView view in Subviews)
                RemoveSubview(view);
        }

        public void RemoveFromSuperview()
        {
            Parent.RemoveSubview(this);
        }

        public void BringSubviewToFront(UIView view)
        {
            int oldIndex = Children.IndexOf(view);
            Children.RemoveAt(oldIndex);

            AddSubviewInternal(view);
        }

        public virtual void LayoutSubviews()
        {
            NeedsLayout = false;

            foreach (UIView Subview in Subviews)
                Subview.LayoutSubviews();
        }

        #endregion

        #region Events

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

        #endregion

        public Vector2 ClientCoordinate(Vector2 coordinate)
        {
            return new Vector2(
                coordinate.X - ScreenClientBounds.X - Padding.Left,
                coordinate.Y - ScreenClientBounds.Y - Padding.Top
            );
        }

        public Vector2 ScreenCoordinate(Vector2 coordinate)
        {
            return ScreenCoordinate((int)coordinate.X, (int)coordinate.Y);
        }

        public Rectangle ScreenCoordinate(Rectangle rect)
        {
            return new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public Rectangle ScreenCoordinate(int X, int Y, int W, int H)
        {
            return new Rectangle(
                ScreenClientBounds.Left + X,
                ScreenClientBounds.Top + Y,
                W,
                H
            );
        }

        public Vector2 ScreenCoordinate(int X, int Y)
        {
            return new Vector2(
                ScreenClientBounds.Left + X,
                ScreenClientBounds.Top + Y
            );
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
            if (NeedsLayout)
                LayoutSubviews();

            foreach (UIView Child in Children)
                Child.Update(time);
        }

        #region Drawing

        protected Rectangle GetClipRectangle()
        {
            Rectangle Screen = Context.Window.ClientBounds;
            Rectangle clip = ScreenBounds;
            if (Context.ScissorStack.Count > 0)
            {
                Rectangle pclip = Context.ScissorStack.Peek();
                clip = new Rectangle()
                {
                    X = Math.Max(0, Math.Max(pclip.Left, clip.Left)),
                    Y = Math.Max(0, Math.Max(pclip.Top, clip.Top)),
                    Width = Math.Min(Screen.Right, Math.Min(pclip.Right, clip.Right)),
                    Height = Math.Min(Screen.Bottom, Math.Min(pclip.Bottom, clip.Bottom)),
                };
            }
            else
            {
                clip = new Rectangle()
                {
                    X = Math.Max(0, clip.Left),
                    Y = Math.Max(0, clip.Top),
                    Width = Math.Min(Screen.Right, clip.Right),
                    Height = Math.Min(Screen.Bottom, clip.Bottom),
                };
            }

            // Scissor is not specified with right, bottom
            // so remove X, Y from them
            clip.Width -= clip.X;
            clip.Height -= clip.Y;

            // Make sure we don't go offscreen
            if (clip.Right > Screen.Width)
                clip.Width = Screen.Width - clip.X;
            if (clip.Bottom > Screen.Height)
                clip.Height = Screen.Height - clip.Y;

            // TODO: Skip render phase?
            if (clip.Height < 0)
                clip.Height = 0;
            if (clip.Width < 0)
                clip.Width = 0;
            
            return clip;
        }

        protected virtual void BeginDraw()
        {
            // Create the batch if this is the first time we're being drawn
            if (Batch == null)
                Batch = new SpriteBatch(Context.Graphics.GraphicsDevice);

            // Begin the batch
            Batch.Begin(SpriteSortMode.Deferred, null, null, null, Context.Rasterizer);

            // Set up the scissors for this view
            Rectangle Screen = Context.Window.ClientBounds;
            Screen.X = 0;
            Screen.Y = 0;
            if (Screen.Intersects(ScreenBounds))
            {
                OldScissor = Batch.GraphicsDevice.ScissorRectangle;
                Batch.GraphicsDevice.ScissorRectangle = GetClipRectangle();
            }
            else
                OldScissor = null;
        }

        protected virtual void EndDraw()
        {
            Batch.End();

            // Reset the scissors
            if (OldScissor != null)
                Batch.GraphicsDevice.ScissorRectangle = OldScissor.Value;
        }

        /// <summary>
        /// Draws entire content of the panel, including children
        /// </summary>
        /// <param name="CurrentBatch"></param>
        public virtual void Draw(SpriteBatch CurrentBatch, Rectangle BoundingBox)
        {
            if (!BoundingBox.Intersects(Bounds))
                return;
            BeginDraw();

            DrawBackground(Batch);
            DrawContent(Batch);
            EndDraw();

            if (ClipsSubviews)
                Context.ScissorStack.Push(ScreenBounds);

            DrawChildren(Batch, BoundingBox);

            if (ClipsSubviews)
                Context.ScissorStack.Pop();
            
            BeginDraw();
            DrawBorder(Batch);
            EndDraw();
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
        /// <param name="BoundingBox">If the subview does not intersect this area, it won't be drawn.</param>
        protected virtual void DrawChildren(SpriteBatch CurrentBatch, Rectangle BoundingBox)
        {
            foreach (UIView Subview in Children)
                Subview.Draw(CurrentBatch, BoundingBox);
        }

        #endregion
    }
}
