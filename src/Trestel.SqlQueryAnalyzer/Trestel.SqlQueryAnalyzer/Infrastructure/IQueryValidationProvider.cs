// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trestel.SqlQueryAnalyzer.Infrastructure.Models;

namespace Trestel.SqlQueryAnalyzer.Infrastructure
{
    /// <summary>
    /// Provider for raw query string validation. Provider is repsonsible for checking correctness of the query and for returning information
    /// regarding expected parameters and result set.
    /// </summary>
    public interface IQueryValidationProvider
    {
        /// <summary>
        /// Validates the specified raw SQL query.
        /// </summary>
        /// <param name="rawSqlQuery">The raw SQL query.</param>
        /// <returns>Validation result of provided raw query along with expected input parameters and returning result set.</returns>
        ValidationResult Validate(string rawSqlQuery);
    }
}
