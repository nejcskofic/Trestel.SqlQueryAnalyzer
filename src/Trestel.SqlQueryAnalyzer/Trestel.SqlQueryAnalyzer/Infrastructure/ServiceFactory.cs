// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Trestel.SqlQueryAnalyzer.Design;
using Trestel.SqlQueryAnalyzer.Infrastructure.CallSiteAnalysis;

namespace Trestel.SqlQueryAnalyzer.Infrastructure
{
    /// <summary>
    /// Contains factory for creating/returning services for consumption by analyser.
    /// </summary>
    public class ServiceFactory
    {
        private readonly ICallSiteAnalyzer[] _callSiteAnalyzers;
        private readonly Func<string, IQueryValidationProvider>[] _registeredFactories;
        private readonly IDictionary<ConnectionStringData, IQueryValidationProvider> _queryValidationProviderCache;

        private volatile CachedEntry _cachedEntry;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceFactory" /> class.
        /// </summary>
        /// <param name="callSiteAnalyzers">The call site analyzer.</param>
        /// <param name="validationFactories">The validation factories.</param>
        private ServiceFactory(ICallSiteAnalyzer[] callSiteAnalyzers, Func<string, IQueryValidationProvider>[] validationFactories)
        {
            _callSiteAnalyzers = callSiteAnalyzers;
            _registeredFactories = validationFactories;
            _queryValidationProviderCache = new ConcurrentDictionary<ConnectionStringData, IQueryValidationProvider>();
        }

        /// <summary>
        /// Gets the call site analyzer.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>Call site analyzer instance of null if no matching analyzer was found.</returns>
        public ICallSiteAnalyzer GetCallSiteAnalyzer(CallSiteContext context)
        {
            if (context == null) return null;

            for (int i = 0; i < _callSiteAnalyzers.Length; i++)
            {
                var analyzer = _callSiteAnalyzers[i];
                if (analyzer.CanAnalyzeCallSite(context)) return analyzer;
            }

            return null;
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

        /// <summary>
        /// Creates new instance of <see cref="Builder"/> which is used to build <see cref="ServiceFactory"/>.
        /// </summary>
        /// <returns>New instance of <see cref="Builder"/></returns>
        public static Builder New()
        {
            return new Builder();
        }

        /// <summary>
        /// Builder class for creating new <see cref="ServiceFactory"/>.
        /// </summary>
        public sealed class Builder
        {
            private readonly Func<string, IQueryValidationProvider>[] _registeredFactories;
            private readonly List<ICallSiteAnalyzer> _callSiteAnalyzers;

            /// <summary>
            /// Initializes a new instance of the <see cref="Builder"/> class.
            /// </summary>
            internal Builder()
            {
                _registeredFactories = new Func<string, IQueryValidationProvider>[Enum.GetValues(typeof(DatabaseType)).Length];
                _callSiteAnalyzers = new List<ICallSiteAnalyzer>();
            }

            /// <summary>
            /// Registers the query validation provider factory.
            /// </summary>
            /// <param name="type">The type.</param>
            /// <param name="factory">The factory.</param>
            /// <returns>This instance</returns>
            /// <exception cref="System.ArgumentNullException">factory</exception>
            public Builder RegisterQueryValidationProviderFactory(DatabaseType type, Func<string, IQueryValidationProvider> factory)
            {
                if (factory == null) throw new ArgumentNullException(nameof(factory));

                _registeredFactories[(int)type] = factory;
                return this;
            }

            /// <summary>
            /// Registers the call site analyzer instance. Order of calls is important since checks will be performed sequentially.
            /// Because of this, default/fallback instance should be registered last.
            /// </summary>
            /// <param name="callSiteAnalyzer">The call site analyzer.</param>
            /// <returns>This instance</returns>
            /// <exception cref="System.ArgumentNullException">callSiteAnalyzer</exception>
            public Builder RegisterCallSiteAnalyzerInstance(ICallSiteAnalyzer callSiteAnalyzer)
            {
                if (callSiteAnalyzer == null) throw new ArgumentNullException(nameof(callSiteAnalyzer));

                _callSiteAnalyzers.Add(callSiteAnalyzer);
                return this;
            }

            /// <summary>
            /// Creates new <see cref="ServiceFactory"/> instance.
            /// </summary>
            /// <returns>New <see cref="ServiceFactory"/> instance.</returns>
            public ServiceFactory Build()
            {
                return new ServiceFactory(
                    _callSiteAnalyzers.ToArray(),
                    (Func<string, IQueryValidationProvider>[])_registeredFactories.Clone());
            }
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
