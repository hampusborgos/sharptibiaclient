using System;
using Microsoft.Xna.Framework;

namespace CTC
{
    /// <summary>
    /// The 133 colours an outfit can be dyed in. LookHead, LookBody, LookLegs and
    /// LookFeet are indices into this table, not colours in their own right.
    ///
    /// The table is not stored anywhere in Tibia.dat or Tibia.spr; the client
    /// generates it. It is nineteen hues at seven saturation/intensity pairs,
    /// with the first column of every pair replaced by a step on a grey ramp —
    /// which is why index 0 is white and 19, 38, 57 ... get progressively darker
    /// greys rather than continuing the hue wheel.
    /// </summary>
    public static class OutfitColors
    {
        private const int Hues = 19;
        private const int Shades = 7;

        public const int Count = Hues * Shades;

        private static readonly Color[] Table = BuildTable();

        /// <summary>
        /// The colour for an outfit index. Out of range indices are white, which
        /// leaves the sprite's own shading untouched.
        /// </summary>
        public static Color Lookup(int Index)
        {
            if (Index < 0 || Index >= Count)
                return Color.White;
            return Table[Index];
        }

        /// <summary>
        /// Dyes a creature sprite in place and hands it back.
        ///
        /// The sprite itself is painted in greys; its colours live in a second
        /// layer that masks it in pure yellow, red, green and blue for head, body,
        /// legs and feet. Multiplying the grey through the masked colour is what
        /// turns one sprite sheet into every outfit in the game.
        ///
        /// This is pixels only, no graphics device, so it can be checked without
        /// opening a window.
        /// </summary>
        public static Byte[] Dye(Byte[] Pixels, Byte[] Mask, ClientOutfit Outfit)
        {
            Color Head = Lookup(Outfit.LookHead);
            Color Body = Lookup(Outfit.LookBody);
            Color Legs = Lookup(Outfit.LookLegs);
            Color Feet = Lookup(Outfit.LookFeet);

            for (int i = 0; i < Pixels.Length; i += 4)
            {
                if (Pixels[i + 3] == 0 || Mask[i + 3] == 0)
                    continue;

                // The mask only ever holds the four primaries, but it is compared
                // loosely so a sprite that was ever resampled still dyes cleanly.
                Boolean Red = Mask[i + 0] > 127;
                Boolean Green = Mask[i + 1] > 127;
                Boolean Blue = Mask[i + 2] > 127;

                Color Dyed;
                if (Red && Green && !Blue)
                    Dyed = Head;
                else if (Red && !Green && !Blue)
                    Dyed = Body;
                else if (!Red && Green && !Blue)
                    Dyed = Legs;
                else if (!Red && !Green && Blue)
                    Dyed = Feet;
                else
                    continue;

                Pixels[i + 0] = (Byte)(Pixels[i + 0] * Dyed.R / 255);
                Pixels[i + 1] = (Byte)(Pixels[i + 1] * Dyed.G / 255);
                Pixels[i + 2] = (Byte)(Pixels[i + 2] * Dyed.B / 255);
            }

            return Pixels;
        }

        private static Color[] BuildTable()
        {
            Color[] Colors = new Color[Count];
            for (int Index = 0; Index < Count; ++Index)
                Colors[Index] = Generate(Index);
            return Colors;
        }

        private static Color Generate(int Index)
        {
            float Hue, Saturation, Intensity;

            if (Index % Hues == 0)
            {
                // The grey ramp: white at 0, black-ward in seven steps.
                Hue = 0.0f;
                Saturation = 0.0f;
                Intensity = 1.0f - (float)Index / Hues / Shades;
            }
            else
            {
                Hue = (Index % Hues) / 18.0f;
                switch (Index / Hues)
                {
                    case 0: Saturation = 0.25f; Intensity = 1.00f; break;
                    case 1: Saturation = 0.25f; Intensity = 0.75f; break;
                    case 2: Saturation = 0.50f; Intensity = 0.75f; break;
                    case 3: Saturation = 0.667f; Intensity = 0.75f; break;
                    case 4: Saturation = 1.00f; Intensity = 1.00f; break;
                    case 5: Saturation = 1.00f; Intensity = 0.75f; break;
                    default: Saturation = 1.00f; Intensity = 0.50f; break;
                }
            }

            if (Saturation == 0.0f)
            {
                Byte Grey = (Byte)(Intensity * 255);
                return new Color(Grey, Grey, Grey);
            }

            return FromHSI(Hue, Saturation, Intensity);
        }

        /// <summary>
        /// The conversion the original client uses. It is not quite HSV: the sector
        /// is interpolated between the intensity and the least intense channel,
        /// which keeps the six corners of the wheel exactly on the primaries.
        /// </summary>
        private static Color FromHSI(float Hue, float Saturation, float Intensity)
        {
            float Red = 0.0f, Green = 0.0f, Blue = 0.0f;
            float Low = Intensity * (1.0f - Saturation);

            if (Hue < 1.0f / 6.0f)
            {
                Red = Intensity;
                Blue = Low;
                Green = Blue + (Intensity - Blue) * (6.0f * Hue);
            }
            else if (Hue < 2.0f / 6.0f)
            {
                Green = Intensity;
                Blue = Low;
                Red = Green - (Intensity - Blue) * (6.0f * Hue - 1.0f);
            }
            else if (Hue < 3.0f / 6.0f)
            {
                Green = Intensity;
                Red = Low;
                Blue = Red + (Intensity - Red) * (6.0f * Hue - 2.0f);
            }
            else if (Hue < 4.0f / 6.0f)
            {
                Blue = Intensity;
                Red = Low;
                Green = Blue - (Intensity - Red) * (6.0f * Hue - 3.0f);
            }
            else if (Hue < 5.0f / 6.0f)
            {
                Blue = Intensity;
                Green = Low;
                Red = Green + (Intensity - Green) * (6.0f * Hue - 4.0f);
            }
            else
            {
                Red = Intensity;
                Green = Low;
                Blue = Red - (Intensity - Green) * (6.0f * Hue - 5.0f);
            }

            return new Color((Byte)(Red * 255), (Byte)(Green * 255), (Byte)(Blue * 255));
        }
    }
}
