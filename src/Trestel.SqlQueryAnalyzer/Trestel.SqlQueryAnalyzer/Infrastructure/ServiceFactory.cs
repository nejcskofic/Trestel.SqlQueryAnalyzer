// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Trestel.SqlQueryAnalyzer.Design;
using Trestel.SqlQueryAnalyzer.Infrastructure.Models;

namespace Trestel.SqlQueryAnalyzer.Infrastructure
{
    /// <summary>
    /// Contains factory for creating/returning services for consumption by analyser.
    /// </summary>
    public class ServiceFactory
    {
        private readonly Func<string, IQueryValidationProvider>[] _registeredFactories;
        private readonly IDictionary<ConnectionStringData, IQueryValidationProvider> _queryValidationProviderCache;

        private volatile CachedEntry _cachedEntry;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceFactory"/> class.
        /// </summary>
        public ServiceFactory()
        {
            _registeredFactories = new Func<string, IQueryValidationProvider>[Enum.GetValues(typeof(DatabaseType)).Length];
            _queryValidationProviderCache = new ConcurrentDictionary<ConnectionStringData, IQueryValidationProvider>();
        }

        /// <summary>
        /// Registers the query validation provider factory.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="factory">The factory.</param>
        /// <returns>This instance</returns>
        /// <exception cref="System.ArgumentNullException">factory</exception>
        public ServiceFactory RegisterQueryValidationProviderFactory(DatabaseType type, Func<string, IQueryValidationProvider> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            _registeredFactories[(int)type] = factory;
            return this;
        }

        /// <summary>
        /// Gets the query validation provider.
        /// </summary>
        /// <param name="connectionData">The connection data.</param>
        /// <returns>
        /// Matching query validation provider.
        /// </returns>
        /// <exception cref="System.ArgumentException">Argument is null or empty. - connectionString</exception>
        public IQueryValidationProvider GetQueryValidationProvider(ConnectionStringData connectionData)
        {
            if (!connectionData.IsDefined) return null;

            var cachedEntry = _cachedEntry;
            if (cachedEntry != null && cachedEntry.ConnectionData == connectionData)
            {
                return cachedEntry.Provider;
            }

            IQueryValidationProvider provider;
            if (!_queryValidationProviderCache.TryGetValue(connectionData, out provider))
            {
                var factory = _registeredFactories[(int)connectionData.DatabaseType];
                if (factory == null) return null;

                provider = factory(connectionData.ConnectionString);
                _queryValidationProviderCache.Add(connectionData, provider);
                cachedEntry = new CachedEntry(connectionData, provider);
                Interlocked.Exchange(ref _cachedEntry, cachedEntry);
            }

            return provider;
        }

        private sealed class CachedEntry
        {
            public CachedEntry(ConnectionStringData connectionData, IQueryValidationProvider provider)
            {
                ConnectionData = connectionData;
                Provider = provider;
            }

            public ConnectionStringData ConnectionData { get; }

            public IQueryValidationProvider Provider { get; }
        }
    }
}
