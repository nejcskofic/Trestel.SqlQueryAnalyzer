// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Trestel.SqlQueryAnalyzer.Infrastructure.CallSiteAnalysis;
using Trestel.SqlQueryAnalyzer.Infrastructure.QueryAnalysis;

namespace Trestel.SqlQueryAnalyzer.Analyzers
{
    /// <summary>
    /// Helper class containing supported diagnostics for <see cref="SqlQueryAnalyzer"/> and methods for creating those diagnostics.
    /// </summary>
    public static class SqlQueryAnalyzerDiagnostic
    {
        // TODO: Add error type when target type does not contain any properties (bug in provider or bug in code)

        /// <summary>
        /// The category name
        /// </summary>
        public const string CategoryName = "SQL";

        /// <summary>
        /// The failed to validate diagnostic identifier
        /// </summary>
        public const string FailedToValidateDiagnosticId = "SQL0001";

        /// <summary>
        /// The errors in SQL query diagnostic identifier
        /// </summary>
        public const string ErrorsInSqlQueryDiagnosticId = "SQL0002";

        /// <summary>
        /// The unsupported diagnostic identifier
        /// </summary>
        public const string UnsupportedDiagnosticId = "SQL0003";

        /// <summary>
        /// The missing columns in query result diagnostic identifier
        /// </summary>
        public const string MissingColumnsInQueryResultDiagnosticId = "SQL0004";

        /// <summary>
        /// The unused columns in query result diagnostic identifier
        /// </summary>
        public const string UnusedColumnsInQueryResultDiagnosticId = "SQL0005";

        /// <summary>
        /// The mismatch between property types diagnostic identifier
        /// </summary>
        public const string PropertyTypeMismatchDiagnosticId = "SQL0006";

        /// <summary>
        /// The mismatch between types diagnostic identifier
        /// </summary>
        public const string TypeMismatchDiagnosticId = "SQL0007";

        /// <summary>
        /// The expected single column in query result diagnostic identifier
        /// </summary>
        public const string ExpectedSingleColumnInQueryResultDiagnosticId = "SQL0008";

        /// <summary>
        /// The missing database hint attribute diagnostic identifier
        /// </summary>
        public const string MissingDatabaseHintAttributeDiagnosticId = "SQL0009";

        /// <summary>
        /// The parameter type mismatch diagnostic identifier
        /// </summary>
        public const string ParameterTypeMismatchDiagnosticId = "SQL0010";

        /// <summary>
        /// The missing parameter diagnostic identifier
        /// </summary>
        public const string MissingParameterDiagnosticId = "SQL0011";

        /// <summary>
        /// The unused parameter diagnostic identifier
        /// </summary>
        public const string UnusedParameterDiagnosticId = "SQL0012";

        private static readonly DiagnosticDescriptor FailedToValidateDescriptor = new DiagnosticDescriptor(
            FailedToValidateDiagnosticId,
            "Unable to complete validation",
            "Could not validate query because of following error: {0}",
            CategoryName,
            DiagnosticSeverity.Warning,
            true,
            "Analyzer should be able to complete validation",
            null);

        private static readonly DiagnosticDescriptor ErrorsInSqlQueryDescriptor = new DiagnosticDescriptor(
            ErrorsInSqlQueryDiagnosticId,
            "Error in SQL statement",
            "There are following errors in SQL query:{0}",
            CategoryName,
            DiagnosticSeverity.Error,
            true,
            "SQL query should be syntactically correct and use existing database objects.",
            null);

        private static readonly DiagnosticDescriptor UnsupportedDescriptor = new DiagnosticDescriptor(
            UnsupportedDiagnosticId,
            "Validation not supported",
            "Validation of SQL query string that is not literal is not supported.",
            CategoryName,
            DiagnosticSeverity.Warning,
            true,
            "SQL query should be entered as string literal.",
            null);

        private static readonly DiagnosticDescriptor MissingColumnsInQueryResultDescriptor = new DiagnosticDescriptor(
            MissingColumnsInQueryResultDiagnosticId,
            "Missing columns",
            "Following columns were expected in result set, but were not found:{0}",
            CategoryName,
            DiagnosticSeverity.Error,
            true,
            "SQL query should return all expected columns.",
            null);

        private static readonly DiagnosticDescriptor UnusedColumnsInQueryResultDescriptor = new DiagnosticDescriptor(
            UnusedColumnsInQueryResultDiagnosticId,
            "Unused columns",
            "Following columns were found in result set, but are not being used:{0}",
            CategoryName,
            DiagnosticSeverity.Error,
            true,
            "SQL query should return only necessary columns.",
            null);

        private static readonly DiagnosticDescriptor PropertyTypeMismatchDescriptor = new DiagnosticDescriptor(
            PropertyTypeMismatchDiagnosticId,
            "Types do not match",
            "For column '{0}' expected type '{1}', but found type '{2}'.",
            CategoryName,
            DiagnosticSeverity.Error,
            true,
            "Property type should match column type.",
            null);

        private static readonly DiagnosticDescriptor TypeMismatchDescriptor = new DiagnosticDescriptor(
            TypeMismatchDiagnosticId,
            "Types do not match",
            "Expected type '{0}', but found type '{1}'.",
            CategoryName,
            DiagnosticSeverity.Error,
            true,
            "Property type should match column type.",
            null);

        private static readonly DiagnosticDescriptor ExpectedSingleColumnInQueryResultDescriptor = new DiagnosticDescriptor(
            ExpectedSingleColumnInQueryResultDiagnosticId,
            "Unexpected number of columns",
            "Expected single column of type '{0}' but found multiple columns.",
            CategoryName,
            DiagnosticSeverity.Error,
            true,
            "SQL query should return single column of matching type.",
            null);

        private static readonly DiagnosticDescriptor MissingDatabaseHintDescriptor = new DiagnosticDescriptor(
            MissingDatabaseHintAttributeDiagnosticId,
            "Missing database hint",
            "Analysis cannot continue because there is no 'Trestel.Database.Design.DatabaseHintAttribute' attribute applied to method, class or assembly.",
            CategoryName,
            DiagnosticSeverity.Warning,
            true,
            "Attribute 'Trestel.Database.Design.DatabaseHintAttribute' is required to specify connection to database for analisys.",
            null);

        private static readonly DiagnosticDescriptor ParameterTypeMismatchDescriptor = new DiagnosticDescriptor(
            ParameterTypeMismatchDiagnosticId,
            "Types do not match",
            "For parameter '{0}' expected type '{1}', but found type '{2}'.",
            CategoryName,
            DiagnosticSeverity.Error,
            true,
            "Parameter type should match one expected by database.",
            null);

        private static readonly DiagnosticDescriptor MissingParameterDescriptor = new DiagnosticDescriptor(
            MissingParameterDiagnosticId,
            "Missing parameters",
            "Following parameters were expected by query but were not found:{0}",
            CategoryName,
            DiagnosticSeverity.Error,
            true,
            "All parameters for query should be supplied.",
            null);

        private static readonly DiagnosticDescriptor UnusedParameterDescriptor = new DiagnosticDescriptor(
            UnusedParameterDiagnosticId,
            "Unused parameters",
            "Following parameters are present but not required:{0}",
            CategoryName,
            DiagnosticSeverity.Error,
            true,
            "Only parameters requried for query execution should be declared.",
            null);

        /// <summary>
        /// Gets the supported diagnostics.
        /// </summary>
        /// <returns>Array of all supported diagnostics</returns>
        internal static ImmutableArray<DiagnosticDescriptor> GetSupportedDiagnostics()
        {
            return ImmutableArray.Create(
                    FailedToValidateDescriptor,
                    ErrorsInSqlQueryDescriptor,
                    UnsupportedDescriptor,
                    MissingColumnsInQueryResultDescriptor,
                    UnusedColumnsInQueryResultDescriptor,
                    PropertyTypeMismatchDescriptor,
                    TypeMismatchDescriptor,
                    ExpectedSingleColumnInQueryResultDescriptor,
                    MissingDatabaseHintDescriptor,
                    ParameterTypeMismatchDescriptor,
                    MissingParameterDescriptor,
                    UnusedParameterDescriptor);
        }

        /// <summary>
        /// Creates the failed to validate diagnostic.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="message">The message.</param>
        /// <returns>Failed to validate diagnostic</returns>
        internal static Diagnostic CreateFailedToValidateDiagnostic(Location location, string message)
        {
            return Diagnostic.Create(FailedToValidateDescriptor, location, message);
        }

        /// <summary>
        /// Creates the errors in SQL query diagnostic.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="errors">The errors.</param>
        /// <returns>Errors in SQL query diagnostic</returns>
        internal static Diagnostic CreateErrorsInSqlQueryDiagnostic(Location location, IList<string> errors)
        {
            return Diagnostic.Create(ErrorsInSqlQueryDescriptor, location, Environment.NewLine + String.Join(Environment.NewLine, errors ?? Enumerable.Empty<string>()));
        }

        /// <summary>
        /// Creates the unsupported diagnostic.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns>Unsupported diagnostic</returns>
        internal static Diagnostic CreateUnsupportedDiagnostic(Location location)
        {
            return Diagnostic.Create(UnsupportedDescriptor, location);
        }

        /// <summary>
        /// Creates the missing columns in query result diagnostic.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="missingColumns">The missing columns.</param>
        /// <returns>Missing columns in query result diagnostic</returns>
        internal static Diagnostic CreateMissingColumnsInQueryResultDiagnostic(Location location, IList<ResultField> missingColumns)
        {
            string columnsText;
            if (missingColumns == null)
            {
                columnsText = "";
            }
            else
            {
                var builder = new StringBuilder();
                for (int i = 0; i < missingColumns.Count; i++)
                {
                    builder.AppendLine();
                    builder.Append(missingColumns[i].FieldName);
                    builder.Append(" (");
                    builder.Append(missingColumns[i].FieldType.ToDisplayString());
                    builder.Append(")");
                }

                columnsText = builder.ToString();
            }

            return Diagnostic.Create(MissingColumnsInQueryResultDescriptor, location, columnsText);
        }

        /// <summary>
        /// Creates the unused columns in query result diagnostic.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="unusedColumns">The unused columns.</param>
        /// <returns>Unused columns in query result diagnostic</returns>
        internal static Diagnostic CreateUnusedColumnsInQueryResultDiagnostic(Location location, IList<ColumnInfo> unusedColumns)
        {
            string columnsText;
            if (unusedColumns == null)
            {
                columnsText = "";
            }
            else
            {
                var builder = new StringBuilder();
                for (int i = 0; i < unusedColumns.Count; i++)
                {
                    builder.AppendLine();
                    builder.Append(unusedColumns[i].Name);
                }

                columnsText = builder.ToString();
            }

            return Diagnostic.Create(UnusedColumnsInQueryResultDescriptor, location, columnsText);
        }

        /// <summary>
        /// Creates the property type mismatch diagnostic.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <param name="foundType">Found type.</param>
        /// <returns>Property type mismatch diagnostic</returns>
        internal static Diagnostic CreatePropertyTypeMismatchDiagnostic(Location location, string propertyName, ITypeSymbol expectedType, ITypeSymbol foundType)
        {
            return Diagnostic.Create(PropertyTypeMismatchDescriptor, location, propertyName, expectedType.ToDisplayString(), foundType.ToDisplayString());
        }

        /// <summary>
        /// Creates the type mismatch diagnostic.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <param name="foundType">Found type.</param>
        /// <returns>Type mismatch diagnostic</returns>
        internal static Diagnostic CreateTypeMismatchDiagnostic(Location location, ITypeSymbol expectedType, ITypeSymbol foundType)
        {
            return Diagnostic.Create(TypeMismatchDescriptor, location, expectedType?.ToDisplayString(), foundType?.ToDisplayString());
        }

        /// <summary>
        /// Creates the expected single column in query result diagnostic.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <returns>Expected single column in query result diagnostic</returns>
        internal static Diagnostic CreateExpectedSingleColumnInQueryResultDiagnostic(Location location, ITypeSymbol expectedType)
        {
            return Diagnostic.Create(ExpectedSingleColumnInQueryResultDescriptor, location, expectedType?.ToDisplayString());
        }

        /// <summary>
        /// Creates the missing database hint diagnostic.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns>Missing database hint diagnostic</returns>
        internal static Diagnostic CreateMissingDatabaseHintDiagnostic(Location location)
        {
            return Diagnostic.Create(MissingDatabaseHintDescriptor, location);
        }

        /// <summary>
        /// Creates the parameter type mismatch diagnostic.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="expectedType">The expected type.</param>
        /// <param name="foundType">Type of the found.</param>
        /// <returns>Parameter type mismatch diagnostic</returns>
        internal static Diagnostic CreateParameterTypeMismatchDiagnostic(Location location, string parameterName, ITypeSymbol expectedType, ITypeSymbol foundType)
        {
            return Diagnostic.Create(ParameterTypeMismatchDescriptor, location, parameterName, expectedType?.ToDisplayString(), foundType?.ToDisplayString());
        }

        /// <summary>
        /// Creates the missing parameter diagnostic.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="missingParameters">The missing parameters.</param>
        /// <returns>Missing parameter diagnostic</returns>
        internal static Diagnostic CreateMissingParameterDiagnostic(Location location, IList<ParameterInfo> missingParameters)
        {
            string parametersText;
            if (missingParameters == null)
            {
                parametersText = "";
            }
            else
            {
                var builder = new StringBuilder();
                for (int i = 0; i < missingParameters.Count; i++)
                {
                    builder.AppendLine();
                    builder.Append(missingParameters[i].ParameterName);
                    builder.Append(" (");
                    builder.Append(missingParameters[i].ParameterType.FullName);
                    builder.Append(")");
                }

                parametersText = builder.ToString();
            }

            return Diagnostic.Create(MissingParameterDescriptor, location, parametersText);
        }

        /// <summary>
        /// Creates the unused parameter diagnostic.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="unusedParameters">The unused parameters.</param>
        /// <returns>Unused parameter diagnostic</returns>
        internal static Diagnostic CreateUnusedParameterDiagnostic(Location location, IList<Parameter> unusedParameters)
        {
            string parametersText;
            if (unusedParameters == null)
            {
                parametersText = "";
            }
            else
            {
                var builder = new StringBuilder();
                for (int i = 0; i < unusedParameters.Count; i++)
                {
                    builder.AppendLine();
                    builder.Append(unusedParameters[i].ParameterName);
                }

                parametersText = builder.ToString();
            }

            return Diagnostic.Create(UnusedParameterDescriptor, location, parametersText);
        }
    }
}
