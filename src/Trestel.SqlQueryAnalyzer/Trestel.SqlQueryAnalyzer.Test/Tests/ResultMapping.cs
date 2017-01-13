// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using Templates;
using TestHelper;
using Trestel.SqlQueryAnalyzer.Analyzers;
using Trestel.SqlQueryAnalyzer.CallSiteAnalyzers;
using Trestel.SqlQueryAnalyzer.Common;
using Trestel.SqlQueryAnalyzer.Design;
using Trestel.SqlQueryAnalyzer.Infrastructure;
using Trestel.SqlQueryAnalyzer.Infrastructure.QueryAnalysis;

namespace Tests
{
    [TestFixture]
    public class ResultMapping : SqlQueryDiagnosticVerifier
    {
        [Test]
        public void SimpleMappingOfTypes()
        {
            var query = "SELECT BusinessEntityID, Title, FirstName, MiddleName, LastName, ModifiedDate FROM Person.Person";
            var additionalClass = @"
    class Person
    {
        public int BusinessEntityID { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
";
            var source = SourceCodeTemplates.GetSourceCodeFromTemplateWithResultMapping(query, "Person", additionalClass);

            var mockupValidationProvider = new MockupValidationProvider();
            mockupValidationProvider.AddExpectedResult(
                query,
                Result.Success(
                    ValidatedQuery.New()
                        .AddOutputColumn("BusinessEntityID", typeof(int))
                        .AddOutputColumn("Title", typeof(string))
                        .AddOutputColumn("FirstName", typeof(string))
                        .AddOutputColumn("LastName", typeof(string))
                        .AddOutputColumn("ModifiedDate", typeof(DateTime)).Build()));

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection))
                .Build();

            VerifyCSharpDiagnostic(factory, source);
        }

        [Test]
        public void MissingColumnInResultObject()
        {
            var query = "SELECT BusinessEntityID, Title, FirstName, MiddleName, LastName, ModifiedDate FROM Person.Person";
            var additionalClass = @"
    class Person
    {
        public int BusinessEntityID { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
";
            var source = SourceCodeTemplates.GetSourceCodeFromTemplateWithResultMapping(query, "Person", additionalClass);

            var mockupValidationProvider = new MockupValidationProvider();
            mockupValidationProvider.AddExpectedResult(
                query,
                Result.Success(
                    ValidatedQuery.New()
                        .AddOutputColumn("BusinessEntityID", typeof(int))
                        .AddOutputColumn("Title", typeof(string))
                        .AddOutputColumn("FirstName", typeof(string))
                        .AddOutputColumn("LastName", typeof(string))
                        .AddOutputColumn("ModifiedDate", typeof(DateTime)).Build()));

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection))
                .RegisterCallSiteAnalyzerInstance(new GenericAnalyzer())
                .Build();

            var expected = new DiagnosticResult
            {
                Id = SqlQueryAnalyzerDiagnostic.UnusedColumnsInQueryResultDiagnosticId,
                Message = "Following columns were found in result set, but are not being used:\nLastName",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 34) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void MissingColumnInQueryResultSet()
        {
            var query = "SELECT BusinessEntityID, Title, FirstName, MiddleName, ModifiedDate FROM Person.Person";
            var additionalClass = @"
    class Person
    {
        public int BusinessEntityID { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
";
            var source = SourceCodeTemplates.GetSourceCodeFromTemplateWithResultMapping(query, "Person", additionalClass);

            var mockupValidationProvider = new MockupValidationProvider();
            mockupValidationProvider.AddExpectedResult(
                query,
                Result.Success(
                    ValidatedQuery.New()
                        .AddOutputColumn("BusinessEntityID", typeof(int))
                        .AddOutputColumn("Title", typeof(string))
                        .AddOutputColumn("FirstName", typeof(string))
                        .AddOutputColumn("ModifiedDate", typeof(DateTime)).Build()));

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection))
                .RegisterCallSiteAnalyzerInstance(new GenericAnalyzer())
                .Build();

            var expected = new DiagnosticResult
            {
                Id = SqlQueryAnalyzerDiagnostic.MissingColumnsInQueryResultDiagnosticId,
                Message = "Following columns were expected in result set, but were not found:\nLastName (string)",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 34) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void TypeMissmatch()
        {
            var query = "SELECT BusinessEntityID, Title, FirstName, MiddleName, LastName, ModifiedDate FROM Person.Person";
            var additionalClass = @"
    class Person
    {
        public int BusinessEntityID { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ModifiedDate { get; set; }
    }
";
            var source = SourceCodeTemplates.GetSourceCodeFromTemplateWithResultMapping(query, "Person", additionalClass);

            var mockupValidationProvider = new MockupValidationProvider();
            mockupValidationProvider.AddExpectedResult(
                query,
                Result.Success(
                    ValidatedQuery.New()
                        .AddOutputColumn("BusinessEntityID", typeof(int))
                        .AddOutputColumn("Title", typeof(string))
                        .AddOutputColumn("FirstName", typeof(string))
                        .AddOutputColumn("LastName", typeof(string))
                        .AddOutputColumn("ModifiedDate", typeof(DateTime)).Build()));

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection))
                .RegisterCallSiteAnalyzerInstance(new GenericAnalyzer())
                .Build();

            var expected = new DiagnosticResult
            {
                Id = SqlQueryAnalyzerDiagnostic.PropertyTypeMismatchDiagnosticId,
                Message = "For column 'ModifiedDate' expected type 'System.DateTime', but found type 'string'.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 34) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void AssignToNullableOrObject()
        {
            var query = "SELECT BusinessEntityID, Title, FirstName, MiddleName, LastName, ModifiedDate FROM Person.Person";
            var additionalClass = @"
    class Person
    {
        public int BusinessEntityID { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public object LastName { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
";
            var test = SourceCodeTemplates.GetSourceCodeFromTemplateWithResultMapping(query, "Person", additionalClass);

            var mockupValidationProvider = new MockupValidationProvider();
            mockupValidationProvider.AddExpectedResult(
                query,
                Result.Success(
                    ValidatedQuery.New()
                        .AddOutputColumn("BusinessEntityID", typeof(int))
                        .AddOutputColumn("Title", typeof(string))
                        .AddOutputColumn("FirstName", typeof(string))
                        .AddOutputColumn("LastName", typeof(string))
                        .AddOutputColumn("ModifiedDate", typeof(DateTime)).Build()));

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection))
                .Build();

            VerifyCSharpDiagnostic(factory, test);
        }

        [Test]
        public void AssignSingleColumn()
        {
            var query = "SELECT rowguid FROM Person.Person";
            var source = SourceCodeTemplates.GetSourceCodeFromTemplateWithResultMapping(query, "Guid");

            var mockupValidationProvider = new MockupValidationProvider();
            mockupValidationProvider.AddExpectedResult(
                query,
                Result.Success(
                    ValidatedQuery.New()
                        .AddOutputColumn("rowguid", typeof(Guid))
                        .Build()));

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection))
                .Build();

            VerifyCSharpDiagnostic(factory, source);
        }

        [Test]
        public void SingleColumn()
        {
            var query = "SELECT rowguid FROM Person.Person";
            var source = SourceCodeTemplates.GetSourceCodeFromTemplateWithResultMapping(query, "Guid");

            var mockupValidationProvider = new MockupValidationProvider();
            mockupValidationProvider.AddExpectedResult(
                query,
                Result.Success(
                    ValidatedQuery.New()
                        .AddOutputColumn("rowguid", typeof(Guid))
                        .Build()));

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection))
                .Build();

            VerifyCSharpDiagnostic(factory, source);
        }

        [Test]
        public void SingleNullableColumn()
        {
            var query = "SELECT rowguid FROM Person.Person";
            var source = SourceCodeTemplates.GetSourceCodeFromTemplateWithResultMapping(query, "Guid?");

            var mockupValidationProvider = new MockupValidationProvider();
            mockupValidationProvider.AddExpectedResult(
                query,
                Result.Success(
                    ValidatedQuery.New()
                        .AddOutputColumn("rowguid", typeof(Guid?))
                        .Build()));

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection))
                .Build();

            VerifyCSharpDiagnostic(factory, source);
        }

        [Test]
        public void SingleByteArrayColumn()
        {
            var query = "SELECT RecordVersion FROM Person.Person";
            var source = SourceCodeTemplates.GetSourceCodeFromTemplateWithResultMapping(query, "byte[]");

            var mockupValidationProvider = new MockupValidationProvider();
            mockupValidationProvider.AddExpectedResult(
                query,
                Result.Success(
                    ValidatedQuery.New()
                        .AddOutputColumn("RecordVersion", typeof(byte[]))
                        .Build()));

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection))
                .Build();

            VerifyCSharpDiagnostic(factory, source);
        }

        [Test]
        public void SingleByteArrayColumnToString()
        {
            var query = "SELECT RecordVersion FROM Person.Person";
            var source = SourceCodeTemplates.GetSourceCodeFromTemplateWithResultMapping(query, "string");

            var mockupValidationProvider = new MockupValidationProvider();
            mockupValidationProvider.AddExpectedResult(
                query,
                Result.Success(
                    ValidatedQuery.New()
                        .AddOutputColumn("RecordVersion", typeof(byte[]))
                        .Build()));

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection))
                .RegisterCallSiteAnalyzerInstance(new GenericAnalyzer())
                .Build();

            var expected = new DiagnosticResult
            {
                Id = SqlQueryAnalyzerDiagnostic.TypeMismatchDiagnosticId,
                Message = "Expected type 'byte[]', but found type 'string'.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 34) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void SingleColumnWithTypeMissmatch()
        {
            var query = "SELECT rowguid FROM Person.Person";
            var source = SourceCodeTemplates.GetSourceCodeFromTemplateWithResultMapping(query, "string");

            var mockupValidationProvider = new MockupValidationProvider();
            mockupValidationProvider.AddExpectedResult(
                query,
                Result.Success(
                    ValidatedQuery.New()
                        .AddOutputColumn("rowguid", typeof(Guid))
                        .Build()));

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection))
                .RegisterCallSiteAnalyzerInstance(new GenericAnalyzer())
                .Build();

            var expected = new DiagnosticResult
            {
                Id = SqlQueryAnalyzerDiagnostic.TypeMismatchDiagnosticId,
                Message = "Expected type 'System.Guid', but found type 'string'.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 34) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }
    }
}
