﻿namespace Dixin.Linq.Parallel
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

#if NETFX
    using Microsoft.ConcurrencyVisualizer.Instrumentation;
#endif

    using static Functions;

    using Parallel = System.Threading.Tasks.Parallel;

    internal static partial class Partitioning
    {
        internal static void Range()
        {
            int[] array = Enumerable.Range(0, Environment.ProcessorCount * 4).ToArray();
            array.AsParallel().Visualize(value => Compute(value), nameof(Range));
        }
    }

    internal static partial class Partitioning
    {
        internal static void Strip()
        {
            IEnumerable<int> source = Enumerable.Range(0, Environment.ProcessorCount * 4);
            source.AsParallel().Visualize(ParallelEnumerable.Select, value => Compute(value));
        }

        internal static void StripLoadBalance()
        {
            IEnumerable<int> source = Enumerable.Range(0, Environment.ProcessorCount * 4);
            source.AsParallel().Visualize(ParallelEnumerable.Select, value => Compute(value % 2));
        }

        internal static void StripForArray()
        {
            int[] array = Enumerable.Range(0, Environment.ProcessorCount * 4).ToArray();
            Partitioner.Create(array, loadBalance: true).AsParallel().Visualize(value => Compute(value), nameof(Range));
        }
    }

    internal struct Data
    {
        internal Data(int value)
        {
            this.Value = value;
        }

        internal int Value { get; }

        public override int GetHashCode() => this.Value % Environment.ProcessorCount;

        public override bool Equals(object obj) => obj is Data && this.GetHashCode() == ((Data)obj).GetHashCode();

        public override string ToString() => this.Value.ToString();
    }

    internal static partial class Partitioning
    {
        internal static void HashInGroupBy()
        {
            IEnumerable<Data> source = new int[] { 0, 1, 2, 2, 2, 2, 3, 4, 5, 6, 10 }.Select(value => new Data(value));
            source.AsParallel()
                .Visualize(
                    (parallelQuery, elementSelector) => parallelQuery.GroupBy(
                        keySelector: data => data, // Key object's GetHashCode will be called.
                        elementSelector: elementSelector),
                    data => Compute(data.Value)) // elementSelector.
                .ForAll(_ => { });
            // Equivalent to:
            // MarkerSeries markerSeries = Markers.CreateMarkerSeries("Parallel");
            // source.AsParallel()
            //   .GroupBy(
            //       keySelector: data => data,
            //       elementSelector: data =>
            //           {
            //               using (markerSeries.EnterSpan(Thread.CurrentThread.ManagedThreadId, data.ToString()))
            //               {
            //                   Compute(data.Value);
            //               }

            //               return data;
            //           })
            //   .ForAll(_ => { });
        }

        internal static void HashInJoin()
        {
            IEnumerable<Data> outerSource = new int[] { 0, 1, 2, 2, 2, 2, 3, 6 }.Select(value => new Data(value));
            IEnumerable<Data> innerSource = new int[] { 4, 5, 6, 7 }.Select(value => new Data(value));
            outerSource.AsParallel()
                .Visualize(
                    (parallelQuery, resultSelector) => parallelQuery
                        .Join(
                            inner: innerSource.AsParallel(),
                            outerKeySelector: data => data, // Key object's GetHashCode will be called.
                            innerKeySelector: data => data, // Key object's GetHashCode will be called.
                            resultSelector: (outerData, innerData) => resultSelector(outerData)),
                    data => Compute(data.Value)) // resultSelector.
                .ForAll(_ => { });
        }

        internal static void Chunk()
        {
            IEnumerable<int> source = Enumerable.Range(0, (1 + 2) * 3 * Environment.ProcessorCount + 3);
            Partitioner.Create(source, EnumerablePartitionerOptions.None).AsParallel()
                .Visualize(ParallelEnumerable.Select, _ => Compute())
                .ForAll(_ => { });
        }
    }

    public class StaticPartitioner<TSource> : Partitioner<TSource>
    {
        protected readonly IBuffer<TSource> buffer;

        public StaticPartitioner(IEnumerable<TSource> source)
        {
            this.buffer = source.Share();
        }

        public override IList<IEnumerator<TSource>> GetPartitions(int partitionCount)
        {
            if (partitionCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(partitionCount), $"{partitionCount} must be greater than 0.");
            }

            return Enumerable
                .Range(0, partitionCount)
                .Select(_ => this.buffer.GetEnumerator())
                .ToArray();
        }
    }

    public class DynamicPartitioner<TSource> : StaticPartitioner<TSource>
    {
        public DynamicPartitioner(IEnumerable<TSource> source) : base(source)
        {
        }

        public override bool SupportsDynamicPartitions => true;

        public override IEnumerable<TSource> GetDynamicPartitions() => this.buffer;
    }

    internal static partial class Partitioning
    {
        internal static void StaticPartitioner()
        {
            IEnumerable<int> source = Enumerable.Range(0, Environment.ProcessorCount * 4);
            new StaticPartitioner<int>(source).AsParallel()
                .Visualize(ParallelEnumerable.Select, value => Compute(value))
                .ForAll(_ => { });
        }

        internal static void DynamicPartitioner()
        {
            IEnumerable<int> source = Enumerable.Range(0, Environment.ProcessorCount * 4);
            Parallel.ForEach(new DynamicPartitioner<int>(source), value => Compute(value));
        }

#if NETFX
        internal static void VisualizeDynamicPartitioner()
        {
            IEnumerable<int> source = Enumerable.Range(0, Environment.ProcessorCount * 4);
            MarkerSeries markerSeries = Markers.CreateMarkerSeries(nameof(Parallel));
            Parallel.ForEach(
                new DynamicPartitioner<int>(source),
                value =>
                {
                    using (markerSeries.EnterSpan(Thread.CurrentThread.ManagedThreadId, value.ToString()))
                    {
                        Compute(value);
                    }
                });
        }
#endif

        internal static IList<IList<TSource>> GetPartitions<TSource>(IEnumerable<TSource> partitionsSource, int partitionCount)
        {
            List<IList<TSource>> partitions = Enumerable
                .Range(0, partitionCount)
                .Select<int, IList<TSource>>(_ => new List<TSource>())
                .ToList();
            Thread[] partitioningThreads = Enumerable
                .Range(0, partitionCount)
                .Select(_ => partitionsSource.GetEnumerator())
                .Select((partitionIterator, partitionIndex) => new Thread(() =>
                    {
                        IList<TSource> partition = partitions[partitionIndex];
                        using (partitionIterator)
                        {
                            while (partitionIterator.MoveNext())
                            {
                                partition.Add(partitionIterator.Current);
                            }
                        }
                    }))
                .ToArray();
            partitioningThreads.ForEach(thread => thread.Start());
            partitioningThreads.ForEach(thread => thread.Join());
            return partitions;
        }
    }
}

#if DEMO
namespace System.Collections.Concurrent
{
    using System.Collections.Generic;

    public abstract class Partitioner<TSource>
    {
        protected Partitioner()
        {
        }

        public virtual bool SupportsDynamicPartitions => false;

        public abstract IList<IEnumerator<TSource>> GetPartitions(int partitionCount);

        public virtual IEnumerable<TSource> GetDynamicPartitions()
        {
            throw new NotSupportedException("Dynamic partitions are not supported by this partitioner.");
        }
    }
}

namespace System.Threading.Tasks
{
    using System.Collections.Concurrent;

    public static class Parallel
    {
        public static ParallelLoopResult ForEach<TSource>(Partitioner<TSource> source, Action<TSource> body);
    }
}
#endif