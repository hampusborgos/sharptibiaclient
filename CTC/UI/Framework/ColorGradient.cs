using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace CTC
{
    public class ColorGradient
    {
        List<Color> Colors = new List<Color>();
        List<float> Cutoffs = new List<float>();

        public ColorGradient(float[] cutoffs, Color[] colors)
        {
            if (cutoffs.Length != colors.Length)
                throw new ArgumentException("ColorGradient must be constructed with an equal amount of cutoffs and colors.");

            for (int i = 0; i < colors.Length; ++i)
                AddColor(cutoffs[i], colors[i]);
        }

        public void AddColor(float atCutoff, Color color)
        {
            // Find the index which is less that the cutoff
            int index = Cutoffs.FindIndex(delegate(float f)
            {
                return f >= atCutoff;
            });

            // Add the color & cutoff
            if (index == -1)
            {
                Cutoffs.Add(atCutoff);
                Colors.Add(color);
            }
            else
            {
                Cutoffs.Insert(index, atCutoff);
                Colors.Insert(index, color);
            }

        }

        public Color Sample(float point)
        {
            if (point <= Cutoffs[0])
                return Colors[0];
            if (point >= Cutoffs[Cutoffs.Count - 1])
                return Colors[Colors.Count - 1];

            int lowerIndex = Cutoffs.FindLastIndex(delegate(float f)
            {
                return f < point;
            });
            int higherIndex = Cutoffs.FindIndex(delegate(float f)
            {
                return f >= point;
            });
            float normalizedPoint = point - Cutoffs[lowerIndex];
            normalizedPoint /= Cutoffs[higherIndex] - Cutoffs[lowerIndex];
            return new Color(
                (int)(Colors[lowerIndex].R * (1f - normalizedPoint) + Colors[higherIndex].R * normalizedPoint),
                (int)(Colors[lowerIndex].G * (1f - normalizedPoint) + Colors[higherIndex].G * normalizedPoint),
                (int)(Colors[lowerIndex].B * (1f - normalizedPoint) + Colors[higherIndex].B * normalizedPoint),
                (int)(Colors[lowerIndex].A * (1f - normalizedPoint) + Colors[higherIndex].A * normalizedPoint)
            );
        }
    };
}
