using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Random = System.Random;

namespace PrepareLanding.Core.Extensions
{
    /// <summary>
    ///     Extensions for statistics
    /// </summary>
    public static class Statistics
    {
        /// <summary>
        ///     Calculates the mean of a sequence.
        /// </summary>
        /// <param name="sequence">The float sequence to calculate the mean of.</param>
        /// <returns>The mean of the sequence.</returns>
        public static float Mean(this IEnumerable<float> sequence)
        {
            return sequence.ToList().Mean();
        }

        /// <summary>
        ///     Calculates the mean of a list.
        /// </summary>
        /// <param name="list">The floats to calculate the mean of.</param>
        /// <returns>The mean of the list.</returns>
        public static float Mean(this IList<float> list)
        {
            return Mean(list, d => d);
        }

        /// <summary>
        ///     Calculates the mean of a generic sequence.
        /// </summary>
        /// <param name="sequence">The generic sequence to calculate the mean of.</param>
        /// <param name="floatConverterFunc">The function to convert each item of the sequence to float.</param>
        /// <returns>The mean of the sequence.</returns>
        public static float Mean<T>(this IEnumerable<T> sequence, Func<T, float> floatConverterFunc)
        {
            return sequence.ToList().Mean(floatConverterFunc);
        }

        /// <summary>
        ///     Calculates the mean of a generic list.
        /// </summary>
        /// <typeparam name="T">The type of list to calculate the mean of.</typeparam>
        /// <param name="list">The generic list to calculate the mean of.</param>
        /// <param name="floatConverter">The function to convert each item to float.</param>
        /// <returns>The mean of the sequence.</returns>
        public static float Mean<T>(this IList<T> list, Func<T, float> floatConverter)
        {
            return list.Select(floatConverter).Aggregate(0f, (agg, item) => agg + item, total => total / list.Count);
        }

        /// <summary>
        ///     Calculates the median of a sequence of floats.
        /// </summary>
        /// <param name="sequence">The sequence to operate on.</param>
        /// <returns>The median of the sequence.</returns>
        public static float Median(this IEnumerable<float> sequence)
        {
            var list = sequence.ToList();
            var mid = (list.Count - 1) / 2;
            return list.NthOrderStatistic(mid);
        }

        /// <summary>
        ///     Calculates the median of a sequence of elements.
        /// </summary>
        /// <typeparam name="T">The type of elements in sequence.</typeparam>
        /// <param name="sequence">The sequence to operate on.</param>
        /// <param name="floatConverterFunc">Logic to get a float from each element.</param>
        /// <returns>The median of the sequence.</returns>
        public static float Median<T>(this IEnumerable<T> sequence, Func<T, float> floatConverterFunc)
        {
            return Median(sequence.Select(floatConverterFunc));
        }

        /// <summary>
        ///     Gets the median member of a list of elements.
        /// </summary>
        /// <typeparam name="T">Type of elements in the list.</typeparam>
        /// <param name="list">The list to operate on.</param>
        /// <returns>The median of the list.</returns>
        public static T Median<T>(this IList<T> list) where T : IComparable<T>
        {
            return list.NthOrderStatistic((list.Count - 1) / 2);
        }

        /// <summary>
        ///     Calculate the statistics Mode of a list of floats.
        /// </summary>
        /// <param name="list">The list of floats from which to get the Mode.</param>
        /// <param name="decimals">Number of decimal place used to get the mode from floats.</param>
        /// <param name="midpointRounding">
        ///     Specifies how mathematical rounding methods should process a number that is midway
        ///     between two numbers.
        /// </param>
        /// <returns>The Mode of the list if one is found, otherwise null.</returns>
        public static float? Mode(List<float> list, int decimals = 0,
            MidpointRounding midpointRounding = MidpointRounding.AwayFromZero)
        {
            if (list.Count < 1)
                return null;

            var newList = list.Select(f => (float) Math.Round((decimal) f, decimals, midpointRounding)).ToList();

            return newList
                .GroupBy(f => f)
                .OrderByDescending(f => f.Count())
                .ThenBy(f => f.Key)
                .Select(f => (float?) f.Key)
                .FirstOrDefault();
        }

        /// <summary>
        ///     Returns Nth smallest element from the list. Here n starts from 0 so that n=0 returns minimum, n=1 returns 2nd
        ///     smallest element etc.
        ///     Note: specified list would be mutated in the process.
        /// </summary>
        public static T NthOrderStatistic<T>(this IList<T> list, int n, Random rnd = null) where T : IComparable<T>
        {
            return NthOrderStatistic(list, n, 0, list.Count - 1, rnd);
        }

        /// <summary>
        ///     Calculates the standard deviation of a sequence of floats. (note: this is Population Standard Deviation).
        /// </summary>
        /// <param name="sequence">The sequence to operate on.</param>
        /// <returns>The standard deviation of the sequence.</returns>
        public static float StandardDeviation(this IEnumerable<float> sequence)
        {
            var list = sequence.ToList();
            var avg = list.Average();
            return Mathf.Sqrt(list.Average(v => Mathf.Pow(v - avg, 2)));
        }

        /// <summary>
        ///     Calculates the standard deviation of a sequence of elements.
        /// </summary>
        /// <typeparam name="T">The type of elements in sequence.</typeparam>
        /// <param name="sequence">The sequence to operate on.</param>
        /// <param name="floatConverterFunc">Logic to get a float from each element.</param>
        /// <returns>The standard deviation of the sequence.</returns>
        public static float StandardDeviation<T>(this IEnumerable<T> sequence, Func<T, float> floatConverterFunc)
        {
            return sequence.Select(floatConverterFunc).StandardDeviation();
        }

        /// <summary>
        ///     Swap two elements positions in a list.
        /// </summary>
        /// <typeparam name="T">Type of elements.</typeparam>
        /// <param name="list">The list to swap on.</param>
        /// <param name="i">The first element position to swap.</param>
        /// <param name="j">The second element position to swap.</param>
        public static void Swap<T>(this IList<T> list, int i, int j)
        {
            if (i == j) return;
            var temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }

        /// <summary>
        ///     Calculates the variance of a sequence of floats.
        /// </summary>
        /// <param name="sequence">The floats to calculate the variance of.</param>
        /// <returns>The variance of the list.</returns>
        public static float Variance(this IEnumerable<float> sequence)
        {
            return sequence.ToList().Variance();
        }

        /// <summary>
        ///     Calculates the variance of a list of floats.
        /// </summary>
        /// <param name="list">The list of floats to calculate the variance of.</param>
        /// <returns>The variance of the list.</returns>
        public static float Variance(this IList<float> list)
        {
            return list.Variance(list.Mean());
        }

        /// <summary>
        ///     Calculates the variance of a list of floats.
        /// </summary>
        /// <param name="list">The floats to calculate the variance of.</param>
        /// <param name="mean">The mean value for the list.</param>
        /// <returns>The variance of the list.</returns>
        public static float Variance(this IList<float> list, float mean)
        {
            return list.Variance(mean, d => d);
        }

        /// <summary>
        ///     Calculates the variance of a generic sequence.
        /// </summary>
        /// <typeparam name="T">The type of generic sequence to calculate the variance of.</typeparam>
        /// <param name="sequence">The generic sequence to calculate the variance of.</param>
        /// <param name="floatConverterFunc">The function to convert each item of the generic sequence to float.</param>
        /// <returns>The variance of the list.</returns>
        public static float Variance<T>(this IEnumerable<T> sequence, Func<T, float> floatConverterFunc)
        {
            return sequence.ToList().Variance(floatConverterFunc);
        }

        /// <summary>
        ///     Calculates the variance of a generic sequence.
        /// </summary>
        /// <typeparam name="T">The type of generic sequence to calculate the variance of.</typeparam>
        /// <param name="sequence">The generic sequence to calculate the variance of.</param>
        /// <param name="mean">The mean value for the list.</param>
        /// <param name="floatConverterFunc">The function to convert each item of the generic sequence to float.</param>
        /// <returns>The variance of the list.</returns>
        public static float Variance<T>(this IEnumerable<T> sequence, float mean, Func<T, float> floatConverterFunc)
        {
            return sequence.ToList().Variance(mean, floatConverterFunc);
        }

        /// <summary>
        ///     Calculates the variance of a generic list.
        /// </summary>
        /// <typeparam name="T">The type of generic list to calculate the variance of.</typeparam>
        /// <param name="list">The generic list to calculate the variance of.</param>
        /// <param name="floatConverterFunc">The function to convert each item of the generic list to float.</param>
        /// <returns>The variance of the list.</returns>
        public static float Variance<T>(this IList<T> list, Func<T, float> floatConverterFunc)
        {
            return list.Variance(list.Mean(floatConverterFunc), floatConverterFunc);
        }

        /// <summary>
        ///     Calculates the variance of a generic list.
        /// </summary>
        /// <typeparam name="T">The type of generic list to calculate the variance of.</typeparam>
        /// <param name="list">The generic list to calculate the variance of.</param>
        /// <param name="mean">The mean value for the list.</param>
        /// <param name="floatConverterFunc">The function to convert each item of the generic list to float.</param>
        /// <returns>The variance of the list.</returns>
        public static float Variance<T>(this IList<T> list, float mean, Func<T, float> floatConverterFunc)
        {
            return list.Count == 0
                ? 0
                : list.Select(floatConverterFunc).Aggregate(0f, (agg, item) => Mathf.Pow(item - mean, 2),
                    total => total / (list.Count - 1));
        }

        private static T NthOrderStatistic<T>(this IList<T> list, int n, int start, int end, Random rnd)
            where T : IComparable<T>
        {
            while (true)
            {
                var pivotIndex = list.Partition(start, end, rnd);
                if (pivotIndex == n) return list[pivotIndex];
                if (n < pivotIndex) end = pivotIndex - 1;
                else start = pivotIndex + 1;
            }
        }

        /// <summary>
        ///     Partitions the given list around a pivot element such that all elements on left of pivot are less than or equal to
        ///     pivot
        ///     Elements to right of the pivot are guaranteed greater than the pivot. Can be used for sorting N-order statistics
        ///     such
        ///     as median finding algorithms.
        ///     Pivot is selected randomly if random number generator is supplied else its selected as last element in the list.
        /// </summary>
        private static int Partition<T>(this IList<T> list, int start, int end, Random rnd = null)
            where T : IComparable<T>
        {
            if (rnd != null) list.Swap(end, rnd.Next(start, end));
            var pivot = list[end];
            var lastLow = start - 1;
            for (var i = start; i < end; i++)
                if (list[i].CompareTo(pivot) <= 0) list.Swap(i, ++lastLow);
            list.Swap(end, ++lastLow);
            return lastLow;
        }
    }
}