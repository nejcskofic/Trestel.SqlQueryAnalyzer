// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using Trestel.SqlQueryAnalyzer.Design;

namespace Trestel.SqlQueryAnalyzer.Models
{
    /// <summary>
    /// Represents connection string data specified in connection string hint.
    /// </summary>
    internal struct ConnectionStringData
    {
        /// <summary>
        /// The empty/nonexistent data.
        /// </summary>
        public static readonly ConnectionStringData Empty = default(ConnectionStringData);

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionStringData"/> struct.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="databaseType">Type of the database.</param>
        /// <exception cref="System.ArgumentException">Argument is null or empty. - connectionString</exception>
        public ConnectionStringData(string connectionString, DatabaseType databaseType)
        {
            if (String.IsNullOrEmpty(connectionString)) throw new ArgumentException("Argument is null or empty.", nameof(connectionString));

            ConnectionString = connectionString;
            DatabaseType = databaseType;
        }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        public string ConnectionString { get; }

        /// <summary>
        /// Gets the type of the database.
        /// </summary>
        /// <value>
        /// The type of the database.
        /// </value>
        public DatabaseType DatabaseType { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is defined.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is defined; otherwise, <c>false</c>.
        /// </value>
        public bool IsDefined
        {
            get { return ConnectionString != null; }
        }
    }
}
