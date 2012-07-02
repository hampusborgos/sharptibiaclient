using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    public enum UIElementType
    {
        None,
        Frame,
        Window,
        Button,
        Textbox,
        Tab,
        TabHighlight,
        ButtonHighlight,
        InventorySlot,
        Checkbox,
        ScrollbarGem,
        ScrollbarBackground,
        ScrollbarTop,
        ScrollbarTopHighlight,
        ScrollbarBottom,
        ScrollbarBottomHighlight,
    };

    public enum UISkinOrientation
    {
        TopLeft,
        Top,
        TopRight,
        Left,
        Center,
        Right,
        BottomLeft,
        Bottom,
        BottomRight
    };

    public class UISkinElement
    {
        public UIElementType Type;
        private Rectangle[] Box = new Rectangle[9];

        public UISkinElement(UIElementType e)
        {
            Type = e;
        }

        public void Set(UISkinOrientation o, Rectangle r)
        {
            Box[(int)o] = r;
        }

        public Rectangle this [UISkinOrientation o]
        {
            get
            {
                return Box[(int)o];
            }
        }
    };

    public class UISkin
    {
        UIContext Context;
        Dictionary<UIElementType, UISkinElement> Types = new Dictionary<UIElementType, UISkinElement>();

        Texture2D UISheet;

        public UISkin(UIContext Context)
        {
            this.Context = Context;
            UISheet = Context.Content.Load<Texture2D>("DefaultSkin");
        }

        public void AddElement(UISkinElement e)
        {
            Types[e.Type] = e;
        }

        public Vector2 Measure(UIElementType type, UISkinOrientation orientation)
        {
            if (!Types.ContainsKey(type))
                return new Vector2(0, 0);
            UISkinElement e = Types[type];
            return new Vector2(e[orientation].Width, e[orientation].Height);
        }

        public void DrawBackground(SpriteBatch Batch, UIElementType type, Rectangle rect)
        {
            if (!Types.ContainsKey(type))
                return;
            UISkinElement e = Types[type];
            // Draw center square
            // Do this first since other stuff might overlap
            Vector2 pos = new Vector2(rect.X + e[UISkinOrientation.TopLeft].Width, rect.Y + e[UISkinOrientation.TopLeft].Height);
            if (e[UISkinOrientation.Center].Width > 0 && e[UISkinOrientation.Center].Height > 0)
            {
                while (pos.Y < rect.Bottom)
                {
                    pos.X = rect.X + e[UISkinOrientation.TopLeft].Width;
                    while (pos.X < rect.Right)
                    {
                        Batch.Draw(UISheet, pos, e[UISkinOrientation.Center], Color.White);
                        pos.X += e[UISkinOrientation.Center].Width;
                    }
                    pos.Y += e[UISkinOrientation.Center].Height;
                }
            }
        }

        public void DrawBox(SpriteBatch Batch, UIElementType type, Rectangle rect)
        {
            if (!Types.ContainsKey(type))
                return;
            UISkinElement e = Types[type];

            Vector2 pos;

            // Draw top left
            if (e[UISkinOrientation.TopLeft].Width > 0 && e[UISkinOrientation.TopLeft].Height > 0)
            {
                pos = new Vector2(rect.X, rect.Y);
                Batch.Draw(UISheet, pos, e[UISkinOrientation.TopLeft], Color.White);
            }
            
            // Draw top
            if (e[UISkinOrientation.Top].Width > 0 && e[UISkinOrientation.Top].Height > 0)
            {
                pos = new Vector2(rect.X + e[UISkinOrientation.TopLeft].Width, rect.Y); 
                while (pos.X < rect.Right)
                {
                    Batch.Draw(UISheet, pos, e[UISkinOrientation.Top], Color.White);
                    pos.X += e[UISkinOrientation.Top].Width;
                }
            }

            // Draw top right
            if (e[UISkinOrientation.TopRight].Width > 0 && e[UISkinOrientation.TopRight].Height > 0)
            {
                pos = new Vector2(rect.Right - e[UISkinOrientation.TopRight].Width, rect.Y);
                Batch.Draw(UISheet, pos, e[UISkinOrientation.TopRight], Color.White);
            }

            // Draw center left
            if (e[UISkinOrientation.Left].Width > 0 && e[UISkinOrientation.Left].Height > 0)
            {
                pos = new Vector2(rect.X, rect.Y + e[UISkinOrientation.TopLeft].Height);
                while (pos.Y < rect.Bottom)
                {
                    Batch.Draw(UISheet, pos, e[UISkinOrientation.Left], Color.White);
                    pos.Y += e[UISkinOrientation.Left].Height;
                }
            }

            // Draw center right
            if (e[UISkinOrientation.Right].Width > 0 && e[UISkinOrientation.Right].Height > 0)
            {
                pos = new Vector2(rect.Right - e[UISkinOrientation.Right].Width, rect.Y + e[UISkinOrientation.TopRight].Height);
                while (pos.Y < rect.Bottom)
                {
                    Batch.Draw(UISheet, pos, e[UISkinOrientation.Right], Color.White);
                    pos.Y += e[UISkinOrientation.Right].Height;
                }
            }

            // Draw bottom left
            if (e[UISkinOrientation.BottomLeft].Width > 0 && e[UISkinOrientation.BottomLeft].Height > 0)
            {
                pos = new Vector2(rect.X, rect.Bottom - e[UISkinOrientation.BottomLeft].Height);
                Batch.Draw(UISheet, pos, e[UISkinOrientation.BottomLeft], Color.White);
            }

            // Draw bottom
            if (e[UISkinOrientation.Bottom].Width > 0 && e[UISkinOrientation.Bottom].Height > 0)
            {
                pos = new Vector2(rect.X + e[UISkinOrientation.BottomLeft].Width, rect.Bottom - e[UISkinOrientation.Bottom].Height);
                while (pos.X < rect.Right)
                {
                    Batch.Draw(UISheet, pos, e[UISkinOrientation.Bottom], Color.White);
                    pos.X += e[UISkinOrientation.Bottom].Width;
                }
            }

            // Draw bottom right
            if (e[UISkinOrientation.BottomRight].Width > 0 && e[UISkinOrientation.BottomRight].Height > 0)
            {
                pos = new Vector2(rect.Right - e[UISkinOrientation.BottomRight].Width, rect.Bottom - e[UISkinOrientation.BottomRight].Height);
                Batch.Draw(UISheet, pos, e[UISkinOrientation.BottomRight], Color.White);
            }
        }
    
        public void Load(System.IO.Stream File)
        {
            UISkinElement e;

            e = new UISkinElement(UIElementType.Frame);
            e.Set(UISkinOrientation.TopLeft,        new Rectangle(106,       183,    4,      17));
            e.Set(UISkinOrientation.Top,            new Rectangle(114,       183,    96,     17));
            e.Set(UISkinOrientation.TopRight,       new Rectangle(110,       183,    4,      17));
            e.Set(UISkinOrientation.Left,           new Rectangle(256,       0,      4,      96));
            e.Set(UISkinOrientation.Center,         new Rectangle(0,	        0,		96,		96));
            e.Set(UISkinOrientation.Right,          new Rectangle(260,	    0,	    4,		96));
            e.Set(UISkinOrientation.BottomLeft,     new Rectangle(98,        193,	4,		4));
            e.Set(UISkinOrientation.Bottom,         new Rectangle(2,	        193,	96,		4));
            e.Set(UISkinOrientation.BottomRight,    new Rectangle(102,       193,	4,		4));
            AddElement(e);

            e = new UISkinElement(UIElementType.Window);
            e.Set(UISkinOrientation.TopLeft,        new Rectangle(106,      184,    4,      3));
            e.Set(UISkinOrientation.Top,            new Rectangle(2,        211,    96,     3));
            e.Set(UISkinOrientation.TopRight,       new Rectangle(110,      184,    4,      3));
            e.Set(UISkinOrientation.Left,           new Rectangle(256,      0,      4,      96));
            e.Set(UISkinOrientation.Center,         new Rectangle(0,        0,      96,     96));
            e.Set(UISkinOrientation.Right,          new Rectangle(260,      0,      4,      96));
            e.Set(UISkinOrientation.BottomLeft,     new Rectangle(98,       193,    4,      4));
            e.Set(UISkinOrientation.Bottom,         new Rectangle(2,        193,    96,     4));
            e.Set(UISkinOrientation.BottomRight,    new Rectangle(102,      193,    4,      4));
            AddElement(e);

            e = new UISkinElement(UIElementType.Button);
            e.Set(UISkinOrientation.TopLeft,        new Rectangle(174,	    138,	1,		1));
            e.Set(UISkinOrientation.Top,            new Rectangle(175,       138,    32,     1));
            e.Set(UISkinOrientation.TopRight,       new Rectangle(207,	    138,	1,		1));
            e.Set(UISkinOrientation.Left,           new Rectangle(174,	    139,	1,		18));
            e.Set(UISkinOrientation.Center,         new Rectangle(175,	    139,	32,		18));
            e.Set(UISkinOrientation.Right,          new Rectangle(207,	    139,	1,		18));
            e.Set(UISkinOrientation.BottomLeft,     new Rectangle(174,	    157,	1,		1));
            e.Set(UISkinOrientation.Bottom,         new Rectangle(175,	    157,	32,		1));
            e.Set(UISkinOrientation.BottomRight,    new Rectangle(207,	    157,	1,		1));
            AddElement(e);

            e = new UISkinElement(UIElementType.TabHighlight);
            e.Set(UISkinOrientation.TopLeft,        new Rectangle(114,      200,    2,      2));
            e.Set(UISkinOrientation.Top,            new Rectangle(116,      200,    91,     2));
            e.Set(UISkinOrientation.TopRight,       new Rectangle(208,      200,    2,      2));
            e.Set(UISkinOrientation.Left,           new Rectangle(114,      202,    2,      16));
            e.Set(UISkinOrientation.Center,         new Rectangle(116,      202,    91,     16));
            e.Set(UISkinOrientation.Right,          new Rectangle(208,      202,    2,      16));
            //e.Set(UISkinOrientation.BottomLeft,     new Rectangle(98,       193,    4,      4));
            //e.Set(UISkinOrientation.Bottom,         new Rectangle(2,        193,    96,     4));
            //e.Set(UISkinOrientation.BottomRight,    new Rectangle(102,      193,    4,      4));
            AddElement(e);

            e = new UISkinElement(UIElementType.Tab);
            e.Set(UISkinOrientation.TopLeft,        new Rectangle(210,      200,    2,      2));
            e.Set(UISkinOrientation.Top,            new Rectangle(212,      200,    91,     2));
            e.Set(UISkinOrientation.TopRight,       new Rectangle(304,      200,    2,      2));
            e.Set(UISkinOrientation.Left,           new Rectangle(210,      202,    2,      14));
            e.Set(UISkinOrientation.Center,         new Rectangle(212,      202,    91,     14));
            e.Set(UISkinOrientation.Right,          new Rectangle(304,      202,    2,      14));
            //e.Set(UISkinOrientation.BottomLeft,     new Rectangle(98,       193,    4,      4));
            //e.Set(UISkinOrientation.Bottom,         new Rectangle(2,        193,    96,     4));
            //e.Set(UISkinOrientation.BottomRight,    new Rectangle(102,      193,    4,      4));
            AddElement(e);

            e = new UISkinElement(UIElementType.ButtonHighlight);
            e.Set(UISkinOrientation.TopLeft,        new Rectangle(174,	158,	1,		1));
            e.Set(UISkinOrientation.Top,            new Rectangle(175,	158,	32,		1));
            e.Set(UISkinOrientation.TopRight,       new Rectangle(207,	158,	1,		1));
            e.Set(UISkinOrientation.Left,           new Rectangle(174,	159,	1,		18));
            e.Set(UISkinOrientation.Center,         new Rectangle(175,	159,	32,		18));
            e.Set(UISkinOrientation.Right,          new Rectangle(207,	159,	1,		18));
            e.Set(UISkinOrientation.BottomLeft,     new Rectangle(174,	177,	1,		1));
            e.Set(UISkinOrientation.Bottom,         new Rectangle(175,	177,	32,		1));
            e.Set(UISkinOrientation.BottomRight,    new Rectangle(207,	177,	1,		1));
            AddElement(e);


            // FIXME (ivucica#4#) looks like the "unchecked checkbox" is not the same as "textbox" -- see: "XPlike Tibia.pic" for more info
            e = new UISkinElement(UIElementType.Textbox);
            e.Set(UISkinOrientation.TopLeft,        new Rectangle(308,	96,		1,		1)); // in fact it looks like we have to HARDCODE this piece of skin.
            e.Set(UISkinOrientation.Top,            new Rectangle(309,	96,		10,		1));
            e.Set(UISkinOrientation.TopRight,       new Rectangle(319,	96,		1,		1));
            e.Set(UISkinOrientation.Left,           new Rectangle(308,	97,		1,		10));
            e.Set(UISkinOrientation.Center,         new Rectangle(309,	97,		10,		10));
            e.Set(UISkinOrientation.Right,          new Rectangle(319,	97,		1,		10));
            e.Set(UISkinOrientation.BottomLeft,     new Rectangle(308,	107,	1,		1));
            e.Set(UISkinOrientation.Bottom,         new Rectangle(309,	107,	10,		1));
            e.Set(UISkinOrientation.BottomRight,    new Rectangle(319,	107,	1,		1));
            AddElement(e);
            /*
            e = new UISkinElement(UIElementType.Window);
            e.Set(UISkinOrientation.TopLeft,       new Rectangle(308,	108,	1,		1));
            e.Set(UISkinOrientation.Top,           new Rectangle(309,	108,	10,		1));
            e.Set(UISkinOrientation.TopRight,      new Rectangle(319,	108,	1,		1));
            e.Set(UISkinOrientation.Left,          new Rectangle(308,	109,	1,		10));
            e.Set(UISkinOrientation.Center,        new Rectangle(309,	109,	10,		10));
            e.Set(UISkinOrientation.Right,         new Rectangle(319,	109,	1,		10));
            e.Set(UISkinOrientation.BottomLeft,    new Rectangle(308,	119,	1,		1));
            e.Set(UISkinOrientation.Bottom,        new Rectangle(309,	119,	10,		1));
            e.Set(UISkinOrientation.BottomRight,   new Rectangle(319,	119,	1,		1));
            AddElement(e);


            e = new UISkinElement(UIElementType.Window);
            e.Set(UISkinOrientation.TopLeft,       new Rectangle(44,		226,	5,		5));
            e.Set(UISkinOrientation.Top,           new Rectangle(43,		214,	32,		5));
            e.Set(UISkinOrientation.TopRight,      new Rectangle(49,		226,	5,		5));
            e.Set(UISkinOrientation.Left,          new Rectangle(0,		    214,	5,		32));
            e.Set(UISkinOrientation.Center,        new Rectangle(11,		214,	32,		32));
            e.Set(UISkinOrientation.Right,         new Rectangle(6,		    214,	5,		32));
            e.Set(UISkinOrientation.BottomLeft,    new Rectangle(44,		231,	5,		5));
            e.Set(UISkinOrientation.Bottom,        new Rectangle(43,		219,	32,		5));
            e.Set(UISkinOrientation.BottomRight,   new Rectangle(49,		231,	5,		5));
            AddElement(e);
            */
            e = new UISkinElement(UIElementType.InventorySlot);
            e.Set(UISkinOrientation.TopLeft,        new Rectangle(186,	64, 	1,		1));
            e.Set(UISkinOrientation.Top,            new Rectangle(187,	64, 	31,		1));
            e.Set(UISkinOrientation.TopRight,       new Rectangle(219,	64, 	1,		1));
            e.Set(UISkinOrientation.Left,           new Rectangle(186,	65,	    1,		31));
            e.Set(UISkinOrientation.Center,         new Rectangle(187,	65,	    31,		31));
            e.Set(UISkinOrientation.Right,          new Rectangle(219,	65,	    1,		31));
            e.Set(UISkinOrientation.BottomLeft,     new Rectangle(186,	97, 	1,		1));
            e.Set(UISkinOrientation.Bottom,         new Rectangle(187,	97,	    31,		1));
            e.Set(UISkinOrientation.BottomRight,    new Rectangle(219,	97,	    1,		1));
            AddElement(e);

            e = new UISkinElement(UIElementType.ScrollbarTop);
            //e.Set(UISkinOrientation.TopLeft,       null);
            //e.Set(UISkinOrientation.Top,           null);
            //e.Set(UISkinOrientation.TopRight,      null);
            //e.Set(UISkinOrientation.Left,          null);
            e.Set(UISkinOrientation.Center,         new Rectangle(232,	64,	    12,		12));
            //e.Set(UISkinOrientation.Right,         null);
            //e.Set(UISkinOrientation.BottomLeft,    null);
            //e.Set(UISkinOrientation.Bottom,        null);
            //e.Set(UISkinOrientation.BottomRight,   null);
            AddElement(e);

            e = new UISkinElement(UIElementType.ScrollbarTopHighlight);
            //e.Set(UISkinOrientation.TopLeft,       null);
            //e.Set(UISkinOrientation.Top,           null);
            //e.Set(UISkinOrientation.TopRight,      null);
            //e.Set(UISkinOrientation.Left,          null);
            e.Set(UISkinOrientation.Center,         new Rectangle(234,	122,    12,		12));
            //e.Set(UISkinOrientation.Right,         null);
            //e.Set(UISkinOrientation.BottomLeft,    null);
            //e.Set(UISkinOrientation.Bottom,        null);
            //e.Set(UISkinOrientation.BottomRight,   null);
            AddElement(e);

            e = new UISkinElement(UIElementType.ScrollbarBottom);
            //e.Set(UISkinOrientation.TopLeft,       null);
            //e.Set(UISkinOrientation.Top,           null);
            //e.Set(UISkinOrientation.TopRight,      null);
            //e.Set(UISkinOrientation.Left,          null);
            e.Set(UISkinOrientation.Center,         new Rectangle(244,	64,	    12,		12));
            //e.Set(UISkinOrientation.Right,         null);
            //e.Set(UISkinOrientation.BottomLeft,    null);
            //e.Set(UISkinOrientation.Bottom,        null);
            //e.Set(UISkinOrientation.BottomRight,   null);
            AddElement(e);

            e = new UISkinElement(UIElementType.ScrollbarBottomHighlight);
            //e.Set(UISkinOrientation.TopLeft,       null);
            //e.Set(UISkinOrientation.Top,           null);
            //e.Set(UISkinOrientation.TopRight,      null);
            //e.Set(UISkinOrientation.Left,          null);
            e.Set(UISkinOrientation.Center,         new Rectangle(246,	122,    12,		12));
            //e.Set(UISkinOrientation.Right,         null);
            //e.Set(UISkinOrientation.BottomLeft,    null);
            //e.Set(UISkinOrientation.Bottom,        null);
            //e.Set(UISkinOrientation.BottomRight,   null);
            AddElement(e);

            e = new UISkinElement(UIElementType.ScrollbarGem);
            //e.Set(UISkinOrientation.TopLeft,       null);
            //e.Set(UISkinOrientation.Top,           null);
            //e.Set(UISkinOrientation.TopRight,      null);
            //e.Set(UISkinOrientation.Left,          null);
            e.Set(UISkinOrientation.Center,         new Rectangle(220,	64,    12,		12));
            //e.Set(UISkinOrientation.Right,         null);
            //e.Set(UISkinOrientation.BottomLeft,    null);
            //e.Set(UISkinOrientation.Bottom,        null);
            //e.Set(UISkinOrientation.BottomRight,   null);
            AddElement(e);

            e = new UISkinElement(UIElementType.ScrollbarBackground);
            //e.Set(UISkinOrientation.TopLeft,       null);
            //e.Set(UISkinOrientation.Top,           null);
            //e.Set(UISkinOrientation.TopRight,      null);
            e.Set(UISkinOrientation.Left,           new Rectangle(264,	0,     1,		96));
            e.Set(UISkinOrientation.Center,         new Rectangle(265,	0,     10,		96));
            e.Set(UISkinOrientation.Right,          new Rectangle(275,	0,     1,		96));
            //e.Set(UISkinOrientation.BottomLeft,    null);
            //e.Set(UISkinOrientation.Bottom,        null);
            //e.Set(UISkinOrientation.BottomRight,   null);
            AddElement(e);
        }
    }
}
