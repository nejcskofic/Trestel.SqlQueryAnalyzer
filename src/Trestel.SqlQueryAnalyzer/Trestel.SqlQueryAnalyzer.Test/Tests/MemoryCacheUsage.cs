// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System.Threading;
using NUnit.Framework;
using Trestel.SqlQueryAnalyzer.Services;

namespace Tests.Tests
{
    [TestFixture]
    public class MemoryCacheUsage
    {
        [Test]
        public void EmptyCache()
        {
            var cache = new MemoryCache<object>();

            Assert.Null(cache.GetItem("key"));
        }

        [Test]
        public void CachedRetrieval()
        {
            var cache = new MemoryCache<object>();
            var value1 = new object();
            var value2 = new object();
            var value3 = new object();
            cache.SetItem("key1", value1, 1000, false);
            cache.SetItem("key2", value2, 1000, false);
            cache.SetItem("key3", value3, 1000, false);

            Assert.AreSame(value1, cache.GetItem("key1"));
            Assert.AreSame(value2, cache.GetItem("key2"));
            Assert.AreSame(value3, cache.GetItem("key3"));
        }

        [Test]
        public void RetrieveExpiredItem()
        {
            var cache = new MemoryCache<object>();
            var value = new object();
            cache.SetItem("key", value, 1000, false);

            Assert.NotNull(cache.GetItem("key"));
            Thread.Sleep(1200);
            Assert.Null(cache.GetItem("key"));
        }

        [Test]
        public void RetrieveItemWithExpirationRenewal()
        {
            var cache = new MemoryCache<object>();
            var value = new object();
            cache.SetItem("key", value, 1000, true);

            Assert.NotNull(cache.GetItem("key"));
            Thread.Sleep(600);
            Assert.NotNull(cache.GetItem("key"));
            Thread.Sleep(600);
            Assert.NotNull(cache.GetItem("key"));
            Thread.Sleep(600);
            Assert.NotNull(cache.GetItem("key"));
            Thread.Sleep(1200);
            Assert.Null(cache.GetItem("key"));
        }

        [Test]
        public void InternalStoreCleanup()
        {
            var cache = new MemoryCache<object>();
            var value = new object();
            cache.SetItem("key1", value, 1000, false);
            cache.SetItem("key2", value, 3000, false);

            Thread.Sleep(1200);
            Assert.AreEqual(2, cache.Count);

            cache.SetItem("key3", value, 1000, false);
            Assert.AreEqual(2, cache.Count);
        }

        [Test]
        public void RemoveNonExistentItem()
        {
            var cache = new MemoryCache<object>();

            cache.RemoveItem("key");
        }

        [Test]
        public void RemoveItem()
        {
            var cache = new MemoryCache<object>();
            cache.SetItem("key", new object(), 1000, false);

            cache.RemoveItem("key");

            Assert.IsNull(cache.GetItem("key"));
        }

        public void ContainsItem()
        {
            var cache = new MemoryCache<object>();

            Assert.IsFalse(cache.HasItem("key"));
            cache.SetItem("key", new object(), 1000, false);
            Assert.IsTrue(cache.HasItem("key"));
            Thread.Sleep(1200);
            Assert.IsFalse(cache.HasItem("key"));
        }
    }
}
