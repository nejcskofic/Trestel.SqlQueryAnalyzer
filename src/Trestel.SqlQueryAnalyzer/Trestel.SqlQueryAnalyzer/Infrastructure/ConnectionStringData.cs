// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using Trestel.SqlQueryAnalyzer.Design;

namespace Trestel.SqlQueryAnalyzer.Infrastructure
{
    /// <summary>
    /// Represents connection string data specified in connection string hint.
    /// </summary>
    public struct ConnectionStringData : IEquatable<ConnectionStringData>
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

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="obj1">The obj1.</param>
        /// <param name="obj2">The obj2.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(ConnectionStringData obj1, ConnectionStringData obj2)
        {
            return obj1.Equals(obj2);
        }

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="obj1">The obj1.</param>
        /// <param name="obj2">The obj2.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(ConnectionStringData obj1, ConnectionStringData obj2)
        {
            return !(obj1 == obj2);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is ConnectionStringData ? Equals((ConnectionStringData)obj) : false;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (ConnectionString != null ? ConnectionString.GetHashCode() : 0);
                hash = hash * 31 + (int)DatabaseType;
                return hash;
            }
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.
        /// </returns>
        public bool Equals(ConnectionStringData other)
        {
            return ConnectionString == other.ConnectionString &&
                DatabaseType == other.DatabaseType;
        }
    }
}
