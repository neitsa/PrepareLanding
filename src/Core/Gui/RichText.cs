using System;
using UnityEngine;

namespace PrepareLanding.Core.Gui
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

    public static class StringExtension
    {
        public static string RichTextBold(this string text)
        {
            return RichText.Bold(text);
        }

        public static string RichTextItalic(this string text)
        {
            return RichText.Italic(text);
        }

        public static string RichTextSize(this string text, int size)
        {
            return RichText.Size(text, size);
        }

        public static string RichTextColor(this string text, string colorName)
        {
            return RichText.Color(text, colorName);
        }

        public static string RichTextColor(this string text, Color color)
        {
            return RichText.Color(text, color);
        }

        public static T Chain<T>(this T source, Func<T, T> operation) where T : class
        {
            return operation(source);
        }
    }
}