using System.Collections.Generic;
using System.Linq;

namespace PrepareLanding.Extensions
{
    public static class ListExtensions
    {
        public static bool ContainsAll<T>(this List<T> thisList, List<T> other)
        {
            return thisList.Intersect(other).Count() == other.Count;
        }

        public static bool ContainsAll<T>(this IEnumerable<T> thisEnumerable, IEnumerable<T> other)
        {
            var otherList = other as IList<T> ?? other.ToList();
            return thisEnumerable.Intersect(otherList).Count() == otherList.Count();
        }

        public static bool IsSubset<T>(this IEnumerable<T> thisEnumerable, IEnumerable<T> other)
        {
            return !thisEnumerable.Except(other).Any();
        }

        public static bool IsSubsetInOrder<T>(this List<T> thisList, List<T> other)
        {
            if (!IsSubset(thisList, other))
                return false;

            var otherIndex = other.IndexOf(thisList[0]);

            /*
            for (var i = 0; i < thisList.Count; i++)
            {
                
                if (Comparer<T>.Default.Compare(other[i + otherIndex], thisList[i]) != 0)
                    return false;
            }*/
            return !thisList.Where((t, i) => Comparer<T>.Default.Compare(other[i + otherIndex], t) != 0).Any();
        }
    }
}
