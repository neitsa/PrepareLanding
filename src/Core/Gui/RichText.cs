using UnityEngine;

namespace PrepareLanding.Gui
{
    /// <summary>
    ///     Extensions for <see cref="Verse.Text" /> class.
    /// </summary>
    public static class RichText
    {
        public static string Bold(string text)
        {
            return $"<b>{text}</b>";
        }

        public static string Italic(string text)
        {
            return $"<i>{text}</i>";
        }

        public static string Size(string text, int size)
        {
            if (size <= 0)
                size = 10;

            return $"<size={size}>{text}</size>";
        }

        public static string Color(string text, string colorName)
        {
            return $"<color={colorName}>{text}</color>";
        }

        public static string Color(string text, Color color)
        {
            var hexString = ColorUtility.ToHtmlStringRGB(color);
            return $"<color=#{hexString}>{text}</color>";
        }
    }
}