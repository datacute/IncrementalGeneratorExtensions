#if !DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAYINSTANCECACHE // Feature: EquatableImmutableArrayInstanceCache (optional)
#if !DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAY // Dependency: EquatableImmutableArray
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace Datacute.IncrementalGeneratorExtensions
{
    // The source generation pipelines compare these a lot
    // so being able to quickly tell when they are different
    // is important.
    // We will use an instance cache to find when we can reuse
    // an existing object, massively speeding up the Equals call.

    /// <summary>
    /// A registry that keeps track of all generic instances of <see cref="EquatableImmutableArrayInstanceCache{T}"/>
    /// so that their caches can be cleared in a single operation.
    /// </summary>
    public static class EquatableImmutableArrayInstanceCacheRegistry
    {
        private static Action _clearActions;
        // ReSharper disable once ChangeFieldTypeToSystemThreadingLock shared source code needs to work in netStandard 2.0
        private static readonly object SyncRoot = new object();

        /// <summary>
        /// Registers a cache clearer action for a specific generic instance.
        /// </summary>
        /// <param name="clearAction">The action that clears the cache for a specific type.</param>
        public static void RegisterClearAction(Action clearAction)
        {
            lock (SyncRoot)
            {
                if (clearAction != null)
                {
                    _clearActions += clearAction;
                }
            }
        }

        /// <summary>
        /// Clears all registered instance caches.
        /// </summary>
        public static void ClearAll()
        {
            Action actions;
            lock (SyncRoot)
            {
                actions = _clearActions;
            }
            
            actions?.Invoke();
        }
    }

    /// <summary>
    /// A cache for instances of <see cref="EquatableImmutableArray{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements in the array, which must implement <see cref="IEquatable{T}"/>.</typeparam>
    public static class EquatableImmutableArrayInstanceCache<T> where T : IEquatable<T>
    {
        // Small MRU fast-path to avoid dictionary/lock/WeakReference work for very hot keys.
        // Power-of-two size chosen as a trade-off between footprint and hit-rate.
        private const int MruSize = 64; // must be power of two
        private const int MruMask = MruSize - 1;

        // ReSharper disable StaticMemberInGenericType
        private static readonly int[] MruLengths;
        private static readonly int[] MruFirstHashes;
        // ReSharper restore StaticMemberInGenericType
        private static readonly EquatableImmutableArray<T>[] MruInstances;

        // Two-level dictionary cache: length -> composite bucket hash -> list of instances
        // Preceded by an MRU fast-path based on length and first element hash.
        // Because this is a generic class, there is a separate static cache for each type T
        private static readonly ConcurrentDictionary<int, ConcurrentDictionary<int, List<WeakReference<EquatableImmutableArray<T>>>>> Cache;

        // Pre-allocated factory delegates to avoid per-call delegate allocations on .NET Framework.
        private static readonly Func<int, ConcurrentDictionary<int, List<WeakReference<EquatableImmutableArray<T>>>>> LengthDictFactory;

        private static readonly Func<int, List<WeakReference<EquatableImmutableArray<T>>>> CandidateListFactory;

        // Explicit static constructor removes the beforefieldinit flag. On .NET Framework,
        // this forces the JIT to check initialization exactly once upon first access to the
        // class, rather than injecting state checks before every static field read in our MRU
        // hot-path, which measurably improves performance.
        static EquatableImmutableArrayInstanceCache()
        {
            MruLengths = new int[MruSize];
            MruFirstHashes = new int[MruSize];
            MruInstances = new EquatableImmutableArray<T>[MruSize];

            Cache = new ConcurrentDictionary<int, ConcurrentDictionary<int, List<WeakReference<EquatableImmutableArray<T>>>>>();
            LengthDictFactory = _ => new ConcurrentDictionary<int, List<WeakReference<EquatableImmutableArray<T>>>>();
            CandidateListFactory = _ => new List<WeakReference<EquatableImmutableArray<T>>>();
            
            EquatableImmutableArrayInstanceCacheRegistry.RegisterClearAction(Clear);
        }

        private struct SweepTarget
        {
            public int Length;
            public int BucketHash;
            public List<WeakReference<EquatableImmutableArray<T>>> CandidateList;
        }

        // Circular buffer of recently observed candidate lists, used by SweepStaleBuckets
        // to avoid enumerating ConcurrentDictionary (which boxes on .NET Framework).
        private const int SweepRingSize = 1024;
        private static readonly SweepTarget[] SweepRing = new SweepTarget[SweepRingSize];
        // ReSharper disable once StaticMemberInGenericType the index belongs to the SweepRing
        // and is intended to be one per type.
        private static int _sweepRingIndex;

        // Number of buckets inspected during a post-miss sweep.  Large enough to make a
        // meaningful dent in stale entries without adding noticeable cost to the miss path.
        private const int SweepBatchSize = 16;

        /// <summary>
        /// Clears the cache, removing all stored instances.
        /// </summary>
        public static void Clear()
        {
            Array.Clear(MruLengths, 0, MruSize);
            Array.Clear(MruFirstHashes, 0, MruSize);
            Array.Clear(MruInstances, 0, MruSize);
            Cache.Clear();
            Array.Clear(SweepRing, 0, SweepRingSize);
            
#if !DATACUTE_EXCLUDE_LIGHTWEIGHTTRACE && !DATACUTE_EXCLUDE_GENERATORSTAGE
            SetCount(GeneratorStage.EquatableImmutableArrayInstanceCacheSize, 0L);
#endif
        }

#if !DATACUTE_EXCLUDE_LIGHTWEIGHTTRACE && !DATACUTE_EXCLUDE_GENERATORSTAGE
        private static readonly int TypeMapId = LightweightTrace.RegisterName(typeof(T).ToString());

        private static void SetCount<TEnum>(TEnum counterId, long count) where TEnum : Enum
        {
            LightweightTrace.SetCount(counterId, TypeMapId, mapValue: true, count);
        }
        private static void IncrementCount<TEnum>(TEnum counterId) where TEnum : Enum
        {
            LightweightTrace.IncrementCount(counterId, TypeMapId, mapValue: true);
        }
        private static void DecrementCount<TEnum>(TEnum counterId) where TEnum : Enum
        {
            LightweightTrace.DecrementCount(counterId, TypeMapId, mapValue: true);
        }
#endif

        /// <summary>
        /// Gets or creates an instance of <see cref="EquatableImmutableArray{T}"/> from the provided values.
        /// </summary>
        /// <param name="values">The immutable array of values to create the instance from.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the operation to complete.</param>
        /// <returns>An instance of <see cref="EquatableImmutableArray{T}"/> containing the provided values.</returns>
        public static EquatableImmutableArray<T> GetOrCreate(ImmutableArray<T> values, CancellationToken cancellationToken = default)
        {
            if (values.IsEmpty)
                return EquatableImmutableArray<T>.Empty;

            // If we were to calculate the hash of the entire array first, and find
            // matching instances based on that, we would still have to check each element
            // for equality. Instead, we will first find a small number of potentially equal arrays,
            // and then check each element for equality, since that is required anyway.
            // If we find a match, we've saved the time to compute the full hash.

            // To quickly narrow down the candidates for equality checks,
            // this implementation uses a two-level cache, preceded by an MRU fast-path:
            // 1. The MRU fast-path is based on a hash of the length and first element.
            // 2. The first dictionary level is based on the length of the array.
            // 3. The second dictionary level is a composite bucket hash of the first and last elements.
            // Using multi-element hashing reduces collisions when arrays differ only at the ends,
            // avoiding O(n) scans of large candidate lists.

            var comparer = EqualityComparer<T>.Default;
            var length = values.Length;
            var firstElementHash = values[0] == null ? 0 : comparer.GetHashCode(values[0]);

            // MRU fast-path: check a small, lock-free table of recent hot entries.
            // Probe order: read small ints first (length/hash) to avoid
            // dereferencing the instance unless we have a strong match candidate.
            int mruIndex = (firstElementHash ^ length) & MruMask;

            // Opportunistic reads (no Volatile) to avoid memory barrier overhead on .NET Framework.
            // Torn reads are not possible for 32-bit ints, and stale values just cause a safe miss.
            if (MruLengths[mruIndex] == length && MruFirstHashes[mruIndex] == firstElementHash)
            {
                var mruCandidate = MruInstances[mruIndex];
                if (mruCandidate != null)
                {
                    bool match = true;
                    for (int i = 0; i < length; i++)
                    {
                        if (!comparer.Equals(values[i], mruCandidate[i]))
                        {
                            match = false;
                            break;
                        }
                    }

                    if (match)
                    {
#if !DATACUTE_EXCLUDE_LIGHTWEIGHTTRACE && !DATACUTE_EXCLUDE_GENERATORSTAGE
                        IncrementCount(GeneratorStage.EquatableImmutableArrayCacheHit);
#endif
                        return mruCandidate;
                    }
                }
            }

            var bucketHash = firstElementHash;
            if (length > 1)
            {
                var lastHash = values[length - 1] == null ? 0 : comparer.GetHashCode(values[length - 1]);
                bucketHash = EquatableImmutableArray<T>.HashHelpers_Combine(bucketHash, lastHash);
            }

            // Get or create the length-based dictionary
            if (!Cache.TryGetValue(length, out var lengthDict))
                lengthDict = Cache.GetOrAdd(length, LengthDictFactory);

            // Get or create the hash-based list
            if (!lengthDict.TryGetValue(bucketHash, out var candidateList))
            {
                candidateList = lengthDict.GetOrAdd(bucketHash, CandidateListFactory);
                
                // Track this list for allocation-free sweeping
                var idx = (uint)Interlocked.Increment(ref _sweepRingIndex) % SweepRingSize;
                SweepRing[idx] = new SweepTarget { Length = length, BucketHash = bucketHash, CandidateList = candidateList };
            }

            // result is set to the matched or newly created instance inside the lock.
            // isMiss is set when no cached instance was found, to trigger a post-lock sweep.
            // The lock block intentionally has no early returns: calling SweepStaleBuckets
            // inside the lock would risk deadlock (sweep locks other lists; another thread
            // could hold one of those lists and be waiting for this one).
            EquatableImmutableArray<T> result = null;
            bool isMiss = false;

            lock (candidateList)
            {
                // Check for matches (backwards to allow removal of dead references)
                for (int i = candidateList.Count - 1; i >= 0; i--)
                {
#if !DATACUTE_EXCLUDE_LIGHTWEIGHTTRACEEXTENSIONS && !DATACUTE_EXCLUDE_GENERATORSTAGE
                    cancellationToken.ThrowIfCancellationRequested(0);
#else
                    cancellationToken.ThrowIfCancellationRequested();
#endif
                    if (candidateList[i].TryGetTarget(out var existing))
                    {
                        // Check if this candidate matches element by element
                        bool isMatch = true;

                        for (int elementIndex = 0; elementIndex < length; elementIndex++)
                        {
                            if (!comparer.Equals(values[elementIndex], existing[elementIndex]))
                            {
                                isMatch = false;
                                break;
                            }
                        }

                        if (isMatch)
                        {
                            result = existing;
#if !DATACUTE_EXCLUDE_LIGHTWEIGHTTRACE && !DATACUTE_EXCLUDE_GENERATORSTAGE
                            IncrementCount(GeneratorStage.EquatableImmutableArrayCacheHit);
#endif
                            break;
                        }
                    }
                    else
                    {
                        // Clean up dead references
#if !DATACUTE_EXCLUDE_LIGHTWEIGHTTRACE && !DATACUTE_EXCLUDE_GENERATORSTAGE
                        IncrementCount(GeneratorStage.EquatableImmutableArrayCacheWeakReferenceRemoved);
                        DecrementCount(GeneratorStage.EquatableImmutableArrayInstanceCacheSize);
#endif
                        candidateList.RemoveAt(i);
                    }
                }

                if (result == null)
                {
                    // No match found; calculate hash and create new instance
                    var hash = EquatableImmutableArray<T>.CalculateHashCode(values, comparer, firstElementHash, 1);
#if !DATACUTE_EXCLUDE_LIGHTWEIGHTTRACE && !DATACUTE_EXCLUDE_GENERATORSTAGE
                    // Record a miss and track the number of cached instances for this type
                    IncrementCount(GeneratorStage.EquatableImmutableArrayCacheMiss);
                    IncrementCount(GeneratorStage.EquatableImmutableArrayInstanceCacheSize);
#endif
                    result = new EquatableImmutableArray<T>(values, hash);
                    candidateList.Add(new WeakReference<EquatableImmutableArray<T>>(result));

                    // Update MRU slot for this key so future hot lookups can take the fast path.
                    // Publish small ints first, instance reference last.
                    // We only need Volatile.Write on the instance to ensure safe publication.
                    MruLengths[mruIndex] = length;
                    MruFirstHashes[mruIndex] = firstElementHash;
                    Volatile.Write(ref MruInstances[mruIndex], result);

                    isMiss = true;
                }
            }

            // On a miss we have already paid the allocation cost, so use the opportunity
            // to sweep a bounded number of other buckets in lengthDict: remove dead
            // WeakReferences and, if a bucket becomes empty, remove it from the dictionary.
            // This keeps the dictionary size bounded without adding any cost to the hit path.
            if (isMiss)
                SweepStaleBuckets(length, bucketHash);

            return result;
        }

        /// <summary>
        /// Inspects up to <see cref="SweepBatchSize"/> recent buckets tracked in the sweep ring
        /// and removes dead <see cref="WeakReference{T}"/> entries. Empty buckets are removed from the dictionary.
        /// </summary>
        /// <remarks>
        /// Called only on the miss path, after the caller's <c>candidateList</c> lock has been
        /// released. Overcomes .NET Framework enumeration allocations by iterating a circular array.
        /// </remarks>
        /// <param name="currentLength">The length of the bucket just populated.</param>
        /// <param name="currentBucketHash">The hash of the bucket just populated.</param>
        private static void SweepStaleBuckets(int currentLength, int currentBucketHash)
        {
            int startIdx = Interlocked.Increment(ref _sweepRingIndex);
            
            for (int i = 0; i < SweepBatchSize; i++)
            {
                int index = (int)((uint)(startIdx + i) % SweepRingSize);
                var target = SweepRing[index];
                
                if (target.CandidateList == null) continue;
                if (target.Length == currentLength && target.BucketHash == currentBucketHash) continue;

                var list = target.CandidateList;
                bool isEmpty;
                lock (list)
                {
                    for (int j = list.Count - 1; j >= 0; j--)
                    {
                        if (!list[j].TryGetTarget(out _))
                        {
#if !DATACUTE_EXCLUDE_LIGHTWEIGHTTRACE && !DATACUTE_EXCLUDE_GENERATORSTAGE
                            IncrementCount(GeneratorStage.EquatableImmutableArrayCacheWeakReferenceRemoved);
                            DecrementCount(GeneratorStage.EquatableImmutableArrayInstanceCacheSize);
#endif
                            list.RemoveAt(j);
                        }
                    }
                    isEmpty = list.Count == 0;
                }

                if (isEmpty)
                {
                    // Re-check under the lock before removing to narrow (not eliminate)
                    // the window in which another thread's new entry could be orphaned.
                    lock (list)
                    {
                        if (list.Count == 0)
                        {
                            if (Cache.TryGetValue(target.Length, out var lengthDict))
                                lengthDict.TryRemove(target.BucketHash, out _);
                        }
                    }
                }
            }
        }
    }
}
#endif // !DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAY
#endif // !DATACUTE_EXCLUDE_EQUATABLEIMMUTABLEARRAYINSTANCECACHE
