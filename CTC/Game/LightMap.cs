using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CTC
{
    /// <summary>
    /// Rendering support for Tibia's lighting model.
    ///
    /// Tibia lights the world in two parts: a flat ambient level for the whole
    /// screen, and a radial glow around every light emitting item and creature.
    /// Both the ambient level and the per-source intensity/colour arrive as data
    /// (Tibia.dat carries LightLevel/LightColor per item, and the protocol sends
    /// WorldLight 0x82 and CreatureLight 0x8D), so all this class has to do is
    /// turn those numbers into pixels.
    ///
    /// The result is accumulated into an offscreen buffer and then multiplied
    /// over the finished scene, so unlit areas darken and lit areas keep their
    /// colour.
    /// </summary>
    public static class LightMap
    {
        /// <summary>
        /// Below this floor Tibia ignores the world light entirely - underground
        /// is always pitch dark regardless of the time of day above.
        /// </summary>
        public const int SeaFloor = 7;

        /// <summary>
        /// Size of the generated falloff texture. The glow is scaled to each
        /// light's radius when drawn, so this only bounds the gradient quality.
        /// </summary>
        private const int GlowResolution = 128;

        private static Texture2D _Glow;

        /// <summary>
        /// A radial falloff used as the brush for every light source.
        /// </summary>
        public static Texture2D Glow(GraphicsDevice Device)
        {
            if (_Glow != null)
                return _Glow;

            Color[] Pixels = new Color[GlowResolution * GlowResolution];
            float Center = (GlowResolution - 1) / 2.0f;

            for (int y = 0; y < GlowResolution; ++y)
            {
                for (int x = 0; x < GlowResolution; ++x)
                {
                    float dx = (x - Center) / Center;
                    float dy = (y - Center) / Center;
                    float Distance = (float)Math.Sqrt(dx * dx + dy * dy);

                    // Squared falloff, which holds a bright core and fades to
                    // nothing at the rim so neighbouring lights blend smoothly.
                    float Intensity = 1.0f - Distance;
                    if (Intensity <= 0.0f)
                    {
                        Pixels[y * GlowResolution + x] = Color.Transparent;
                        continue;
                    }
                    Intensity *= Intensity;

                    byte Value = (byte)(Intensity * 255.0f);
                    // Premultiplied, to match SpriteBatch's default blending.
                    Pixels[y * GlowResolution + x] = new Color(Value, Value, Value, Value);
                }
            }

            _Glow = new Texture2D(Device, GlowResolution, GlowResolution, false, SurfaceFormat.Color);
            _Glow.SetData(Pixels);
            return _Glow;
        }

        /// <summary>
        /// Tibia stores light colours as an index into its 216 entry palette,
        /// six evenly spaced steps per channel. Index 0 and anything past the
        /// palette mean plain white.
        /// </summary>
        public static Color FromEightBit(int Index)
        {
            if (Index <= 0 || Index >= 216)
                return Color.White;

            return new Color(
                (Index / 36) % 6 * 51,
                (Index / 6) % 6 * 51,
                Index % 6 * 51);
        }

        /// <summary>
        /// The screen-wide light level, as the colour the scene gets multiplied
        /// by before any individual light sources are added.
        /// </summary>
        public static Color Ambient(LightSource World, int Floor)
        {
            // Underground the sun does not reach, whatever the surface says.
            if (Floor > SeaFloor)
                return Color.Black;

            float Level = MathHelper.Clamp(World.Level / 255.0f, 0.0f, 1.0f);
            Color Tint = FromEightBit(World.Color);

            return new Color(
                (int)(Tint.R * Level),
                (int)(Tint.G * Level),
                (int)(Tint.B * Level));
        }

        /// <summary>
        /// Blend state that multiplies the light buffer into the scene.
        /// </summary>
        public static readonly BlendState Multiply = new BlendState
        {
            ColorSourceBlend = Blend.DestinationColor,
            ColorDestinationBlend = Blend.Zero,
            AlphaSourceBlend = Blend.DestinationAlpha,
            AlphaDestinationBlend = Blend.Zero,
        };
    }
}
