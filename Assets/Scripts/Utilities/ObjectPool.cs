using System;
using System.Collections.Generic;
using UnityEngine;

namespace SignalNoise.Utilities
{
    /// <summary>
    /// Generic object pool for frequent instantiations (e.g., visual FX, UI elements).
    /// Usage:
    ///   var pool = new ObjectPool&lt;GameObject&gt;(() => Instantiate(prefab), obj => obj.SetActive(false));
    ///   GameObject obj = pool.Get();
    ///   pool.Return(obj);
    /// </summary>
    public class ObjectPool<T> where T : class
    {
        private readonly Stack<T>    _pool;
        private readonly Func<T>     _factory;
        private readonly Action<T>   _onGet;
        private readonly Action<T>   _onReturn;
        private readonly int         _maxSize;

        /// <summary>
        /// Current number of objects available in the pool.
        /// </summary>
        public int CountInactive => _pool.Count;

        /// <param name="factory">Creates a new instance when the pool is empty.</param>
        /// <param name="onReturn">Called when an object is returned to the pool (e.g., deactivate).</param>
        /// <param name="onGet">Called when an object is retrieved from the pool (e.g., activate). Optional.</param>
        /// <param name="initialSize">Pre-warms the pool with this many instances.</param>
        /// <param name="maxSize">Pool will not hold more than this many inactive objects.</param>
        public ObjectPool(
            Func<T>   factory,
            Action<T> onReturn  = null,
            Action<T> onGet     = null,
            int       initialSize = 0,
            int       maxSize     = 100)
        {
            _factory  = factory  ?? throw new ArgumentNullException(nameof(factory));
            _onReturn = onReturn;
            _onGet    = onGet;
            _maxSize  = maxSize;
            _pool     = new Stack<T>(Mathf.Max(initialSize, 4));

            for (int i = 0; i < initialSize; i++)
            {
                T obj = _factory();
                _onReturn?.Invoke(obj);
                _pool.Push(obj);
            }
        }

        /// <summary>Retrieves an object from the pool, creating one if the pool is empty.</summary>
        public T Get()
        {
            T obj = _pool.Count > 0 ? _pool.Pop() : _factory();
            _onGet?.Invoke(obj);
            return obj;
        }

        /// <summary>Returns an object to the pool. Excess objects beyond maxSize are discarded.</summary>
        public void Return(T obj)
        {
            if (obj == null) return;

            if (_pool.Count < _maxSize)
            {
                _onReturn?.Invoke(obj);
                _pool.Push(obj);
            }
            // If beyond max size, let GC collect it (or destroy if MonoBehaviour)
        }

        /// <summary>Clears all pooled objects without running any cleanup callbacks.</summary>
        public void Clear()
        {
            _pool.Clear();
        }
    }
}
