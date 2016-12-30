// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Trestel.Database
{
    /// <summary>
    /// Wrapper class for SQL query string.
    /// </summary>
    public sealed class Sql
    {
        private readonly string _sqlString;

        private Sql(string sqlString)
        {
            _sqlString = sqlString;
        }

        /// <summary>
        /// Gets the SQL string.
        /// </summary>
        /// <value>
        /// The SQL string.
        /// </value>
        public string SqlString
        {
            get
            {
                return _sqlString;
            }
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="Sql"/> to <see cref="System.String"/>.
        /// </summary>
        /// <param name="sql">The SQL.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator string(Sql sql)
        {
            return sql?.SqlString;
        }

        /// <summary>
        /// Constructs new SQL string instance.
        /// </summary>
        /// <param name="sqlString">The SQL string.</param>
        /// <returns>New <see cref="SqlString"/> intance.</returns>
        /// <exception cref="System.ArgumentNullException">sqlString</exception>
        public static Sql From(string sqlString)
        {
            if (String.IsNullOrEmpty(sqlString))
            {
                throw new ArgumentNullException(nameof(sqlString));
            }

            return new Sql(sqlString);
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this;
        }
    }
}
