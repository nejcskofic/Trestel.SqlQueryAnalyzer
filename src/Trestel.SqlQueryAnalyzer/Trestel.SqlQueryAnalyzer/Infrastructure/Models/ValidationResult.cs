// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Trestel.SqlQueryAnalyzer.Infrastructure.Models
{
    /// <summary>
    /// Represents validation result of raw query analysis.
    /// </summary>
    public sealed class ValidationResult
    {
        private readonly bool _isSuccess;
        private readonly ValidatedQuery _validatedQuery;
        private readonly ImmutableArray<string> _errors;

        private ValidationResult(ValidatedQuery validatedQuery)
        {
            _isSuccess = true;
            _validatedQuery = validatedQuery;
            _errors = ImmutableArray.Create<string>();
        }

        private ValidationResult(IEnumerable<string> errors)
        {
            _isSuccess = false;
            _validatedQuery = null;
            _errors = ImmutableArray.CreateRange(errors);
        }

        /// <summary>
        /// Gets a value indicating whether this instance is success.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is success; otherwise, <c>false</c>.
        /// </value>
        public bool IsSuccess
        {
            get
            {
                return _isSuccess;
            }
        }

        /// <summary>
        /// Gets the validated query.
        /// </summary>
        /// <value>
        /// The validated query.
        /// </value>
        public ValidatedQuery ValidatedQuery
        {
            get
            {
                return _validatedQuery;
            }
        }

        /// <summary>
        /// Gets the errors.
        /// </summary>
        /// <value>
        /// The errors.
        /// </value>
        public ImmutableArray<string> Errors
        {
            get
            {
                return _errors;
            }
        }

        /// <summary>
        /// Creates successful analysis result with required data.
        /// </summary>
        /// <param name="validatedQuery">The validated query.</param>
        /// <returns>Successful analysis result.</returns>
        /// <exception cref="System.ArgumentNullException">validatedQuery</exception>
        public static ValidationResult Success(ValidatedQuery validatedQuery)
        {
            if (validatedQuery == null) throw new ArgumentNullException(nameof(validatedQuery));
            return new ValidationResult(validatedQuery);
        }

        /// <summary>
        /// Creates failed analysis result with specified errors.
        /// </summary>
        /// <param name="errors">The errors.</param>
        /// <returns>Failed analysis result.</returns>
        public static ValidationResult Failure(IEnumerable<string> errors)
        {
            if (errors == null) errors = Enumerable.Empty<string>();
            return new ValidationResult(errors);
        }
    }
}
