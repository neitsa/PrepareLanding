using System.Collections.Generic;
using UnityEngine;

namespace PrepareLanding.Core
{
    public static class ColorUtils
    {
        /// <summary>
        ///     Create a solid (no alpha) <see cref="Gradient" /> from a list of colors.
        /// </summary>
        /// <param name="colors">The list of colors to create the gradient (2 colors minimum).</param>
        /// <returns>The <see cref="Gradient" /> create from the given list of colors or null if less than 2 colors were given.</returns>
        public static Gradient CreateSolidGradient(IList<Color> colors)
        {
            var numColors = colors.Count;
            if (numColors < 2)
                return null;

            var gradient = new Gradient();

            var t = 1f / (numColors - 1);

            var gck = new GradientColorKey[numColors];
            var gak = new GradientAlphaKey[numColors];
            for (var i = 0; i < numColors; i++)
            {
                gck[i].color = colors[i];
                gck[i].time = i * t;

                gak[i].alpha = 1.0f; // "solid" color
                gak[i].time = i * t;
            }

            gradient.SetKeys(gck, gak);

            return gradient;
        }

        /// <summary>
        ///     Create a list of colors for a given number of samples.
        /// </summary>
        /// <param name="gradient">The gradient from which to create the colors.</param>
        /// <param name="numberOfsamples">The number of color samples required.</param>
        /// <returns>A list of colors.</returns>
        public static List<Color> CreateColorSamples(Gradient gradient, int numberOfsamples)
        {
            /*
             not that hard to understand but I may forget, so:
              let say the gradient contains two colors: leftmost is black and rightmost is white (in-between a gradient of colors between black and white)
              now let say we need 3 "numberOfSamples", so:
              iSample = 1 / (3 - 1) =  0.5
              we have 3 loops so time = 0 ; 0.5 ; 1
              so the resulting list of colors is: pure black (0), gray (0.5) and pure white (1) [with their respective RGBA values]
              This function is helpful to map gradient colors to another set of samples (like, for instance, temperatures)
            */

            var colorList = new List<Color>(numberOfsamples);

            var iSample = 1f / (numberOfsamples - 1);
            for (var i = 0; i < numberOfsamples; i++)
            {
                var time = i * iSample;
                var color = gradient.Evaluate(time);
                colorList.Add(color);
            }

            return colorList;
        }

        /// <summary>
        ///     Create a <see cref="Texture2D" /> from a <see cref="Gradient" />.
        /// </summary>
        /// <param name="gradient">The gradient from which to create the texture.</param>
        /// <param name="width">Width of the resulting texture in pixels.</param>
        /// <param name="height">Height of the resulting texture, in pixels.</param>
        /// <returns>A <see cref="Texture2D" />.</returns>
        public static Texture2D CreateGradientTexture(Gradient gradient, int width = 100, int height = 1)
        {
            // width, height, [red, green, blue, alpha], no mip-map and usual bilinear filter
            var gradientTexture =
                new Texture2D(width, height, TextureFormat.RGBA32, false) {filterMode = FilterMode.Bilinear};

            var colors = CreateColorSamples(gradient, width);

            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                gradientTexture.SetPixel(x, y, colors[x]);

            gradientTexture.Apply();
            return gradientTexture;
        }
    }
}