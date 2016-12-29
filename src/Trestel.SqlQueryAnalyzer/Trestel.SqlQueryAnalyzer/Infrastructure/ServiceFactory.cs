using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trestel.SqlQueryAnalyzer.Design;

namespace Trestel.SqlQueryAnalyzer.Infrastructure
{
    public class ServiceFactory
    {
        private readonly Func<string, IQueryValidationProvider>[] _registeredFactories;
        private ValidationProviderCacheMode _validationProviderCacheMode;

        private QueryValidationProviderCacheKey _cachedKey;
        private IQueryValidationProvider _cachedProvider;
        private IDictionary<QueryValidationProviderCacheKey, IQueryValidationProvider> _queryValidationProviderCache;

        public ServiceFactory()
        {
            _registeredFactories = new Func<string, IQueryValidationProvider>[Enum.GetValues(typeof(DatabaseType)).Length];
            _validationProviderCacheMode = ValidationProviderCacheMode.None;
        }

        public ServiceFactory RegisterQueryValidationProviderFactory(DatabaseType type, Func<string, IQueryValidationProvider> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            _registeredFactories[(int)type] = factory;
            return this;
        }

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
            public string ConnectionString { get; }

            public DatabaseType DatabaseType { get; }

            public QueryValidationProviderCacheKey(string connectionString, DatabaseType databaseType )
            {
                ConnectionString = connectionString;
                DatabaseType = databaseType;
            }

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

        private enum ValidationProviderCacheMode
        {
            None,
            Single,
            Multiple
        }
    }
}
