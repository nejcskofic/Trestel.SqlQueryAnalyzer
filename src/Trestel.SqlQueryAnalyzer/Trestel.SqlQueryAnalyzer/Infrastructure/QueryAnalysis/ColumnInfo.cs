// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Trestel.SqlQueryAnalyzer.Infrastructure.QueryAnalysis
{
    /// <summary>
    /// Immutable class containing column information such as name, type and ordinal position.
    /// </summary>
    public sealed class ColumnInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnInfo"/> class.
        /// </summary>
        /// <param name="ordinal">The ordinal.</param>
        /// <param name="name">The name.</param>
        /// <param name="type">The type.</param>
        public ColumnInfo(int ordinal, string name, Type type)
        {
            Ordinal = ordinal;
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Gets the ordinal position.
        /// </summary>
        /// <value>
        /// The ordinal.
        /// </value>
        public int Ordinal
        {
            get;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name
        {
            get;
        }

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public Type Type
        {
            get;
        }
    }
}
