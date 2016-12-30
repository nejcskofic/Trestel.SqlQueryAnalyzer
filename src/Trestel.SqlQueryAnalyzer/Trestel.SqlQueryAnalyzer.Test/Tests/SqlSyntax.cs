// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using Templates;
using TestHelper;
using Trestel.SqlQueryAnalyzer.Analyzers;
using Trestel.SqlQueryAnalyzer.Design;
using Trestel.SqlQueryAnalyzer.Infrastructure;
using Trestel.SqlQueryAnalyzer.Infrastructure.Models;

namespace Tests
{
    [TestFixture]
    public class SqlSyntax : SqlQueryDiagnosticVerifier
    {
        [Test]
        public void NothingToReport()
        {
            var test = @"
using System;

namespace TestNamespace
{
    class Program
    {
        static void Main(string[] args)
        {
        }
    }
}
";
            var factory = new ServiceFactory();

            VerifyCSharpDiagnostic(factory, test);
        }

        [Test]
        public void ValidateParsing()
        {
            var query = "SELECT BusinessEntityID, Title, FirstName, LastName FROM Person.Person";
            var source = SourceCodeTemplates.GetSourceCodeFromSimpleTemplate(query);

            var mockupValidationProvider = new MockupValidationProvider();
            mockupValidationProvider.AddExpectedResult(
                query,
                ValidationResult.Success(new ValidatedQuery.Builder().Build()));

            var factory = new ServiceFactory().RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection));

            VerifyCSharpDiagnostic(factory, source);

            // verify that validation provider was called
            Assert.AreEqual(1, mockupValidationProvider.AccessedQueries.Count, "Expected one query.");
            Assert.AreEqual(query, mockupValidationProvider.AccessedQueries[0], "Queries should match.");
        }

        [Test]
        public void ValidateErrorSyntaxErrorReporting()
        {
            var query = "SELECTA BusinessEntityID, Title, FirstName, LastName FROM Person.Person";
            var source = SourceCodeTemplates.GetSourceCodeFromSimpleTemplate(query);

            var mockupValidationProvider = new MockupValidationProvider();
            mockupValidationProvider.AddExpectedResult(
                query,
                ValidationResult.Failure(new List<string>() { "Incorrect syntax near the keyword 'FROM'." }));

            var factory = new ServiceFactory().RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection));

            var expected = new DiagnosticResult
            {
                Id = SqlQueryAnalyzer.ErrorsInSqlQueryDiagnosticId,
                Message = "There are following errors in SQL query:\nIncorrect syntax near the keyword 'FROM'.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 23) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);

            Assert.AreEqual(1, mockupValidationProvider.AccessedQueries.Count, "Expected one query.");
            Assert.AreEqual(query, mockupValidationProvider.AccessedQueries[0], "Queries should match.");
        }
    }
}
