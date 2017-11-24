using System.Linq;

namespace PrepareLanding.Core.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        ///  Repeat a string n times.
        /// </summary>
        /// <param name="s">The string to repeat.</param>
        /// <param name="n">Number of times the string must be repeated.</param>
        /// <returns>The input string repeated n times.</returns>
        public static string Repeat(this string s, int n)
        {
            return new string(Enumerable.Range(0, n).SelectMany(x => s).ToArray());
        }
    }
}
