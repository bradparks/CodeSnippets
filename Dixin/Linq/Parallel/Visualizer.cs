namespace Dixin.Linq.Parallel
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
#if NETFX
    using System.Threading;

    using Microsoft.ConcurrencyVisualizer.Instrumentation;
#endif

    public static partial class Visualizer
    {
        internal const string Parallel = nameof(Parallel);

        internal const string Sequential = nameof(Sequential);

        internal static void Visualize<TSource>(
            this IEnumerable<TSource> source, Action<TSource> action, string span = Sequential, int category = 0)
        {
#if NETFX
            using (Markers.EnterSpan(category, span))
            {
                MarkerSeries markerSeries = Markers.CreateMarkerSeries(span);
                source.ForEach(value =>
                {
                    using (markerSeries.EnterSpan(Thread.CurrentThread.ManagedThreadId, value.ToString()))
                    {
                        action(value);
                    }
                });
            }
#endif
        }

        internal static void Visualize<TSource>(
            this ParallelQuery<TSource> source, Action<TSource> action, string span = Parallel, int category = 1)
        {
#if NETFX
            using (Markers.EnterSpan(category, span))
            {
                MarkerSeries markerSeries = Markers.CreateMarkerSeries(span);
                source.ForAll(value =>
                {
                    using (markerSeries.EnterSpan(Thread.CurrentThread.ManagedThreadId, value.ToString()))
                    {
                        action(value);
                    }
                });
            }
#endif
        }
    }

    public static partial class Visualizer
    {
        internal static IEnumerable<TResult> Visualize<TSource, TMiddle, TResult>(
            this IEnumerable<TSource> source,
            Func<IEnumerable<TSource>, Func<TSource, TMiddle>, IEnumerable<TResult>> query,
            Func<TSource, TMiddle> func,
            Func<TSource, string> funcSpan = null,
            string span = Sequential,
            int category = 0)
        {
#if NETFX
            MarkerSeries markerSeries = Markers.CreateMarkerSeries(span);
            return query(
                source,
                value =>
                {
                    using (markerSeries.EnterSpan(
                        category, funcSpan?.Invoke(value) ?? value.ToString()))
                    {
                        return func(value);
                    }
                });
#else
            return query(source, func);
#endif
        }

        internal static ParallelQuery<TResult> Visualize<TSource, TMiddle, TResult>(
            this ParallelQuery<TSource> source,
            Func<ParallelQuery<TSource>, Func<TSource, TMiddle>, ParallelQuery<TResult>> query,
            Func<TSource, TMiddle> func,
            Func<TSource, string> funcSpan = null,
            string span = Parallel)
        {
#if NETFX
            MarkerSeries markerSeries = Markers.CreateMarkerSeries(span);
            return query(
                source,
                value =>
                {
                    using (markerSeries.EnterSpan(
                        Thread.CurrentThread.ManagedThreadId, funcSpan?.Invoke(value) ?? value.ToString()))
                    {
                        return func(value);
                    }
                });
#else
            return query(source, func);
#endif
        }
    }

    public static partial class Visualizer
    {
        internal static TSource Visualize<TSource>(
            this ParallelQuery<TSource> source,
            Func<ParallelQuery<TSource>, Func<TSource, TSource, TSource>, TSource> aggregate,
            Func<TSource, TSource, TSource> func,
            string span = nameof(ParallelEnumerable.Aggregate))
        {
#if NETFX
            MarkerSeries markerSeries = Markers.CreateMarkerSeries(span);
            return aggregate(
                source,
                (accumulate, value) =>
                {
                    using (markerSeries.EnterSpan(Thread.CurrentThread.ManagedThreadId, $"{accumulate} {value}"))
                    {
                        return func(accumulate, value);
                    }
                });
#else
            return aggregate(source, func);
#endif
        }

        internal static TAccumulate Visualize<TSource, TAccumulate>(
            this ParallelQuery<TSource> source,
            Func<ParallelQuery<TSource>, TAccumulate, Func<TAccumulate, TSource, TAccumulate>, TAccumulate> aggregate,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> func,
            string span = nameof(ParallelEnumerable.Aggregate))
        {
#if NETFX
            MarkerSeries markerSeries = Markers.CreateMarkerSeries(span);
            return aggregate(
                source,
                seed,
                (accumulate, value) =>
                {
                    using (markerSeries.EnterSpan(Thread.CurrentThread.ManagedThreadId, value.ToString()))
                    {
                        return func(accumulate, value);
                    }
                });
#else
            return aggregate(source, seed, func);
#endif
        }

        internal static TResult Visualize<TSource, TAccumulate, TResult>(
            this ParallelQuery<TSource> source,
            Func<ParallelQuery<TSource>, TAccumulate, Func<TAccumulate, TSource, TAccumulate>, Func<TAccumulate, TResult>, TResult> aggregate,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> func,
            Func<TAccumulate, TResult> resultSelector,
            string span = nameof(ParallelEnumerable.Aggregate))
        {
#if NETFX
            MarkerSeries markerSeries = Markers.CreateMarkerSeries(span);
            return aggregate(
                source,
                seed,
                (accumulate, value) =>
                {
                    using (markerSeries.EnterSpan(Thread.CurrentThread.ManagedThreadId, value.ToString()))
                    {
                        return func(accumulate, value);
                    }
                },
                resultSelector);
#else
            return aggregate(source, seed, func, resultSelector);
#endif
        }

        internal static TResult Visualize<TSource, TAccumulate, TResult>(
            this ParallelQuery<TSource> source,
            Func<ParallelQuery<TSource>, TAccumulate, Func<TAccumulate, TSource, TAccumulate>, Func<TAccumulate, TAccumulate, TAccumulate>, Func<TAccumulate, TResult>, TResult> aggregate,
            TAccumulate seed,
            Func<TAccumulate, TSource, TAccumulate> updateAccumulatorFunc,
            Func<TAccumulate, TAccumulate, TAccumulate> combineAccumulatorsFunc,
            Func<TAccumulate, TResult> resultSelector,
            string span = nameof(ParallelEnumerable.Aggregate))

        {
#if NETFX
            MarkerSeries markerSeries = Markers.CreateMarkerSeries(span);
            return aggregate(
                source,
                seed,
                (accumulate, value) =>
                    {
                        using (markerSeries.EnterSpan(Thread.CurrentThread.ManagedThreadId, $"{accumulate} {value}"))
                        {
                            return updateAccumulatorFunc(accumulate, value);
                        }
                    },
                (accumulates, accumulate) =>
                    {
                        using (markerSeries.EnterSpan(Thread.CurrentThread.ManagedThreadId, $"{accumulates} {accumulate}"))
                        {
                            return combineAccumulatorsFunc(accumulates, accumulate);
                        }
                    },
                resultSelector);
#else
            return aggregate(source, seed, updateAccumulatorFunc, combineAccumulatorsFunc, resultSelector);
#endif
        }

        internal static TResult Visualize<TSource, TAccumulate, TResult>(
            this ParallelQuery<TSource> source,
            Func<ParallelQuery<TSource>, Func<TAccumulate>, Func<TAccumulate, TSource, TAccumulate>, Func<TAccumulate, TAccumulate, TAccumulate>, Func<TAccumulate, TResult>, TResult> aggregate,
            Func<TAccumulate> seedFactory,
            Func<TAccumulate, TSource, TAccumulate> updateAccumulatorFunc,
            Func<TAccumulate, TAccumulate, TAccumulate> combineAccumulatorsFunc,
            Func<TAccumulate, TResult> resultSelector,
            string span = nameof(ParallelEnumerable.Aggregate))

        {
#if NETFX
            MarkerSeries markerSeries = Markers.CreateMarkerSeries(span);
            return aggregate(
                source,
                seedFactory,
                (accumulate, value) =>
                {
                    using (markerSeries.EnterSpan(Thread.CurrentThread.ManagedThreadId, $"{accumulate} {value}"))
                    {
                        return updateAccumulatorFunc(accumulate, value);
                    }
                },
                (accumulates, accumulate) =>
                {
                    using (markerSeries.EnterSpan(Thread.CurrentThread.ManagedThreadId, $"{accumulates} {accumulate}"))
                    {
                        return combineAccumulatorsFunc(accumulates, accumulate);
                    }
                },
                resultSelector);
#else
            return aggregate(source, seedFactory, updateAccumulatorFunc, combineAccumulatorsFunc, resultSelector);
#endif
        }
    }
}
