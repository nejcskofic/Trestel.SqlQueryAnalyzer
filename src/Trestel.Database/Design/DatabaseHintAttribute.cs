// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trestel.Database.Design
{
    /// <summary>
    /// Development time attribute to specify connection string used for static analysis of SQL queries.
    /// </summary>
    /// <seealso cref="System.Attribute" />
    [Conditional("DatabaseHintAttribute")]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public sealed class DatabaseHintAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseHintAttribute"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public DatabaseHintAttribute(string connectionString)
            : this(connectionString, DatabaseType.SqlServer)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseHintAttribute"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="databaseType">Type of the database.</param>
        public DatabaseHintAttribute(string connectionString, DatabaseType databaseType)
        {
            ConnectionString = connectionString;
            DatabaseType = databaseType;
        }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        public string ConnectionString
        {
            get;
        }

        /// <summary>
        /// Gets the type of the database.
        /// </summary>
        /// <value>
        /// The type of the database.
        /// </value>
        public DatabaseType DatabaseType
        {
            get;
        }
    }
}
