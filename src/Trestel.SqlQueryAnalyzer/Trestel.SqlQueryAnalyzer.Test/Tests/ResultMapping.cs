using System;
using TestHelper;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using Trestel.SqlQueryAnalyzer.Infrastructure;
using Trestel.SqlQueryAnalyzer.Design;
using Trestel.SqlQueryAnalyzer.Analyzers;
using Templates;

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
                ValidationResult.Success(
                    new ValidatedQuery.Builder()
                        .AddOutputColumn("BusinessEntityID", typeof(int))
                        .AddOutputColumn("Title", typeof(string))
                        .AddOutputColumn("FirstName", typeof(string))
                        .AddOutputColumn("LastName", typeof(string))
                        .AddOutputColumn("ModifiedDate", typeof(DateTime)).Build()));

            var factory = new ServiceFactory().RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection));

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
                ValidationResult.Success(
                    new ValidatedQuery.Builder()
                        .AddOutputColumn("BusinessEntityID", typeof(int))
                        .AddOutputColumn("Title", typeof(string))
                        .AddOutputColumn("FirstName", typeof(string))
                        .AddOutputColumn("LastName", typeof(string))
                        .AddOutputColumn("ModifiedDate", typeof(DateTime)).Build()));

            var factory = new ServiceFactory().RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection));

            var expected = new DiagnosticResult
            {
                Id = SqlQueryAnalyzer.UnusedColumnsInQueryResultDiagnosticId,
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
                ValidationResult.Success(
                    new ValidatedQuery.Builder()
                        .AddOutputColumn("BusinessEntityID", typeof(int))
                        .AddOutputColumn("Title", typeof(string))
                        .AddOutputColumn("FirstName", typeof(string))
                        .AddOutputColumn("ModifiedDate", typeof(DateTime)).Build()));

            var factory = new ServiceFactory().RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection));

            var expected = new DiagnosticResult
            {
                Id = SqlQueryAnalyzer.MissingColumnsInQueryResultDiagnosticId,
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
                ValidationResult.Success(
                    new ValidatedQuery.Builder()
                        .AddOutputColumn("BusinessEntityID", typeof(int))
                        .AddOutputColumn("Title", typeof(string))
                        .AddOutputColumn("FirstName", typeof(string))
                        .AddOutputColumn("LastName", typeof(string))
                        .AddOutputColumn("ModifiedDate", typeof(DateTime)).Build()));

            var factory = new ServiceFactory().RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection));

            var expected = new DiagnosticResult
            {
                Id = SqlQueryAnalyzer.MismatchBetweenPropertyTypesDiagnosticId,
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
                ValidationResult.Success(
                    new ValidatedQuery.Builder()
                        .AddOutputColumn("BusinessEntityID", typeof(int))
                        .AddOutputColumn("Title", typeof(string))
                        .AddOutputColumn("FirstName", typeof(string))
                        .AddOutputColumn("LastName", typeof(string))
                        .AddOutputColumn("ModifiedDate", typeof(DateTime)).Build()));

            var factory = new ServiceFactory().RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection));

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
                ValidationResult.Success(
                    new ValidatedQuery.Builder()
                        .AddOutputColumn("rowguid", typeof(Guid))
                        .Build()));

            var factory = new ServiceFactory().RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection));

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
                ValidationResult.Success(
                    new ValidatedQuery.Builder()
                        .AddOutputColumn("rowguid", typeof(Guid))
                        .Build()));

            var factory = new ServiceFactory().RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection));

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
                ValidationResult.Success(
                    new ValidatedQuery.Builder()
                        .AddOutputColumn("rowguid", typeof(Guid?))
                        .Build()));

            var factory = new ServiceFactory().RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection));

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
                ValidationResult.Success(
                    new ValidatedQuery.Builder()
                        .AddOutputColumn("RecordVersion", typeof(byte[]))
                        .Build()));

            var factory = new ServiceFactory().RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection));

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
                ValidationResult.Success(
                    new ValidatedQuery.Builder()
                        .AddOutputColumn("RecordVersion", typeof(byte[]))
                        .Build()));

            var factory = new ServiceFactory().RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection));

            var expected = new DiagnosticResult
            {
                Id = SqlQueryAnalyzer.MismatchBetweenTypesDiagnosticId,
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
                ValidationResult.Success(
                    new ValidatedQuery.Builder()
                        .AddOutputColumn("rowguid", typeof(Guid))
                        .Build()));

            var factory = new ServiceFactory().RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection));

            var expected = new DiagnosticResult
            {
                Id = SqlQueryAnalyzer.MismatchBetweenTypesDiagnosticId,
                Message = "Expected type 'System.Guid', but found type 'string'.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 34) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }
    }
}
