using System.Collections.Generic;
using System.Linq;

namespace PrepareLanding.Core.Extensions
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

        public static bool IsSubsetInOrder<T>(this List<T> subsetList, List<T> containingList)
        {
            if (!IsSubset(subsetList, containingList))
                return false;

            var otherIndex = containingList.IndexOf(subsetList[0]);

            return !subsetList.Where((t, i) => Comparer<T>.Default.Compare(containingList[i + otherIndex], t) != 0)
                .Any();
        }

        public static bool IsSubsetInOrderSamePos<T>(this List<T> subsetList, List<T> containingList)
        {
            if (subsetList.Count > containingList.Count)
                return false;

            if (!subsetList.IsSubset(containingList))
                return false;

            //return !subsetList.Where((t, i) => Comparer<T>.Default.Compare(t, containingList[i]) != 0).Any(); 
            var count = subsetList.Count;
            for (var i = 0; i < count; i++)
                if (!EqualityComparer<T>.Default.Equals(subsetList[i], containingList[i]))
                    return false;

            return true;
        }
    }
}