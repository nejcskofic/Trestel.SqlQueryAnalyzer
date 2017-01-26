// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

namespace Trestel.Database
{
    /// <summary>
    /// Wrapper class for SQL query string.
    /// </summary>
    public static class Sql
    {
        /// <summary>
        /// Wraps string which contains SQL query for analysis. Returns same instance as is given.
        /// </summary>
        /// <param name="sqlString">The SQL string.</param>
        /// <returns>Instance reveived as argument.</returns>
        public static string From(string sqlString)
        {
            return sqlString;
        }
    }
}
