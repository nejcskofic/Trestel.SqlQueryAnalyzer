// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.SqlClient;
using System.Globalization;
using Trestel.SqlQueryAnalyzer.Infrastructure;
using Trestel.SqlQueryAnalyzer.Infrastructure.Models;

namespace Trestel.SqlQueryAnalyzer.Providers.SqlServer
{
    /// <summary>
    /// Provides validation provider for SQL Server. This provider uses TVF introduced in SQL Server 2014 for providing input parameter type information and
    /// returning result set.
    /// </summary>
    /// <seealso cref="Trestel.SqlQueryAnalyzer.Infrastructure.IQueryValidationProvider" />
    public class SqlServerQueryValidationProvider : IQueryValidationProvider
    {
        // Ignore error 11529: The metadata could not be determined because every code path results in an error; see previous errors for some of these.
        // Ignore error 11501: The batch could not be analyzed because of compile errors.
        private static readonly ImmutableArray<int> _suppressedErrors = ImmutableArray.Create(11529, 11501);

        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerQueryValidationProvider"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <exception cref="System.ArgumentNullException">connectionString</exception>
        public SqlServerQueryValidationProvider(string connectionString)
        {
            if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));

            _connectionString = connectionString;
        }

        /// <summary>
        /// Validates the specified raw SQL query.
        /// </summary>
        /// <param name="rawSqlQuery">The raw SQL query.</param>
        /// <returns>
        /// Validation result of provided raw query along with expected input parameters and returning result set.
        /// </returns>
        /// <exception cref="System.ArgumentException">Argument null or empty. - rawSqlQuery</exception>
        public ValidationResult Validate(string rawSqlQuery)
        {
            if (String.IsNullOrEmpty(rawSqlQuery)) throw new ArgumentException("Argument null or empty.", nameof(rawSqlQuery));

            List<string> errors = new List<string>();
            ValidatedQuery.Builder builder = new ValidatedQuery.Builder();
            bool hasErrors = false;

            using (var connection = new SqlConnection(_connectionString))
            {
                var command = CreateQueryForDescribingFirstResultSet(connection, rawSqlQuery);

                connection.Open();
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (reader.IsDBNull(5))
                    {
                        // success
                        if (!reader.GetBoolean(4))
                        {
                            // visible column
                            var sqlType = (SqlServerType)reader.GetInt32(2);
                            builder.AddOutputColumn(reader.GetString(1), sqlType.GetEquivalentCLRType(reader.GetBoolean(3)));
                        }
                    }
                    else
                    {
                        // error
                        hasErrors = true;
                        var errorNumber = reader.GetInt32(5);

                        if (!_suppressedErrors.Contains(errorNumber))
                        {
                            var error = String.Format(CultureInfo.InvariantCulture, "Msg {0}, Level {1}, State {2}: {3}", errorNumber, reader.GetInt32(6), reader.GetInt32(7), reader.GetString(8));
                            errors.Add(error);
                        }
                    }
                }
            }

            if (!hasErrors)
            {
                return ValidationResult.Success(builder.Build());
            }
            else
            {
                return ValidationResult.Failure(errors);
            }
        }

        private static SqlCommand CreateQueryForDescribingFirstResultSet(SqlConnection connection, string rawSqlQuery)
        {
            var command = connection.CreateCommand();
            command.CommandText = @"
SELECT r.column_ordinal, r.name, r.system_type_id, r.is_nullable, r.is_hidden, r.error_number, r.error_severity, r.error_state, r.error_message
FROM sys.dm_exec_describe_first_result_set(@Sql, N'', NULL) r";
            command.CommandType = System.Data.CommandType.Text;
            command.Parameters.Add(new SqlParameter("@Sql", rawSqlQuery));
            return command;
        }
    }
}
