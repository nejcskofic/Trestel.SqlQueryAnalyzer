// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data.SqlClient;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Trestel.SqlQueryAnalyzer.Common;
using Trestel.SqlQueryAnalyzer.Infrastructure;
using Trestel.SqlQueryAnalyzer.Infrastructure.QueryAnalysis;

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
        /// Gets a value indicating whether throttling should be enabled. If static validation provider performs any lengthy operation
        /// (such as direct query to database), you should enable throttling which will delay call to <see cref="ValidateAsync(string, bool, CancellationToken)" />.
        /// While user is typing, query analyzer tasks may get cancelled before expensive calls will be performed in
        /// <see cref="ValidateAsync(string, bool, CancellationToken)" />.
        /// </summary>
        /// <value>
        ///   <c>true</c> if throttling is enabeld; otherwise, <c>false</c>.
        /// </value>
        public bool EnableThrottling
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Validates the specified raw SQL query.
        /// </summary>
        /// <param name="rawSqlQuery">The raw SQL query.</param>
        /// <param name="analyzeParameterInfo">if set to <c>true</c> parameter info should be supplied.</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>
        /// Validation result of provided raw query along with expected input parameters and returning result set.
        /// </returns>
        /// <exception cref="System.ArgumentException">Argument null or empty. - rawSqlQuery</exception>
        public async Task<Result<ValidatedQuery>> ValidateAsync(string rawSqlQuery, bool analyzeParameterInfo, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(rawSqlQuery)) throw new ArgumentException("Argument null or empty.", nameof(rawSqlQuery));

            List<string> errors = new List<string>();
            var builder = ValidatedQuery.New();
            bool hasErrors = false;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                hasErrors = await ValidateQueryAndDescribeResultSetAsync(connection, rawSqlQuery, builder, errors, cancellationToken);

                // if there are no errors check what parameters are expected
                if (!hasErrors && analyzeParameterInfo)
                {
                    await DescribeExpectedParametersAsync(connection, rawSqlQuery, builder, cancellationToken);
                }
            }

            if (!hasErrors)
            {
                return Result.Success(builder.Build());
            }
            else
            {
                return Result.Failure<ValidatedQuery>(errors);
            }
        }

        private static async Task<bool> ValidateQueryAndDescribeResultSetAsync(SqlConnection connection, string rawSqlQuery, ValidatedQuery.Builder builder, List<string> errors, CancellationToken cancellationToken)
        {
            var hasErrors = false;
            var command = CreateQueryForDescribingFirstResultSet(connection, rawSqlQuery);
            using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
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

            return hasErrors;
        }

        private static async Task DescribeExpectedParametersAsync(SqlConnection connection, string rawSqlQuery, ValidatedQuery.Builder builder, CancellationToken cancellationToken)
        {
            // optimization - don't perform query if there are no parameters
            if (!rawSqlQuery.Contains("@")) return;

            var command = CreateQueryForDescribingMissingParameters(connection, rawSqlQuery);
            using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (reader.Read())
                {
                    var parameterName = reader.GetString(1);
                    var parameterSqlType = (SqlServerType)reader.GetInt32(2);

                    // TODO: nullability w.r.t. to input/output parameters
                    builder.AddParameter(parameterName.TrimStart('@'), parameterSqlType.GetEquivalentCLRType(true));
                }
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

        private static SqlCommand CreateQueryForDescribingMissingParameters(SqlConnection connection, string rawSqlQuery)
        {
            var command = connection.CreateCommand();
            command.CommandText = "sp_describe_undeclared_parameters";
            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@tsql", rawSqlQuery));
            return command;
        }
    }
}
