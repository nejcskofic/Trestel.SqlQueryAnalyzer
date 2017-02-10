// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Trestel.SqlQueryAnalyzer.Common;
using Trestel.SqlQueryAnalyzer.Infrastructure;
using Trestel.SqlQueryAnalyzer.Infrastructure.QueryAnalysis;

namespace Trestel.SqlQueryAnalyzer.Services
{
    /// <summary>
    /// Responsible for caching results and processing information.
    /// </summary>
    public sealed class CachingService
    {
        private readonly MemoryCache<Result<ValidatedQuery>> _validationResultCache;
        private readonly MemoryCache<bool> _documentLocationCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="CachingService"/> class.
        /// </summary>
        public CachingService()
        {
            _validationResultCache = new MemoryCache<Result<ValidatedQuery>>();
            _documentLocationCache = new MemoryCache<bool>();
        }

        /// <summary>
        /// Gets or adds validation result.
        /// </summary>
        /// <param name="connectionData">The connection data.</param>
        /// <param name="rawQuery">The raw query.</param>
        /// <param name="analyzeParameterInfo">if set to <c>true</c> [analyze parameter information].</param>
        /// <param name="function">The function.</param>
        /// <returns>
        /// Cached validation result or one constructed from function.
        /// </returns>
        public Result<ValidatedQuery> GetOrAddValidationResult(ConnectionStringData connectionData, string rawQuery, bool analyzeParameterInfo, Func<ConnectionStringData, string, bool, Result<ValidatedQuery>> function)
        {
            var key = ConstructKey(connectionData, rawQuery, analyzeParameterInfo);

            lock (_validationResultCache)
            {
                var result = _validationResultCache.GetItem(key);
                if (result != Result<ValidatedQuery>.Empty) return result;
            }

            var newResult = function(connectionData, rawQuery, analyzeParameterInfo);
            if (newResult != Result<ValidatedQuery>.Empty)
            {
                lock (_validationResultCache)
                {
                    _validationResultCache.SetItem(key, newResult, 10000, true);
                }
            }

            return newResult;
        }

        /// <summary>
        /// Gets or adds validation result.
        /// </summary>
        /// <param name="connectionData">The connection data.</param>
        /// <param name="rawQuery">The raw query.</param>
        /// <param name="analyzeParameterInfo">if set to <c>true</c> [analyze parameter information].</param>
        /// <param name="function">The function.</param>
        /// <returns>
        /// Cached validation result or one constructed from function.
        /// </returns>
        public async Task<Result<ValidatedQuery>> GetOrAddValidationResultAsync(ConnectionStringData connectionData, string rawQuery, bool analyzeParameterInfo, Func<ConnectionStringData, string, bool, Task<Result<ValidatedQuery>>> function)
        {
            var key = ConstructKey(connectionData, rawQuery, analyzeParameterInfo);

            lock (_validationResultCache)
            {
                var result = _validationResultCache.GetItem(key);
                if (result != Result<ValidatedQuery>.Empty) return result;
            }

            var newResult = await function(connectionData, rawQuery, analyzeParameterInfo);
            if (newResult != Result<ValidatedQuery>.Empty)
            {
                lock (_validationResultCache)
                {
                    _validationResultCache.SetItem(key, newResult, 10000, true);
                }
            }

            return newResult;
        }

        /// <summary>
        /// Determines whether cache contains document location.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>
        ///   <c>true</c> if document location was cached; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsOrAddDocumentLocation(SyntaxNode node)
        {
            if (node == null) return false;

            var key = ConstructKey(node);

            lock (_documentLocationCache)
            {
                var exists = _documentLocationCache.HasItem(key);
                if (!exists) _documentLocationCache.SetItem(key, true, 2000, true);
                return exists;
            }
        }

        private static string ConstructKey(ConnectionStringData connectionData, string rawQuery, bool analyzeParameterInfo)
        {
            int hash = connectionData.GetHashCode();
            return hash.ToString("X") + ":" + (rawQuery ?? "") + (analyzeParameterInfo ? ":1" : ":0");
        }

        private static string ConstructKey(SyntaxNode node)
        {
            int start = node.GetLocation().SourceSpan.Start;
            return start.ToString("X") + ":" + (node.SyntaxTree.FilePath ?? "");
        }
    }
}
