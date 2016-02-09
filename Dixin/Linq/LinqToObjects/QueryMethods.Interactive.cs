﻿namespace Dixin.Linq.LinqToObjects
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using Dixin.Reflection;

    internal static partial class QueryMethods
    {
        #region Generation

        internal static void Defer()
        {
            Func<IEnumerable<int>> sequenceFactory = () =>
                {
                    Trace.WriteLine("Executing factory.");
                    return Enumerable.Empty<int>();
                };
            IEnumerable<int> sequence1 = sequenceFactory() // sequenceFactory immediate executes.
                .Where(@int => @int > 0);
            IEnumerable<int> sequence2 = EnumerableEx.Defer(sequenceFactory) // sequenceFactory does not execute.
                .Where(@int => @int > 0);
        }

        internal static void Create()
        {
            IEnumerable<int> sequence = EnumerableEx.Create<int>(async yield =>
                {
                    await yield.Return(0); // yield return 0;
                    await yield.Return(1); // yield return 1;
                    await yield.Break(); // yield break;
                    await yield.Return(2); // yield return 2;
                });
            int[] result = sequence.ToArray(); // 0 1.
        }

        internal static IEnumerable<TResult> CastWithCreate<TResult>
            (this IEnumerable source) => source as IEnumerable<TResult> ?? EnumerableEx.Create<TResult>(async yield =>
                {
                    foreach (object value in source)
                    {
                        await yield.Return((TResult)value); // yield return (TResult)value;
                    }
                });

        internal static IEnumerable<TResult> CreateWithCreate<TResult>
            (Func<IEnumerator<TResult>> getEnumerator) => EnumerableEx.Create<TResult>(async yield =>
                {
                    using (IEnumerator<TResult> iterator = getEnumerator())
                    {
                        while (iterator.MoveNext())
                        {
                            await yield.Return(iterator.Current); // yield return iterator.Current;
                        }
                    }
                });

        #endregion

        #region Filtering

        internal static void DistinctUntilChanged()
        {
            IEnumerable<int> source = new int[]
                {
            0, 0, 0, /* Changed. */ 1, 1, /* Changed. */ 0, 0, /* Changed. */ 2, /* Changed. */ 1, 1
                };
            int[] distinctUntilChanged = source.DistinctUntilChanged().ToArray(); // 0 1 0 2 1.
        }

        #endregion

        #region Mapping

        internal static void Scan()
        {
            int productOfValues = Int32Source().Aggregate((currentProduct, @int) => currentProduct * @int);
            // ((((-1 * 1) * 2) * 3) * -4) => 24.

            int[] productsOfValues = Int32Source().Scan((currentProduct, @int) => currentProduct * @int).ToArray();
            // ((((-1 * 1) * 2) * 3) * -4) => { -1, -2, -6, 24 }.
        }

        internal static void Expand()
        {
            int[] expanded = Enumerable.Range(0, 5).Expand(@int => EnumerableEx.Return(@int * @int)).Take(25).ToArray();
            // 0 1 2 3 4 => map each @int to { @int * @int }:
            // 0 1 4 9 16 => map each @int to { @int * @int }:
            // 0 1 16 81 256 => map each @int to { @int * @int }:
            // 0 1 256 6561 65536 => map each @int to { @int * @int }:
            // 0 1 65536 43046721 4294967296 => ...
        }

        internal static void Expand2()
        {
            int[] expanded = Enumerable.Range(0, 5).Expand(@int => Enumerable.Repeat(@int, 2)).Take(75).ToArray();
            // 0 1 2 3 4 => map each @int to { @int, @int }:
            // 0 0 1 1 2 2 3 3 4 4 => map each @int to { @int, @int }:
            // 0 0 0 0 1 1 1 1 2 2 2 2 3 3 3 3 4 4 4 4 => map each @int to { @int, @int }:
            // 0 0 0 0 0 0 0 0 1 1 1 1 1 1 1 1 2 2 2 2 2 2 2 2 3 3 3 3 3 3 3 3 4 4 4 4 4 4 4 4 => ...
        }

        internal static void Expand3()
        {
            int[] expanded = Enumerable.Range(0, 5).Expand(@int => Enumerable.Empty<int>()).Take(100).ToArray();
            // 0 1 2 3 4 => map each @int to { }.
        }

        #endregion

        #region Partioning

        internal static void SkipLastTakeLast()
        {
            int[] skipFirst2 = Enumerable.Range(0, 5).Skip(2).ToArray(); // 2 3 4.
            int[] skipLast2 = Enumerable.Range(0, 5).SkipLast(2).ToArray(); // 0 1 2.
            int[] takeFirst2 = Enumerable.Range(0, 5).Take(2).ToArray(); // 0 1.
            int[] takeLast2 = Enumerable.Range(0, 5).TakeLast(2).ToArray(); // 3 4.
        }

        #endregion

        #region Conversion

        internal static void Hide()
        {
            List<string> source = new List<string>() { "a", "b" };
            IEnumerable<string> exposed = source.AsEnumerable();
            bool asEnumerable = object.ReferenceEquals(source, exposed); // true.
            (exposed as List<string>).Reverse(); // List<T>.Reverse.
            (exposed as List<string>).Add("c"); // source can be changed.

            IEnumerable<string> hidden = source.Hide(); // hidden is a read only generator.
            bool hide = object.ReferenceEquals(source, hidden); // false.
        }

        #endregion

        #region Buffering

        internal static void Buffer()
        {
            IEnumerable<IList<int>> buffers1 = Enumerable.Range(0, 5).Buffer(2, 1);
            IList<int>[] result1 = buffers1.ToArray();
            // { { 0, 1 },
            //   { 1, 2 },
            //   { 2, 3 },
            //   { 3, 4 },
            //   { 4, }   }

            IEnumerable<IList<int>> buffers2 = Enumerable.Range(0, 5).Buffer(2, 2); // .Buffer(2)
            IList<int>[] result2 = buffers2.ToArray();
            // { { 0, 1 },
            //   { 2, 3 },
            //   { 4, }   }

            IEnumerable<IList<int>> buffers3 = Enumerable.Range(0, 5).Buffer(2, 3);
            IList<int>[] result3 = buffers3.ToArray();
            // { { 0, 1 },
            //   { 3, 4 } }
        }

        internal static void Share()
        {
            IEnumerable<int> sequence = Enumerable.Range(0, 5);
            IEnumerator<int> indepedentIterator1 = sequence.GetEnumerator();
            IEnumerator<int> indepedentIterator2 = sequence.GetEnumerator();
            indepedentIterator1.MoveNext(); Trace.WriteLine(indepedentIterator1.Current); // 0| |
            indepedentIterator2.MoveNext(); Trace.WriteLine(indepedentIterator2.Current); //  |0|
            indepedentIterator1.MoveNext(); Trace.WriteLine(indepedentIterator1.Current); // 1| |
            IEnumerator<int> indepedentIterator3 = sequence.GetEnumerator();                //  | |
            indepedentIterator3.MoveNext(); Trace.WriteLine(indepedentIterator3.Current); //  | |0
            indepedentIterator1.MoveNext(); Trace.WriteLine(indepedentIterator1.Current); // 2| |
            indepedentIterator2.MoveNext(); Trace.WriteLine(indepedentIterator2.Current); //  |1|
                                                                                            // ...

            IBuffer<int> share = Enumerable.Range(0, 5).Share();
            IEnumerator<int> sharedIterator1 = share.GetEnumerator();
            IEnumerator<int> sharedIterator2 = share.GetEnumerator();
            sharedIterator1.MoveNext(); Trace.WriteLine(sharedIterator1.Current); // 0| |
            sharedIterator2.MoveNext(); Trace.WriteLine(sharedIterator2.Current); //  |1|
            sharedIterator1.MoveNext(); Trace.WriteLine(sharedIterator1.Current); // 2| |
            IEnumerator<int> sharedIterator3 = share.GetEnumerator();               //  | |
            sharedIterator3.MoveNext(); Trace.WriteLine(sharedIterator3.Current); //  | |3

            share.Dispose();
            sharedIterator1.MoveNext(); // ObjectDisposedException.
            sharedIterator2.MoveNext(); // ObjectDisposedException.
            sharedIterator3.MoveNext(); // ObjectDisposedException.
        }

        internal static IEnumerable<int> TracedRange(int start, int count)
        {
            for (int value = start; value < start + count; value++)
            {
                Trace.WriteLine(value);
                yield return value;
            }
        }

        internal static void ShareWithSelector()
        {
            IEnumerable<int> source1 = TracedRange(0, 5);
            int[] concat1 = source1.Concat(source1).ToArray(); // 0 1 2 3 4 0 1 2 3 4.
            // Trace: 0 1 2 3 4 0 1 2 3 4.

            IEnumerable<int> source2 = TracedRange(0, 5).Share();
            int[] concat2 = source2.Concat(source2).ToArray(); // 0 1 2 3 4.
            // Trace: 0 1 2 3 4.

            IEnumerable<int> source3 = TracedRange(0, 5);
            int[] concat3 = source3.Share(source => source.Concat(source)).ToArray(); // 0 1 2 3 4.
            // Trace: 0 1 2 3 4.
        }

        internal static IEnumerable<TSource> Concat<TSource>(
            IEnumerable<TSource> first, IEnumerable<TSource> second)
        {
            using (IEnumerator<TSource> iterator1 = first.GetEnumerator())
            {
                while (iterator1.MoveNext())
                {
                    yield return iterator1.Current;
                }
            }

            using (IEnumerator<TSource> iterator2 = second.GetEnumerator())
            {
                while (iterator2.MoveNext())
                {
                    yield return iterator2.Current;
                }
            }
        }

        internal static void ShareWithSelector2()
        {
            IEnumerable<int> source1 = TracedRange(0, 5);
            int[] concat1 = EnumerableEx.Create<int>(async yield =>
                {
                    // Start concatenation:
                    using (IEnumerator<int> indepedentIterator1 = source1.GetEnumerator())
                    {
                        while (indepedentIterator1.MoveNext())
                        {
                            await yield.Return(indepedentIterator1.Current); // yield return 0 1 2 3 4.
                        }
                    }

                    using (IEnumerator<int> indepedentIterator2 = source1.GetEnumerator())
                    {
                        while (indepedentIterator2.MoveNext())
                        {
                            await yield.Return(indepedentIterator2.Current); // yield return 0 1 2 3 4.
                        }
                    }
                }).ToArray(); // 0 1 2 3 4 0 1 2 3 4.

            IEnumerable<int> source2 = TracedRange(0, 5).Share();
            int[] concat2 = EnumerableEx.Create<int>(async yield =>
                {
                    // Start concatenation:
                    using (IEnumerator<int> sharedIterator1 = source2.GetEnumerator())
                    {
                        while (sharedIterator1.MoveNext())
                        {
                            await yield.Return(sharedIterator1.Current); // yield return 0 1 2 3 4.
                        }
                    }

                    using (IEnumerator<int> sharedIterator2 = source2.GetEnumerator())
                    {
                        while (sharedIterator2.MoveNext())
                        {
                            await yield.Return(sharedIterator2.Current); // yield return nothing.
                        }
                    }
                }).ToArray(); // 0 1 2 3 4.

            IEnumerable<int> source3 = TracedRange(0, 5);
            int[] concat3 = EnumerableEx.Create<int>(async yield =>
                {
                    IEnumerable<int> source = source3.Share(); // IBuffer<int>.
                                                               // Start concatenation:
                    using (IEnumerator<int> sharedIterator1 = source.GetEnumerator())
                    {
                        while (sharedIterator1.MoveNext())
                        {
                            await yield.Return(sharedIterator1.Current); // yield return 0 1 2 3 4.
                        }
                    }

                    using (IEnumerator<int> sharedIterator2 = source.GetEnumerator())
                    {
                        while (sharedIterator2.MoveNext())
                        {
                            await yield.Return(sharedIterator2.Current); // yield return nothing.
                        }
                    }
                }).ToArray(); // 0 1 2 3 4.
        }

        internal static void ShareWithSelector3()
        {
            IEnumerable<int> source1 = TracedRange(0, 5);
            Tuple<int, int>[] concat1 = source1.Zip(source1, Tuple.Create).ToArray();
            // (0, 0) (1, 1) (2, 2) (3, 3) (4, 4).
            // Trace: 0 0 1 1 2 2 3 3 4 4.

            IEnumerable<int> source2 = TracedRange(0, 5);
            Tuple<int, int>[] concat2 = source2.Zip(source2, Tuple.Create).ToArray();
            // (0, 1) (2, 3).
            // Trace: 0 1 2 3 4.

            IEnumerable<int> source3 = TracedRange(0, 5);
            Tuple<int, int>[] concat3 = source3.Share(source => source.Zip(source, Tuple.Create)).ToArray();
            // (0, 1) (2, 3).
            // Trace: 0 1 2 3 4.
        }

        internal static void ShareWithSelector4()
        {
            IEnumerable<int> source1 = TracedRange(0, 5);
            Tuple<int, int>[] concat1 = EnumerableEx.Create<Tuple<int, int>>(async yield =>
                {
                    // Start zipping:
                    using (IEnumerator<int> indepdentIterator1 = source1.GetEnumerator())
                    using (IEnumerator<int> indepdentIterator2 = source1.GetEnumerator())
                    {
                        while (indepdentIterator1.MoveNext() && indepdentIterator2.MoveNext())
                        {
                            // yield return (0, 0) (1, 1) (2, 2) (3, 3) (4, 4).
                            await yield.Return(
                                Tuple.Create(indepdentIterator1.Current, indepdentIterator2.Current));
                        }
                    }
                }).ToArray(); // (0, 0) (1, 1) (2, 2) (3, 3) (4, 4).

            IEnumerable<int> source2 = TracedRange(0, 5).Share();
            Tuple<int, int>[] concat2 = EnumerableEx.Create<Tuple<int, int>>(async yield =>
                {
                    // Start zipping:
                    using (IEnumerator<int> sharedIterator1 = source2.GetEnumerator())
                    using (IEnumerator<int> sharedIterator2 = source2.GetEnumerator())
                    {
                        while (sharedIterator1.MoveNext() && sharedIterator2.MoveNext())
                        {
                            // yield return (0, 1) (2, 3).
                            await yield.Return(
                                Tuple.Create(sharedIterator1.Current, sharedIterator2.Current));
                        }
                    }
                }).ToArray(); // (0, 1) (2, 3).

            IEnumerable<int> source3 = TracedRange(0, 5);
            Tuple<int, int>[] concat3 = EnumerableEx.Create<Tuple<int, int>>(async yield =>
                {
                    IEnumerable<int> source = source3.Share();
                    // Start zipping:
                    using (IEnumerator<int> sharedIterator1 = source.GetEnumerator())
                    using (IEnumerator<int> sharedIterator2 = source.GetEnumerator())
                    {
                        while (sharedIterator1.MoveNext() && sharedIterator2.MoveNext())
                        {
                            // yield return (0, 1) (2, 3).
                            await yield.Return(Tuple.Create(sharedIterator1.Current, sharedIterator2.Current));
                        }
                    }
                }).ToArray(); // (0, 1) (2, 3).
        }

        internal static void Publish()
        {
            IBuffer<int> publish = TracedRange(0, 5).Publish();
            IEnumerator<int> iterator1 = publish.GetEnumerator();
            // iterator1: 0 1 2 3 4
            iterator1.MoveNext(); Trace.WriteLine(iterator1.Current); // 0| |Trace: 0
            iterator1.MoveNext(); Trace.WriteLine(iterator1.Current); // 1| |Trace: 1
            iterator1.MoveNext(); Trace.WriteLine(iterator1.Current); // 2| |Trace: 2
            IEnumerator<int> iterator2 = publish.GetEnumerator();       //  | |
                                                                        // iterator2: 3 4                                               | |
            iterator2.MoveNext(); Trace.WriteLine(iterator2.Current); //  |3|Trace: 3
            iterator1.MoveNext(); Trace.WriteLine(iterator1.Current); // 3| |
            iterator2.MoveNext(); Trace.WriteLine(iterator2.Current); //  |4|Trace: 4
            iterator1.MoveNext(); Trace.WriteLine(iterator1.Current); // 4| |
                                                                        // Trace: 0 1 2 3 4.

            publish.Dispose();
            iterator1.MoveNext(); // ObjectDisposedException.
            iterator2.MoveNext(); // ObjectDisposedException.
        }

        internal static void Publish2()
        {
            IBuffer<int> publish = TracedRange(0, 5).Publish();
            IEnumerator<int> iterator1 = publish.GetEnumerator();
            // iterator1: 0 1 2 3 4
            IEnumerator<int> iterator2 = publish.GetEnumerator();
            // iterator1: 0 1 2 3 4
            iterator1.MoveNext(); Trace.WriteLine(iterator1.Current); // 0| |Trace: 0
            iterator1.MoveNext(); Trace.WriteLine(iterator1.Current); // 1| |Trace: 1
            iterator1.MoveNext(); Trace.WriteLine(iterator1.Current); // 2| |Trace: 2
            iterator2.MoveNext(); Trace.WriteLine(iterator2.Current); //  |0|
            iterator1.MoveNext(); Trace.WriteLine(iterator1.Current); // 3| |Trace: 3
            iterator2.MoveNext(); Trace.WriteLine(iterator2.Current); //  |1|
            iterator1.MoveNext(); Trace.WriteLine(iterator1.Current); // 4| |Trace: 4

            publish.Dispose();
            iterator1.MoveNext(); // ObjectDisposedException.
            iterator2.MoveNext(); // ObjectDisposedException.
        }

        internal static void PublishWithSelector1()
        {
            IEnumerable<int> source1 = TracedRange(0, 5);
            Tuple<int, int>[] concat1 = source1.Zip(source1, Tuple.Create).ToArray();
            // (0, 0), (1, 1), (2, 2), (3, 3), (4, 4)
            // Trace: 0 0 1 1 2 2 3 3 4 4

            IEnumerable<int> source2 = TracedRange(0, 5).Publish();
            Tuple<int, int>[] concat2 = source2.Zip(source2, Tuple.Create).ToArray();
            // (0, 0), (1, 1), (2, 2), (3, 3), (4, 4)
            // Trace: 0 1 2 3 4

            IEnumerable<int> source3 = TracedRange(0, 5);
            Tuple<int, int>[] concat3 = source3.Publish(source => source.Zip(source, Tuple.Create)).ToArray();
            // (0, 0), (1, 1), (2, 2), (3, 3), (4, 4)
            // Trace: 0 1 2 3 4
        }

        internal static void Memoize()
        {
            IEnumerable<int> source1 = TracedRange(0, 5);
            int[] concat1 = source1.Concat(source1).ToArray();
            // 0 1 2 3 4 0 1 2 3 4.
            // Trace: 0 1 2 3 4 0 1 2 3 4

            IBuffer<int> source2 = TracedRange(0, 5).Share();
            int[] concat2 = source2.Concat(source2).ToArray();
            // 0 1 2 3 4.
            // Trace: 0 1 2 3 4

            IBuffer<int> source3 = TracedRange(0, 5).Publish();
            int[] concat3 = source2.Concat(source2).ToArray();
            // 0 1 2 3 4.
            // Trace: 0 1 2 3 4

            IBuffer<int> source4 = TracedRange(0, 5).Memoize();
            int[] concat4 = source2.Concat(source2).ToArray();
            // 0 1 2 3 4 0 1 2 3 4.
            // Trace: 0 1 2 3 4
        }

        internal static void MemoizeWithSelector()
        {
            IEnumerable<int> source3 = TracedRange(0, 5);
            int[] concat3 = source3.Memoize(source => source.Concat(source)).ToArray();
            // 0 1 2 3 4 0 1 2 3 4.
            // Trace: 0 1 2 3 4
        }

        internal static void MemoizeWithReaderCount()
        {
            IEnumerable<int> source1 = TracedRange(0, 5).Memoize(2);
            int[] reader1 = source1.ToArray();
            int[] reader2 = source1.ToArray();
            int[] reader3 = source1.ToArray(); // InvalidOperationException.

            IEnumerable<int> source2 = TracedRange(0, 5).Memoize(2);
            int[] concat2 = source2 // First full iteration.
                .Concat(source2) // Second full iteration.
                .Concat(source2).ToArray(); // InvalidOperationException.

            IEnumerable<int> source3 = TracedRange(0, 5);
            int[] concat3 = source3.Memoize(2, source => source.Concat(source).Concat(source))
                .ToArray(); // InvalidOperationException.
        }

        #endregion

        #region Exception

        internal static void Throw()
        {
            IEnumerable<int> @throw = EnumerableEx.Throw<int>(new OperationCanceledException(
                $"{nameof(OperationCanceledException)} from {nameof(EnumerableEx.Throw)}"));
            IEnumerable<int> query = Enumerable.Range(0, 5).Concat(@throw);
            try
            {
                foreach (int value in query)
                {
                    Trace.WriteLine(value);
                }
            }
            catch (OperationCanceledException exception)
            {
                Trace.WriteLine(exception.Message);
            }
            // 0 1 2 3 4 "OperationCanceledException from Throw".
        }

        internal static void CatchWithHandler()
        {
            IEnumerable<string> @throw = EnumerableEx.Throw<string>(new OperationCanceledException());
            IEnumerable<string> @catch = @throw.Catch<string, OperationCanceledException>(
                exception => EnumerableEx.Return($"Handled {exception.GetType().Name}: {exception.Message}"));
            string[] strings = @catch.ToArray(); // Handled OperationCanceledException: The operation was canceled.
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        internal static void Catch()
        {
            IEnumerable<int> scan = Enumerable.Repeat(0, 5).Scan((a, b) => a / b);
            IEnumerable<int> range = Enumerable.Range(0, 5);
            IEnumerable<int> cast = new object[] { 5, "a" }.Cast<int>();
            IEnumerable<IEnumerable<int>> source1 = new IEnumerable<int>[] { scan, range, cast };
            foreach (int value in source1.Catch())
            {
                Trace.WriteLine(value);
            }
            // 0 1 2 3 4.

            IEnumerable<IEnumerable<int>> source2 = new IEnumerable<int>[] { scan, cast };
            try
            {
                foreach (int value in source2.Catch())
                {
                    Trace.WriteLine(value);
                }
            }
            catch (Exception exception)
            {
                Trace.WriteLine(exception.Message);
            }
            // 5 "Specified cast is not valid."
        }

        #endregion

        #region Iteration

        internal static void Do()
        {
            IEnumerable<int> query = Enumerable
                .Range(0, 5).Do(
                    onNext: value => Trace.WriteLine($"Range: {value}"),
                    onCompleted: () => Trace.WriteLine($"Range completes."))
                .Scan((a, b) => a + b).Do(
                    onNext: value => Trace.WriteLine($"Scan: {value}"),
                    onCompleted: () => Trace.WriteLine($"Scan completes."))
                .TakeLast(2).Do(
                    onNext: value => Trace.WriteLine($"TakeLast: {value}"),
                    onCompleted: () => Trace.WriteLine($"TakeLast completes.")); // Defines query.

            foreach (int result in query) // Executes query.
            {
                Trace.WriteLine($"Pulled result: {result}");
            }
        }

        #endregion

        #region Aggregation

        internal static void MinByMaxBy()
        {
            IList<Character> min = Characters()
                .MinBy(character => character.Name, StringComparer.OrdinalIgnoreCase); // { JAVIS }.
            IList<Character> max = Characters()
                .MaxBy(character => character.Name, StringComparer.OrdinalIgnoreCase); // { Vision }.
        }

        internal static void MaxBy()
        {
            IList<Type> maxTypes = mscorlib.ExportedTypes.MaxBy(type => type.GetPublicMembers().Length);
            // { System.Convert }.
        }

        #endregion
    }
}
