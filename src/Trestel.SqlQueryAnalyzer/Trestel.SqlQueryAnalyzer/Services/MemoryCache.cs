// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Trestel.SqlQueryAnalyzer.Services
{
    /// <summary>
    /// Simple, type safe in-memory cache geared toward short and explicit cache item retention. Is not thread-safe.
    /// </summary>
    /// <typeparam name="T">Type of items stored in cache</typeparam>
    public sealed class MemoryCache<T>
    {
        private readonly IDictionary<string, CacheItem<T>> _cacheStore;
        private readonly Queue<ExpirationCacheKey> _expirationQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryCache{T}"/> class.
        /// </summary>
        public MemoryCache()
        {
            _cacheStore = new Dictionary<string, CacheItem<T>>();
            _expirationQueue = new Queue<ExpirationCacheKey>();
        }

        /// <summary>
        /// Gets the number of items in cache. It is possible that some items have expired and have not been removed yet.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        public int Count
        {
            get
            {
                return _cacheStore.Count;
            }
        }

        /// <summary>
        /// Sets the item in cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <param name="expirationInMilliseconds">The expiration in milliseconds.</param>
        /// <param name="renewOnAccess">if set to <c>true</c> [renew on access].</param>
        public void SetItem(string key, T item, int expirationInMilliseconds, bool renewOnAccess)
        {
            var now = DateTime.UtcNow;
            var cacheItem = new CacheItem<T>(
                item,
                now.AddMilliseconds(expirationInMilliseconds),
                renewOnAccess ? expirationInMilliseconds : 0);
            _expirationQueue.Enqueue(new ExpirationCacheKey(key, cacheItem.ExpirationDate));
            _cacheStore[key] = cacheItem;

            RemoveExpiredInternal(now);
        }

        /// <summary>
        /// Gets the item.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Cached item or default if key is not present in cache.</returns>
        public T GetItem(string key)
        {
            var now = DateTime.UtcNow;
            CacheItem<T> cacheItem = null;
            if (_cacheStore.TryGetValue(key, out cacheItem) && cacheItem.ExpirationDate > now)
            {
                if (cacheItem.SlidingExpirationInMs != 0) RenewExpirationDate(key, now, cacheItem);
                return cacheItem.Item;
            }

            return default(T);
        }

        /// <summary>
        /// Determines whether the specified key is in cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        ///   <c>true</c> if the specified cache key exists; otherwise, <c>false</c>.
        /// </returns>
        public bool HasItem(string key)
        {
            var now = DateTime.UtcNow;
            CacheItem<T> cacheItem = null;
            if (_cacheStore.TryGetValue(key, out cacheItem) && cacheItem.ExpirationDate > now)
            {
                if (cacheItem.SlidingExpirationInMs != 0) RenewExpirationDate(key, now, cacheItem);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes the item.
        /// </summary>
        /// <param name="key">The key.</param>
        public void RemoveItem(string key)
        {
            var now = DateTime.UtcNow;
            _cacheStore.Remove(key);

            RemoveExpiredInternal(now);
        }

        private void RenewExpirationDate(string key, DateTime now, CacheItem<T> item)
        {
            var newExpirationDate = now.AddMilliseconds(item.SlidingExpirationInMs);
            if (newExpirationDate > item.ExpirationDate)
            {
                item.ExpirationDate = newExpirationDate;
                _expirationQueue.Enqueue(new ExpirationCacheKey(key, item.ExpirationDate));
            }
        }

        private void RemoveExpiredInternal(DateTime cutoff)
        {
            while (_expirationQueue.Count != 0)
            {
                var item = _expirationQueue.Peek();
                if (item.ExpirationDate >= cutoff) break;

                _expirationQueue.Dequeue();
                CacheItem<T> cacheItem;
                if (_cacheStore.TryGetValue(item.CacheKey, out cacheItem) &&
                    cacheItem.ExpirationDate == item.ExpirationDate)
                {
                    _cacheStore.Remove(item.CacheKey);
                }
            }
        }

        private struct ExpirationCacheKey
        {
            public ExpirationCacheKey(string key, DateTime expirationDate)
            {
                CacheKey = key;
                ExpirationDate = expirationDate;
            }

            public string CacheKey { get; }

            public DateTime ExpirationDate { get; }
        }

        private class CacheItem<U>
        {
            public CacheItem(U item, DateTime expirationDate, int slidingExpirationInMs)
            {
                Item = item;
                ExpirationDate = expirationDate;
                SlidingExpirationInMs = slidingExpirationInMs;
            }

            public U Item { get; }

            public DateTime ExpirationDate { get; set; }

            public int SlidingExpirationInMs { get; }
        }
    }
}
