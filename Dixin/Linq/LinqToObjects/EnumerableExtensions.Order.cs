﻿namespace Dixin.Linq.LinqToObjects
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    internal static partial class EnumerableExtensions
    {
        internal static IEnumerable<TSource> Reverse<TSource>(this IEnumerable<TSource> source)
        {
            TSource[] array = ToArray(source); // Eager evaluation.
            for (int i = array.Length - 1; i >= 0; i--)
            {
                yield return array[i]; // Deferred execution.
            }
        }

        internal static IOrderedEnumerable<TSource> OrderBy<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IComparer<TKey> comparer = null) =>
                new OrderedSequence<TSource, TKey>(source, keySelector, comparer);

        internal static IOrderedEnumerable<TSource> OrderByDescending<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IComparer<TKey> comparer = null) =>
                new OrderedSequence<TSource, TKey>(source, keySelector, comparer, true);

        internal static IOrderedEnumerable<TSource> ThenBy<TSource, TKey>(
            this IOrderedEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IComparer<TKey> comparer = null) => source.CreateOrderedEnumerable(keySelector, comparer, false);

        internal static IOrderedEnumerable<TSource> ThenByDescending<TSource, TKey>(
            this IOrderedEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IComparer<TKey> comparer = null) => source.CreateOrderedEnumerable(keySelector, comparer, true);
    }

    internal class OrderedSequence<TSource, TKey> : IOrderedEnumerable<TSource>
    {
        private readonly IEnumerable<TSource> source;

        private readonly IComparer<TKey> comparer;

        private readonly bool descending;

        private readonly Func<TSource, TKey> keySelector;

        private readonly Func<TSource[], Comparison<int>> previousGetComparison; // Comparison<int> is Func<int, int, int>.

        internal OrderedSequence(
            IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IComparer<TKey> comparer,
            bool descending = false,

            // previousGetComparison is only specified in CreateOrderedEnumerable, 
            // and CreateOrderedEnumerable is only called by ThenBy/ThenByDescending.
            // When OrderBy/OrderByDescending is called, previousGetComparison is not specified.
            Func<TSource[], Comparison<int>> previousGetComparison = null) // Comparison<int> is Func<int, int, int>.
        {
            this.source = source;
            this.keySelector = keySelector;
            this.comparer = comparer ?? Comparer<TKey>.Default;
            this.descending = descending;
            this.previousGetComparison = previousGetComparison;
        }

        public IEnumerator<TSource> GetEnumerator()
        {
            TSource[] values = this.source.ToArray(); // Eager evaluation.
            int count = values.Length;
            if (count <= 0)
            {
                yield break;
            }

            int[] indexMap = new int[count];
            for (int index = 0; index < count; index++)
            {
                indexMap[index] = index;
            }

            // GetComparison is only called once for each generator instance.
            Comparison<int> comparison = this.GetComparison(values); // Comparison<int> is Func<int, int, int>.
            Array.Sort(indexMap, (index1, index2) => // index1 < index2
                {
                    // Format the compareResult. 
                    // When compareResult is equal, return index1 - index2, 
                    // so that indexMap[index1] is before indexMap[index2],
                    // 2 equal values' original order remains the same.
                    int compareResult = comparison(index1, index2);
                    return compareResult == 0 ? index1 - index2 : compareResult;
                }); // More eager evaluation.
            for (int index = 0; index < count; index++)
            {
                yield return values[indexMap[index]];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        // Only called in ThenBy/ThenByDescending.
        public IOrderedEnumerable<TSource> CreateOrderedEnumerable<TNextKey>
            (Func<TSource, TNextKey> nextKeySelector, IComparer<TNextKey> nextComparer, bool nextDescending) =>
                new OrderedSequence<TSource, TNextKey>(
                    this.source, nextKeySelector, nextComparer, nextDescending, this.GetComparison);

        private TKey[] GetKeys(TSource[] values)
        {
            int count = values.Length;
            TKey[] keys = new TKey[count];
            for (int index = 0; index < count; index++)
            {
                keys[index] = this.keySelector(values[index]);
            }

            return keys;
        }

        private Comparison<int> GetComparison(TSource[] values) // Comparison<int> is Func<int, int, int>.
        {
            // GetComparison is only called once for each generator instance,
            // so GetKeys is only called once during the ordering query execution.
            TKey[] keys = this.GetKeys(values);
            if (this.previousGetComparison == null)
            {
                // In OrderBy/OrderByDescending.
                return (index1, index2) =>
                    // OrderBy/OrderByDescending always need to compare keys of 2 values.
                    this.CompareKeys(keys, index1, index2);
            }

            // In ThenBy/ThenByDescending.
            Comparison<int> previousComparison = this.previousGetComparison(values);
            return (index1, index2) =>
            {
                // Only when previousCompareResult is equal, 
                // ThenBy/ThenByDescending needs to compare keys of 2 values.
                int previousCompareResult = previousComparison(index1, index2);
                    return previousCompareResult == 0
                        ? this.CompareKeys(keys, index1, index2)
                        : previousCompareResult;
            };
        }

        private int CompareKeys(TKey[] keys, int index1, int index2)
        {
            // Format the compareResult to always be 0, -1, or 1.
            int compareResult = this.comparer.Compare(keys[index1], keys[index2]);
            return compareResult == 0
                ? 0
                : (this.descending ? (compareResult > 0 ? -1 : 1) : (compareResult > 0 ? 1 : -1));
        }
    }
}
