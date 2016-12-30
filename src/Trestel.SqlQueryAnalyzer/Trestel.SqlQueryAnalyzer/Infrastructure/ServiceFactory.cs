// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using Trestel.SqlQueryAnalyzer.Design;

namespace Trestel.SqlQueryAnalyzer.Infrastructure
{
    /// <summary>
    /// Contains factory for creating/returning services for consumption by analyser.
    /// </summary>
    public class ServiceFactory
    {
        private readonly Func<string, IQueryValidationProvider>[] _registeredFactories;
        private ValidationProviderCacheMode _validationProviderCacheMode;

        private QueryValidationProviderCacheKey _cachedKey;
        private IQueryValidationProvider _cachedProvider;
        private IDictionary<QueryValidationProviderCacheKey, IQueryValidationProvider> _queryValidationProviderCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceFactory"/> class.
        /// </summary>
        public ServiceFactory()
        {
            _registeredFactories = new Func<string, IQueryValidationProvider>[Enum.GetValues(typeof(DatabaseType)).Length];
            _validationProviderCacheMode = ValidationProviderCacheMode.None;
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
        /// <param name="connectionString">The connection string.</param>
        /// <param name="type">The type.</param>
        /// <returns>Matching query validation provider.</returns>
        /// <exception cref="System.ArgumentException">Argument is null or empty. - connectionString</exception>
        public IQueryValidationProvider GetQueryValidationProvider(string connectionString, DatabaseType type)
        {
            if (String.IsNullOrEmpty(connectionString)) throw new ArgumentException("Argument is null or empty.", nameof(connectionString));

            switch (_validationProviderCacheMode)
            {
                case ValidationProviderCacheMode.None:
                    {
                        var factory = _registeredFactories[(int)type];
                        if (factory == null) return null;
                        _cachedProvider = factory(connectionString);
                        _cachedKey = new QueryValidationProviderCacheKey(connectionString, type);
                        _validationProviderCacheMode = ValidationProviderCacheMode.Single;
                        return _cachedProvider;
                    }

                case ValidationProviderCacheMode.Single:
                    {
                        if (_cachedKey.ConnectionString == connectionString && _cachedKey.DatabaseType == type)
                        {
                            return _cachedProvider;
                        }

                        var factory = _registeredFactories[(int)type];
                        if (factory == null) return null;

                        // add existing to dictionary
                        if (_queryValidationProviderCache == null) _queryValidationProviderCache = new Dictionary<QueryValidationProviderCacheKey, IQueryValidationProvider>();
                        _queryValidationProviderCache.Add(_cachedKey, _cachedProvider);
                        var provider = factory(connectionString);
                        _queryValidationProviderCache.Add(new QueryValidationProviderCacheKey(connectionString, type), provider);
                        _validationProviderCacheMode = ValidationProviderCacheMode.Multiple;
                        return provider;
                    }

                case ValidationProviderCacheMode.Multiple:
                    {
                        IQueryValidationProvider provider;
                        var key = new QueryValidationProviderCacheKey(connectionString, type);
                        if (!_queryValidationProviderCache.TryGetValue(key, out provider))
                        {
                            var factory = _registeredFactories[(int)type];
                            if (factory == null) return null;

                            provider = factory(connectionString);
                            _queryValidationProviderCache.Add(key, provider);
                        }

                        return provider;
                    }

                default:
                    return null;
            }
        }

        private struct QueryValidationProviderCacheKey : IEquatable<QueryValidationProviderCacheKey>
        {
            public QueryValidationProviderCacheKey(string connectionString, DatabaseType databaseType)
            {
                ConnectionString = connectionString;
                DatabaseType = databaseType;
            }

            public string ConnectionString { get; }

            public DatabaseType DatabaseType { get; }

            public override bool Equals(object obj)
            {
                return obj is QueryValidationProviderCacheKey ? Equals((QueryValidationProviderCacheKey)obj) : false;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + (ConnectionString != null ? ConnectionString.GetHashCode() : 0);
                    hash = hash * 31 + (int)DatabaseType;
                    return hash;
                }
            }

            public bool Equals(QueryValidationProviderCacheKey other)
            {
                return ConnectionString == other.ConnectionString &&
                    DatabaseType == other.DatabaseType;
            }
        }

#pragma warning disable SA1201 // Elements must appear in the correct order
        private enum ValidationProviderCacheMode
#pragma warning restore SA1201 // Elements must appear in the correct order
        {
            None,
            Single,
            Multiple
        }
    }
}
