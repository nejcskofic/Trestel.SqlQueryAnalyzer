﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trestel.SqlQueryAnalyzer.Infrastructure;

namespace Trestel.SqlQueryAnalyzer.Providers.SqlServer
{
    public class SqlServerQueryValidationProvider : IQueryValidationProvider
    {
        // Ignore error 11529: The metadata could not be determined because every code path results in an error; see previous errors for some of these.
        // Ignore error 11501: The batch could not be analyzed because of compile errors.
        private static readonly ImmutableArray<int> _suppressedErrors = ImmutableArray.Create(11529, 11501);

        private readonly string _connectionString;

        public SqlServerQueryValidationProvider(string connectionString)
        {
            if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));

            _connectionString = connectionString;
        }

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
