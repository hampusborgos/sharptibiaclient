using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace CTC
{
    /// <summary>
    /// Something said on the map: speech above the speaker's head, or one of the
    /// messages the server sends about you, which sit above your own character.
    ///
    /// The 7.4 speak packet carries a name and a position but no creature id, so
    /// there is nothing to attach the text to. It stays on the tile it was said
    /// from, which for the few seconds it is up is where the speaker still is.
    /// Messages about you are the exception: they follow the view, because the
    /// view is centred on the player.
    /// </summary>
    public class SpokenText : GameEffect
    {
        /// <summary>
        /// Indices into Tibia's palette, the same one the protocol uses for
        /// animated text. See GameRenderer.MakeColor.
        /// </summary>
        public const int Yellow = 210;
        public const int Red = 180;
        public const int Green = 30;
        public const int White = 215;

        /// <summary>
        /// Speech wraps narrow, so a yelled sentence stands over its speaker
        /// rather than across everyone standing near them.
        /// </summary>
        private const float WrapWidth = 200;

        public readonly int Color;
        public readonly MapPosition Position;
        public readonly Boolean FollowsPlayer;

        /// <summary>
        /// Where over the tile it stands, in map pixels: the point the speaker
        /// hangs their name and bar from, taken as they spoke so the words keep
        /// their place if the speaker walks away.
        /// </summary>
        public readonly Vector2 Overhead;

        public readonly List<String> Lines = new List<String>();

        /// <summary>
        /// The widest line, which is what the text is centred and clamped on.
        /// </summary>
        public float Width;

        /// <summary>
        /// Speech, above the tile it was said from. A speaker gets a line of
        /// their own over what they said; monsters are given none, since a
        /// goblin announcing itself by name reads as a second name tag.
        /// </summary>
        public SpokenText(String Speaker, String Text, int Color, MapPosition Position, Vector2 Overhead)
            : this(Speaker, Text, Color, Position, Overhead, false)
        {
        }

        /// <summary>A message about the player, above the player.</summary>
        public SpokenText(String Text, int Color, Vector2 Overhead)
            : this(null, Text, Color, new MapPosition(), Overhead, true)
        {
        }

        private SpokenText(String Speaker, String Text, int Color, MapPosition Position, Vector2 Overhead, Boolean FollowsPlayer)
        {
            this.Color = Color;
            this.Position = Position;
            this.Overhead = Overhead;
            this.FollowsPlayer = FollowsPlayer;

            if (Speaker != null && Speaker != "")
                AddLine(Speaker + ":");

            Wrap(Text);

            // Long enough to read what was said. A one word shout should not
            // linger, and a long look description should not vanish half read.
            Duration = Math.Min(7.0, 2.5 + Text.Length * 0.06);
        }

        private void Wrap(String Text)
        {
            String Line = "";
            foreach (String Word in Text.Split(' '))
            {
                String Candidate = Line == "" ? Word : Line + " " + Word;
                if (Line != "" && UIContext.StandardFont.MeasureString(Candidate).X > WrapWidth)
                {
                    AddLine(Line);
                    Line = Word;
                }
                else
                    Line = Candidate;
            }
            AddLine(Line);
        }

        private void AddLine(String Line)
        {
            if (Line == "")
                return;

            Lines.Add(Line);
            Width = Math.Max(Width, UIContext.StandardFont.MeasureString(Line).X);
        }
    }
}
