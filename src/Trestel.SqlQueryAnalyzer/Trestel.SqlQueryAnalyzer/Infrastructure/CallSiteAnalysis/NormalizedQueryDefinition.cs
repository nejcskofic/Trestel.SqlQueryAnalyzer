// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Trestel.SqlQueryAnalyzer.Infrastructure.CallSiteAnalysis
{
    /// <summary>
    /// Represents normalized query definition, with transformed query (if necessary) and expected parameters.
    /// </summary>
    public sealed class NormalizedQueryDefinition
    {
        private NormalizedQueryDefinition(bool checkParameters, IEnumerable<Parameter> parameters, string normalizedSqlQuery)
        {
            CheckParameters = checkParameters;
            InputParameters = ImmutableArray.CreateRange(parameters);
            NormalizedSqlQuery = normalizedSqlQuery;
        }

        /// <summary>
        /// Gets a value indicating whether to check parameters as part of analysis.
        /// </summary>
        /// <value>
        ///   <c>true</c> if parameters should be checked; otherwise, <c>false</c>.
        /// </value>
        public bool CheckParameters { get; }

        /// <summary>
        /// Gets the input parameters.
        /// </summary>
        /// <value>
        /// The input parameters.
        /// </value>
        public ImmutableArray<Parameter> InputParameters { get; }

        /// <summary>
        /// Gets the normalized SQL query. If query contains syntax which is not valid SQL syntax, provider can transform
        /// this into valid SQL query which will be used in analysis instead of one is source code.
        /// </summary>
        /// <value>
        /// The normalized SQL query.
        /// </value>
        public string NormalizedSqlQuery { get; }

        /// <summary>
        /// Creates builder instance with which you can create <see cref="NormalizedQueryDefinition"/>.
        /// </summary>
        /// <returns>New <see cref="Builder"/> instance.</returns>
        public static Builder New()
        {
            return new Builder();
        }

        /// <summary>
        /// Builder class to create <see cref="NormalizedQueryDefinition"/>.
        /// </summary>
        public sealed class Builder
        {
            private readonly List<Parameter> _parameters;
            private string _normalizedSqlQuery;

            /// <summary>
            /// Initializes a new instance of the <see cref="Builder"/> class.
            /// </summary>
            internal Builder()
            {
                _parameters = new List<Parameter>();
            }

            /// <summary>
            /// Adds parameter.
            /// </summary>
            /// <param name="parameterName">Name of the parameter.</param>
            /// <param name="parameterType">Type of the parameter.</param>
            /// <returns>This instance</returns>
            /// <exception cref="System.ArgumentException">Argument null or empty. - parameterName</exception>
            /// <exception cref="System.ArgumentNullException">parameterType</exception>
            public Builder WithParameter(string parameterName, ITypeSymbol parameterType)
            {
                if (String.IsNullOrEmpty(parameterName)) throw new ArgumentException("Argument null or empty.", nameof(parameterName));
                if (parameterType == null) throw new ArgumentNullException(nameof(parameterType));

                _parameters.Add(new Parameter(parameterName, parameterType));
                return this;
            }

            /// <summary>
            /// Sets the normalized SQL query.
            /// </summary>
            /// <param name="normalizedSqlQuery">The normalized SQL query.</param>
            /// <returns>This instance</returns>
            /// <exception cref="System.ArgumentException">Argument null or empty. - normalizedSqlQuery</exception>
            public Builder WithNormalizedSqlQuery(string normalizedSqlQuery)
            {
                if (String.IsNullOrEmpty(normalizedSqlQuery)) throw new ArgumentException("Argument null or empty.", nameof(normalizedSqlQuery));

                _normalizedSqlQuery = normalizedSqlQuery;
                return this;
            }

            /// <summary>
            /// Creates new <see cref="NormalizedQueryDefinition" /> from information in this class.
            /// </summary>
            /// <param name="checkParameters">if set to <c>true</c> if parameters should be checked.</param>
            /// <returns>
            /// New instance of <see cref="NormalizedQueryDefinition" />
            /// </returns>
            public NormalizedQueryDefinition Build(bool checkParameters)
            {
                return new NormalizedQueryDefinition(
                    checkParameters,
                    _parameters,
                    _normalizedSqlQuery);
            }
        }
    }
}
