// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Trestel.SqlQueryAnalyzer.Infrastructure;
using Trestel.SqlQueryAnalyzer.Infrastructure.Models;

namespace TestHelper
{
    public class MockupValidationProvider : IQueryValidationProvider
    {
        private readonly List<string> _connectionStrings;
        private readonly Dictionary<string, ValidationResult> _mockedUpResults;
        private readonly List<string> _accessedQueries;

        public MockupValidationProvider()
        {
            _connectionStrings = new List<string>();
            _mockedUpResults = new Dictionary<string, ValidationResult>();
            _accessedQueries = new List<string>();
        }

        public System.Collections.Immutable.ImmutableList<string> AccessedQueries
        {
            get
            {
                return System.Collections.Immutable.ImmutableList.CreateRange(_accessedQueries);
            }
        }

        public System.Collections.Immutable.ImmutableList<string> ConnectionStrings
        {
            get
            {
                return System.Collections.Immutable.ImmutableList.CreateRange(_connectionStrings);
            }
        }

        public bool EnableThrottling
        {
            get
            {
                return false;
            }
        }

        public MockupValidationProvider WithConnectionString(string connectionString)
        {
            _connectionStrings.Add(connectionString);
            return this;
        }

        public MockupValidationProvider AddExpectedResult(string rawQuery, ValidationResult result)
        {
            if (rawQuery == null) throw new ArgumentNullException(nameof(rawQuery));
            if (result == null) throw new ArgumentNullException(nameof(result));

            _mockedUpResults[rawQuery] = result;
            return this;
        }

        public Task<ValidationResult> ValidateAsync(string rawSqlQuery, CancellationToken cancellationToken)
        {
            if (rawSqlQuery == null) throw new ArgumentNullException(nameof(rawSqlQuery));
            _accessedQueries.Add(rawSqlQuery);

            ValidationResult result = null;
            if (!_mockedUpResults.TryGetValue(rawSqlQuery, out result))
            {
                throw new ArgumentException("There is no configured result for following raw query: " + rawSqlQuery);
            }

            return Task.FromResult(result);
        }
    }
}
