using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Course
{
    public sealed class ObjectPool<T, TObjectLifecycle> : ObjectPool<T, TObjectLifecycle, NonThreadAwareBehavior>
        where T : class
        where TObjectLifecycle : struct, IObjectLifecycle<T>
    {
        public ObjectPool(int size = 16, TObjectLifecycle factory = default(TObjectLifecycle), NonThreadAwareBehavior behavior = default(NonThreadAwareBehavior)) : base(size, factory, behavior)
        {}
    }

    public class ObjectPool<T, TObjectLifecycle, TProcessAwareBehavior>
        where T : class
        where TObjectLifecycle : struct, IObjectLifecycle<T>
        where TProcessAwareBehavior : struct, IProcessAwareBehavior
    {
        private static readonly TObjectLifecycle Lifecycle = new TObjectLifecycle();

        private struct Element
        {
            internal T Value;
        }

        [StructLayout(LayoutKind.Sequential, Size = 128)]
        private struct CacheAwareElement
        {
            private readonly long _pad1;
            private T Value1;
            private T Value2;
            private T Value3;
            private T Value4;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Clear<TEvictionStrategy>(ref CacheAwareElement bucket, ref TEvictionStrategy policy)
                where TEvictionStrategy : struct, IEvictionStrategy<T>
            {
                // Note that the initial read is optimistically not synchronized. That is intentional. 
                // We will interlock only when we have a candidate. in a worst case we may miss some
                // recently returned objects. Not a big deal.
                T inst = bucket.Value1;
                if (inst != null && policy.CanEvict(inst))
                {
                    Interlocked.CompareExchange(ref bucket.Value1, null, inst);
                }

                inst = bucket.Value2;
                if (inst != null && policy.CanEvict(inst))
                {
                    Interlocked.CompareExchange(ref bucket.Value2, null, inst);
                }

                inst = bucket.Value3;
                if (inst != null && policy.CanEvict(inst))
                {
                    Interlocked.CompareExchange(ref bucket.Value3, null, inst);
                }

                inst = bucket.Value4;
                if (inst != null && policy.CanEvict(inst))
                {
                    Interlocked.CompareExchange(ref bucket.Value4, null, inst);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void ClearAndDispose<TEvictionStrategy>(ref CacheAwareElement bucket, ref TEvictionStrategy policy)
                where TEvictionStrategy : struct, IEvictionStrategy<T>
            {
                // Note that the initial read is optimistically not synchronized. That is intentional. 
                // We will interlock only when we have a candidate. in a worst case we may miss some
                // recently returned objects. Not a big deal.
                T inst = bucket.Value1;
                if (inst != null && policy.CanEvict(inst))
                {
                    if (inst == Interlocked.CompareExchange(ref bucket.Value1, null, inst))
                        ((IDisposable)inst).Dispose();
                }

                inst = bucket.Value2;
                if (inst != null && policy.CanEvict(inst))
                {
                    if (inst == Interlocked.CompareExchange(ref bucket.Value2, null, inst))
                        ((IDisposable)inst).Dispose();
                }

                inst = bucket.Value3;
                if (inst != null && policy.CanEvict(inst))
                {
                    if (inst == Interlocked.CompareExchange(ref bucket.Value3, null, inst))
                        ((IDisposable)inst).Dispose();
                }

                inst = bucket.Value4;
                if (inst != null && policy.CanEvict(inst))
                {
                    if (inst == Interlocked.CompareExchange(ref bucket.Value4, null, inst))
                        ((IDisposable)inst).Dispose();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool TryClaim(ref CacheAwareElement bucket, out T item)
            {
                // Note that the initial read is optimistically not synchronized. That is intentional. 
                // We will interlock only when we have a candidate. in a worst case we may miss some
                // recently returned objects. Not a big deal.
                T inst = bucket.Value1;
                if (inst != null && inst == Interlocked.CompareExchange(ref bucket.Value1, null, inst))
                    goto Done;

                inst = bucket.Value2;
                if (inst != null && inst == Interlocked.CompareExchange(ref bucket.Value2, null, inst))
                    goto Done;

                inst = bucket.Value3;
                if (inst != null && inst == Interlocked.CompareExchange(ref bucket.Value3, null, inst))
                    goto Done;

                inst = bucket.Value4;
                if (inst != null && inst == Interlocked.CompareExchange(ref bucket.Value4, null, inst))
                    goto Done;

                item = null;
                return false;

                Done:
                item = inst;
                return true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool TryRelease(ref CacheAwareElement bucket, T value)
            {
                if (null == Interlocked.CompareExchange(ref bucket.Value1, value, null))
                    goto Done;

                if (null == Interlocked.CompareExchange(ref bucket.Value2, value, null))
                    goto Done;

                if (null == Interlocked.CompareExchange(ref bucket.Value3, value, null))
                    goto Done;

                if (null == Interlocked.CompareExchange(ref bucket.Value4, value, null))
                    goto Done;

                return false;

                Done: return true;
            }
        }

        // Storage for the pool objects. The first item is stored in a dedicated field because we
        // expect to be able to satisfy most requests from it.
        private readonly CacheAwareElement[] _firstItems;
        private readonly int _bucketsMask;

        private T _firstElement;
        private int _itemTopCounter;
        private int _itemBottomCounter;
        private readonly uint _itemsMask;
        private readonly Element[] _items;

        private readonly TObjectLifecycle _factory;

        public ObjectPool(int size = 16, TObjectLifecycle factory = default(TObjectLifecycle), TProcessAwareBehavior behavior = default(TProcessAwareBehavior))
        {
            Debug.Assert(size >= 1);
            _factory = factory;

            if (typeof(TProcessAwareBehavior) == typeof(ThreadAwareBehavior))
            {
                int buckets = Bits.NextPowerOf2(behavior.Buckets);
                _bucketsMask = buckets - 1;
                _firstItems = new CacheAwareElement[buckets];
            }

            // PERF: We will always have power of two pools to make operations a lot faster. 
            size = Bits.NextPowerOf2(size);
            size = Math.Max(16, size);

            _items = new Element[size];
            _itemsMask = (uint)size - 1;
        }

        /// <summary>
        /// Produces an instance.
        /// </summary>
        /// <remarks>
        /// Search strategy is a simple linear probing which is chosen for it cache-friendliness.
        /// Note that Free will try to store recycled objects close to the start thus statistically 
        /// reducing how far we will typically search.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Allocate()
        {
            T inst;
            if (typeof(TProcessAwareBehavior) == typeof(ThreadAwareBehavior))
            {
                int threadIndex = Environment.CurrentManagedThreadId & _bucketsMask;
                ref var firstItem = ref _firstItems[threadIndex];
                if (!CacheAwareElement.TryClaim(ref firstItem, out inst))
                {
                    inst = AllocateSlow();
                }
            }
            else
            {
                // PERF: Examine the first element. If that fails, AllocateSlow will look at the remaining elements.
                // Note that the initial read is optimistically not synchronized. That is intentional. 
                // We will interlock only when we have a candidate. in a worst case we may miss some
                // recently returned objects. Not a big deal.

                inst = _firstElement;
                if (inst == null || inst != Interlocked.CompareExchange(ref _firstElement, null, inst))
                {
                    inst = AllocateSlow();
                }
            }

            return inst;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T AllocateSlow()
        {
            var items = _items;

            if (_itemBottomCounter < _itemTopCounter)
            {
                uint claim = ((uint)Interlocked.Increment(ref _itemBottomCounter) - 1) & _itemsMask;

                T inst = items[claim].Value;
                if (inst != null)
                {
                    // WARNING: In a absurdly fast loop this can still fail to get a proper, that is why 
                    // we still use a compare exchange operation instead of using the reference.
                    if (inst == Interlocked.CompareExchange(ref items[claim].Value, null, inst))
                        return inst;
                }
            }

            return _factory.New();
        }

        /// <summary>
        /// Returns objects to the pool.
        /// </summary>
        /// <remarks>
        /// Search strategy is a simple linear probing which is chosen for it cache-friendliness.
        /// Note that Free will try to store recycled objects close to the start thus statistically 
        /// reducing how far we will typically search in Allocate.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Free(T obj)
        {
            Lifecycle.Reset(obj);

            if (typeof(TProcessAwareBehavior) == typeof(ThreadAwareBehavior))
            {
                int threadIndex = Environment.CurrentManagedThreadId & _bucketsMask;
                ref var firstItem = ref _firstItems[threadIndex];

                if (CacheAwareElement.TryRelease(ref firstItem, obj))
                    return;
            }
            else
            {
                if (_firstElement == null)
                {
                    if (null == Interlocked.CompareExchange(ref _firstElement, obj, null))
                        return;
                }
            }

            FreeSlow(obj);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FreeSlow(T obj)
        {
            var items = _items;

            uint current = (uint)Interlocked.Increment(ref _itemTopCounter);
            uint claim = (current - 1) & _itemsMask;

            ref var item = ref items[claim];
            if (item.Value == null)
            {
                // Intentionally not using interlocked here. 
                // In a worst case scenario two objects may be stored into same slot.
                // It is very unlikely to happen and will only mean that one of the objects will get collected.
                item.Value = obj;
            }
        }

        public void Clear(bool partial = true)
        {
            bool doDispose = typeof(IDisposable).IsAssignableFrom(typeof(T));

            if (typeof(TProcessAwareBehavior) == typeof(ThreadAwareBehavior) && !partial)
            {
                for (int i = 0; i < _firstItems.Length; i++)
                {
                    ref var bucket = ref _firstItems[i];
                    while (CacheAwareElement.TryClaim(ref bucket, out var obj))
                    {
                        if (doDispose)
                            ((IDisposable)obj).Dispose();
                    }
                }
            }

            uint current;
            do
            {
                current = (uint)Interlocked.Increment(ref _itemBottomCounter);
                if (current < _itemTopCounter)
                {
                    uint claim = (current - 1) & _itemsMask;

                    T inst = _items[claim].Value;
                    if (inst != null)
                    {
                        // WARNING: In a absurdly fast loop this can still fail to get a proper, that is why 
                        // we still use a compare exchange operation instead of using the reference.
                        if (inst == Interlocked.CompareExchange(ref _items[claim].Value, null, inst) && doDispose)
                        {
                            ((IDisposable)inst).Dispose();
                        }
                    }
                }
            }
            while (current < _itemTopCounter);
        }

        public void Clear<TEvictionStrategy>(TEvictionStrategy evictionStrategy = default(TEvictionStrategy))
            where TEvictionStrategy : struct, IEvictionStrategy<T>
        {
            bool doDispose = typeof(IDisposable).IsAssignableFrom(typeof(T));

            if (typeof(TProcessAwareBehavior) == typeof(ThreadAwareBehavior))
            {
                for (int i = 0; i < _firstItems.Length; i++)
                {
                    ref var bucket = ref _firstItems[i];
                    if (doDispose)
                        CacheAwareElement.ClearAndDispose(ref bucket, ref evictionStrategy);
                    else
                        CacheAwareElement.Clear(ref bucket, ref evictionStrategy);
                }
            }

            uint current = (uint)_itemBottomCounter;
            do
            {
                if (current < _itemTopCounter)
                {
                    uint claim = current & _itemsMask;

                    T inst = _items[claim].Value;
                    if (inst != null && evictionStrategy.CanEvict(inst))
                    {
                        // WARNING: In a absurdly fast loop this can still fail to get a proper, that is why 
                        // we still use a compare exchange operation instead of using the reference.
                        if (inst == Interlocked.CompareExchange(ref _items[claim].Value, null, inst) && doDispose)
                        {
                            ((IDisposable)inst).Dispose();
                        }
                    }
                }
                current++;
            }
            while (current < _itemTopCounter);
        }
    }

    public interface IEvictionStrategy<in T> where T : class
    {
        bool CanEvict(T item);
    }

    public struct AlwaysEvictStrategy<T> : IEvictionStrategy<T> where T : class
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanEvict(T item)
        {
            return true;
        }
    }

    public interface IObjectLifecycle<T> where T : class
    {
        T New();
        void Reset(T value);
    }

    public struct NoResetSupport<T> : IObjectLifecycle<T> where T : class, new()
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T New()
        {
            return new T();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset(T value) { }
    }

    public struct NoResetFactorySupport<T> : IObjectLifecycle<T> where T : class, new()
    {
        /// <remarks>
        /// Not using System.Func{T} because this file is linked into the (debugger) Formatter,
        /// which does not have that type (since it compiles against .NET 2.0).
        /// </remarks>
        public delegate T Factory();

        // factory is stored for the lifetime of the pool. We will call this only when pool needs to
        // expand. compared to "new T()", Func gives more flexibility to implementers and faster
        // than "new T()".
        private readonly Factory _factory;

        public NoResetFactorySupport(Factory factory)
        {
            this._factory = factory;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T New()
        {
            return _factory();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reset(T value) { }
    }


    public interface IProcessAwareBehavior
    {
        int Buckets { get; }
    }

    public struct ThreadAwareBehavior : IProcessAwareBehavior
    {
        private readonly int _buckets;

        public int Buckets
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return _buckets == 0 ? 128 : _buckets; }
        }

        public ThreadAwareBehavior(int buckets = 128)
        {
            _buckets = buckets;
        }
    }

    public struct NonThreadAwareBehavior : IProcessAwareBehavior
    {
        public int Buckets
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return 0; }
        }
    }

    [HardwareCounters(HardwareCounter.InstructionRetired)]
    public class ObjectPoolBenchmark
    {
        public class ObjectToPool
        {
            public struct Behavior : IObjectLifecycle<ObjectToPool>
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public ObjectToPool New()
                {
                    return new ObjectToPool();
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public void Reset(ObjectToPool value) { }
            }
        }

        private static readonly NoResetFactorySupport<ObjectToPool> _factory = new NoResetFactorySupport<ObjectToPool>(() => new ObjectToPool());

        private static readonly ObjectPool<ObjectToPool, NoResetSupport<ObjectToPool>> _withoutFactory = new ObjectPool<ObjectToPool, NoResetSupport<ObjectToPool>>();
        private static readonly ObjectPool<ObjectToPool, NoResetFactorySupport<ObjectToPool>> _withFactory = new ObjectPool<ObjectToPool, NoResetFactorySupport<ObjectToPool>>(factory: _factory);
        private static readonly ObjectPool<ObjectToPool, ObjectToPool.Behavior> _withSpecificNew = new ObjectPool<ObjectToPool, ObjectToPool.Behavior>();

        [Benchmark]
        public ObjectToPool UsingFactory()
        {
            return _withFactory.Allocate();
        }

        [Benchmark]
        public ObjectToPool UsingGenericNew()
        {
            return _withoutFactory.Allocate();
        }

        [Benchmark]
        public ObjectToPool UsingSpecificNew()
        {
            return _withSpecificNew.Allocate();
        }
    }

}
