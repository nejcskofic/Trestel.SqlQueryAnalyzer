// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Trestel.SqlQueryAnalyzer.Infrastructure.QueryAnalysis
{
    /// <summary>
    /// Contains data for validated query (such as output columns).
    /// </summary>
    public sealed class ValidatedQuery
    {
        private ValidatedQuery(IEnumerable<ParameterInfo> parameters, IEnumerable<ColumnInfo> outputColumns)
        {
            Parameters = ImmutableArray.CreateRange(parameters);
            OutputColumns = ImmutableArray.CreateRange(outputColumns);
        }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        /// <value>
        /// The parameters.
        /// </value>
        public ImmutableArray<ParameterInfo> Parameters { get; }

        /// <summary>
        /// Gets the output columns.
        /// </summary>
        /// <value>
        /// The output columns.
        /// </value>
        public ImmutableArray<ColumnInfo> OutputColumns { get; }

        /// <summary>
        /// Creates new <see cref="Builder"/> instance to build this object.
        /// </summary>
        /// <returns>New <see cref="Builder"/> instance.</returns>
        public static Builder New()
        {
            return new Builder();
        }

        /// <summary>
        /// Builder element to build <see cref="ValidatedQuery"/> instance.
        /// </summary>
        public sealed class Builder
        {
            private readonly List<ParameterInfo> _parameters;
            private readonly List<ColumnInfo> _outputColumns;

            /// <summary>
            /// Initializes a new instance of the <see cref="Builder"/> class.
            /// </summary>
            internal Builder()
            {
                _parameters = new List<ParameterInfo>();
                _outputColumns = new List<ColumnInfo>();
            }

            /// <summary>
            /// Adds the parameter.
            /// </summary>
            /// <param name="name">The name.</param>
            /// <param name="type">The type.</param>
            /// <returns>This instance</returns>
            /// <exception cref="System.ArgumentException">Argument null or empty - name</exception>
            /// <exception cref="System.ArgumentNullException">type</exception>
            public Builder AddParameter(string name, Type type)
            {
                if (String.IsNullOrEmpty(name)) throw new ArgumentException("Argument null or empty", nameof(name));
                if (type == null) throw new ArgumentNullException(nameof(type));

                _parameters.Add(new ParameterInfo(_parameters.Count, name, type));
                return this;
            }

            /// <summary>
            /// Adds the output column.
            /// </summary>
            /// <param name="name">The name.</param>
            /// <param name="type">The type.</param>
            /// <returns>This instance</returns>
            /// <exception cref="System.ArgumentException">Name cannot be null or empty - name</exception>
            /// <exception cref="System.ArgumentNullException">type</exception>
            public Builder AddOutputColumn(string name, Type type)
            {
                if (String.IsNullOrEmpty(name)) throw new ArgumentException("Argument null or empty", nameof(name));
                if (type == null) throw new ArgumentNullException(nameof(type));

                _outputColumns.Add(new ColumnInfo(_outputColumns.Count, name, type));
                return this;
            }

            /// <summary>
            /// Builds this instance.
            /// </summary>
            /// <returns>Immutable <see cref="ValidatedQuery"/></returns>
            public ValidatedQuery Build()
            {
                return new ValidatedQuery(_parameters, _outputColumns);
            }
        }
    }
}
