// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Trestel.SqlQueryAnalyzer.Infrastructure.CallSiteAnalysis
{
    /// <summary>
    /// Represents normalized call site information, such as what parameters are passed in and which fields are expected to be returned.
    /// </summary>
    public sealed class NormalizedCallSite
    {
        private NormalizedCallSite(bool checkParameters, bool checkResult, IEnumerable<Parameter> parameters, IEnumerable<ResultField> fields, string normalizedSqlQuery)
        {
            CheckParameters = checkParameters;
            CheckResult = checkResult;
            InputParameters = ImmutableArray.CreateRange(parameters);
            ExpectedFields = ImmutableArray.CreateRange(fields);
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
        /// Gets a value indicating whether to check result as part of analysis.
        /// </summary>
        /// <value>
        ///   <c>true</c> if result should be checked; otherwise, <c>false</c>.
        /// </value>
        public bool CheckResult { get; }

        /// <summary>
        /// Gets the input parameters.
        /// </summary>
        /// <value>
        /// The input parameters.
        /// </value>
        public ImmutableArray<Parameter> InputParameters { get; }

        /// <summary>
        /// Gets the expected fields.
        /// </summary>
        /// <value>
        /// The expected fields.
        /// </value>
        public ImmutableArray<ResultField> ExpectedFields { get; }

        /// <summary>
        /// Gets the normalized SQL query. If query contains syntax which is not valid SQL syntax, provider can transform
        /// this into valid SQL query which will be used in analysis instead of one is source code.
        /// </summary>
        /// <value>
        /// The normalized SQL query.
        /// </value>
        public string NormalizedSqlQuery { get; }

        /// <summary>
        /// Creates builder instance with which you can create <see cref="NormalizedCallSite"/>.
        /// </summary>
        /// <returns>New <see cref="Builder"/> instance.</returns>
        public static Builder New()
        {
            return new Builder();
        }

        /// <summary>
        /// Builder class to create <see cref="NormalizedCallSite"/>.
        /// </summary>
        public sealed class Builder
        {
            private readonly List<Parameter> _parameters;
            private readonly List<ResultField> _fields;
            private bool _isSingleAnonymousFieldExpected;
            private string _normalizedSqlQuery;

            /// <summary>
            /// Initializes a new instance of the <see cref="Builder"/> class.
            /// </summary>
            internal Builder()
            {
                _parameters = new List<Parameter>();
                _fields = new List<ResultField>();
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
            /// Adds the expected field.
            /// </summary>
            /// <param name="fieldName">Name of the field.</param>
            /// <param name="fieldType">Type of the field.</param>
            /// <param name="containingType">Type of the containing.</param>
            /// <returns>This instance</returns>
            /// <exception cref="System.ArgumentException">Argument null or empty. - fieldName</exception>
            /// <exception cref="System.ArgumentNullException">
            /// fieldType
            /// or
            /// containingType
            /// </exception>
            /// <exception cref="System.InvalidOperationException">There is already single anonymous field defined.</exception>
            public Builder WithExpectedField(string fieldName, ITypeSymbol fieldType, ITypeSymbol containingType)
            {
                if (String.IsNullOrEmpty(fieldName)) throw new ArgumentException("Argument null or empty.", nameof(fieldName));
                if (fieldType == null) throw new ArgumentNullException(nameof(fieldType));
                if (containingType == null) throw new ArgumentNullException(nameof(containingType));
                if (_isSingleAnonymousFieldExpected) throw new InvalidOperationException("There is already single anonymous field defined.");

                _fields.Add(new ResultField(fieldName, fieldType, containingType));
                return this;
            }

            /// <summary>
            /// Adds the single anonymous expected field.
            /// </summary>
            /// <param name="fieldType">Type of the field.</param>
            /// <returns>This instance</returns>
            /// <exception cref="System.ArgumentNullException">fieldType</exception>
            /// <exception cref="System.InvalidOperationException">
            /// There is already single anonymous field defined.
            /// or
            /// There is already another non-anonymous field defined.
            /// </exception>
            public Builder WithSingleAnonymousExpectedField(ITypeSymbol fieldType)
            {
                if (fieldType == null) throw new ArgumentNullException(nameof(fieldType));
                if (_isSingleAnonymousFieldExpected) throw new InvalidOperationException("There is already single anonymous field defined.");
                if (_fields.Count > 0) throw new InvalidOperationException("There is already another non-anonymous field defined.");

                _fields.Add(new ResultField(null, fieldType, null));
                _isSingleAnonymousFieldExpected = true;
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
            /// Creates new <see cref="NormalizedCallSite" /> from information in this class.
            /// </summary>
            /// <param name="checkParameters">if set to <c>true</c> if parameters should be checked.</param>
            /// <param name="checkResult">if set to <c>true</c> if result should be checked.</param>
            /// <returns>
            /// New instance of <see cref="NormalizedCallSite" />
            /// </returns>
            public NormalizedCallSite Build(bool checkParameters, bool checkResult)
            {
                return new NormalizedCallSite(
                    checkParameters,
                    checkResult,
                    _parameters,
                    _fields,
                    _normalizedSqlQuery);
            }
        }
    }
}
