using System.Linq;

namespace PrepareLanding.Extensions
{
    public static class StringExtensions
    {

        public static string Repeat(this string s, int n)
        {
            return new string(Enumerable.Range(0, n).SelectMany(x => s).ToArray());
        }

        public static string Repeat(this char c, int n)
        {
            return new string(c, n);
        }
        public static bool IsFullyTypedNumber<T>(this string s)
        {
            if (s == string.Empty)
            {
                return false;
            }
            if (typeof(T) == typeof(float))
            {
                string[] array = s.Split(new char[]
                {
                    '.'
                });
                if (array.Length > 2 || array.Length < 1)
                {
                    return false;
                }

                // Patch
                if (array.Length == 2)
                {
                    if (array[1] == string.Empty)
                        return false;
                }
                // end patch

                if (!array[0].ContainsOnlyCharacters("-0123456789"))
                {
                    return false;
                }
                if (array.Length == 2 && !array[1].ContainsOnlyCharacters("0123456789"))
                {
                    return false;
                }
            }
            return typeof(T) != typeof(int) || s.ContainsOnlyCharacters("-0123456789");
        }

        public static bool ContainsOnlyCharacters(this string s, string allowedChars)
        {
            for (int i = 0; i < s.Length; i++)
            {
                if (!allowedChars.Contains(s[i]))
                {
                    return false;
                }
            }
            return true;
        }
        public static int CharacterCount(this string s, char c)
        {
            int num = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == c)
                {
                    num++;
                }
            }
            return num;
        }
    }
}
