// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

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
        /// The missing database hint attribute diagnostic identifier
        /// </summary>
        public const string MissingDatabaseHintAttributeDiagnosticId = "SQL0008";

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
            "There are following errors in SQL query:\n{0}",
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
            "Following columns were expected in result set, but were not found:\n{0}",
            CategoryName,
            DiagnosticSeverity.Error,
            true,
            "SQL query should return all expected columns.",
            null);

        private static readonly DiagnosticDescriptor UnusedColumnsInQueryResultDescriptor = new DiagnosticDescriptor(
            UnusedColumnsInQueryResultDiagnosticId,
            "Unused columns",
            "Following columns were found in result set, but are not being used:\n{0}",
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

        private static readonly DiagnosticDescriptor MissingDatabaseHintDescriptor = new DiagnosticDescriptor(
            MissingDatabaseHintAttributeDiagnosticId,
            "Missing database hint",
            "Analysis cannot continue because there is no 'Trestel.Database.Design.DatabaseHintAttribute' attribute applied to method, class or assembly.",
            CategoryName,
            DiagnosticSeverity.Warning,
            true,
            "Attribute 'Trestel.Database.Design.DatabaseHintAttribute' is required to specify connection to database for analisys.",
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
                    MissingDatabaseHintDescriptor);
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
        internal static Diagnostic CreateErrorsInSqlQueryDiagnostic(Location location, IEnumerable<string> errors)
        {
            return Diagnostic.Create(ErrorsInSqlQueryDescriptor, location, String.Join("\n", errors ?? Enumerable.Empty<string>()));
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
        internal static Diagnostic CreateMissingColumnsInQueryResultDiagnostic(Location location, IEnumerable<string> missingColumns)
        {
            return Diagnostic.Create(MissingColumnsInQueryResultDescriptor, location, String.Join("\n", missingColumns ?? Enumerable.Empty<string>()));
        }

        /// <summary>
        /// Creates the unused columns in query result diagnostic.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="unusedColumns">The unused columns.</param>
        /// <returns>Unused columns in query result diagnostic</returns>
        internal static Diagnostic CreateUnusedColumnsInQueryResultDiagnostic(Location location, IEnumerable<string> unusedColumns)
        {
            return Diagnostic.Create(UnusedColumnsInQueryResultDescriptor, location, String.Join("\n", unusedColumns ?? Enumerable.Empty<string>()));
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
        /// Creates the missing database hint diagnostic.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns>Missing database hint diagnostic</returns>
        internal static Diagnostic CreateMissingDatabaseHintDiagnostic(Location location)
        {
            return Diagnostic.Create(MissingDatabaseHintDescriptor, location);
        }
    }
}
