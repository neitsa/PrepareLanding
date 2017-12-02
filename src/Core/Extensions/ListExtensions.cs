using System.Collections.Generic;
using System.Linq;
using Verse;

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
            return thisEnumerable.Intersect(otherList).Count() == otherList.Count;
        }

        public static bool IsEqualNoOrder<T>(this IEnumerable<T> thisEnumerable, IEnumerable<T> other)
        {
            return thisEnumerable.OrderBy(t => t).SequenceEqual(other.OrderBy(t => t));
        }

        public static bool IsEqualNoOrderFast<T>(this IEnumerable<T> list1, IEnumerable<T> list2)
        {
            var cnt = new Dictionary<T, int>();
            foreach (var s in list1)
            {
                if (cnt.ContainsKey(s))
                {
                    cnt[s]++;
                }
                else
                {
                    cnt.Add(s, 1);
                }
            }
            foreach (var s in list2)
            {
                if (cnt.ContainsKey(s))
                {
                    cnt[s]--;
                }
                else
                {
                    return false;
                }
            }
            return cnt.Values.All(c => c == 0);
        }

        /// <summary>
        ///     Tells whether an enumerable contains another. This is not order dependent. 
        ///     <see cref="thisEnumerable"/> can be shorter or the same length than <see cref="other"/> and still be a subset.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thisEnumerable">The list that is checked for being a subset.</param>
        /// <param name="other">The containing list.</param>
        /// <returns>true if <see cref="thisEnumerable"/> is a subset of <see cref="other"/>, false otherwise.</returns>
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

        /// <summary>
        /// Re-order elements in a list.
        /// </summary>
        /// <typeparam name="T">type of elements in the list.</typeparam>
        /// <param name="index">The old index of the element to move.</param>
        /// <param name="newIndex">The new index of the element to move.</param>
        /// <param name="elementsList">The list of elements.</param>
        public static void ReorderElements<T>(this IList<T> elementsList, int index, int newIndex)
        {
            if ((index == newIndex) || (index < 0))
            {
                Log.Message($"[PrepareLanding] ReorderElements -> index: {index}; newIndex: {newIndex}");
                return;
            }

            if (elementsList.Count == 0)
            {
                Log.Message("[PrepareLanding] ReorderElements: elementsList count is 0.");
                return;
            }

            if ((index >= elementsList.Count) || (newIndex >= elementsList.Count))
            {
                Log.Message(
                    $"[PrepareLanding] ReorderElements -> index: {index}; newIndex: {newIndex}; elemntsList.Count: {elementsList.Count}");
                return;
            }

            var item = elementsList[index];
            elementsList.RemoveAt(index);
            elementsList.Insert(newIndex, item);
        }
    }
}