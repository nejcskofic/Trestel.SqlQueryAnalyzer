// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Trestel.SqlQueryAnalyzer.Common;
using Trestel.SqlQueryAnalyzer.Infrastructure.QueryAnalysis;

namespace Trestel.SqlQueryAnalyzer.Infrastructure
{
    /// <summary>
    /// Provider for raw query string validation. Provider is repsonsible for checking correctness of the query and for returning information
    /// regarding expected parameters and result set.
    /// </summary>
    public interface IQueryValidationProvider
    {
        /// <summary>
        /// Gets a value indicating whether throttling should be enabled. If static validation provider performs any lengthy operation
        /// (such as direct query to database), you should enable throttling which will delay call to <see cref="ValidateAsync(string, bool, CancellationToken)"/>.
        /// While user is typing, query analyzer tasks may get cancelled before expensive calls will be performed in
        /// <see cref="ValidateAsync(string, bool, CancellationToken)"/>.
        /// </summary>
        /// <value>
        ///   <c>true</c> if throttling is enabeld; otherwise, <c>false</c>.
        /// </value>
        bool EnableThrottling { get; }

        /// <summary>
        /// Validates the specified raw SQL query.
        /// </summary>
        /// <param name="rawSqlQuery">The raw SQL query.</param>
        /// <param name="analyzeParameterInfo">if set to <c>true</c> parameter info should be supplied.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>
        /// Validation result of provided raw query along with expected input parameters and returning result set.
        /// </returns>
        Task<Result<ValidatedQuery>> ValidateAsync(string rawSqlQuery, bool analyzeParameterInfo, CancellationToken cancellationToken);
    }
}
