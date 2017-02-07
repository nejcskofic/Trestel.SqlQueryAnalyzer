// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Moq;
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
    public partial class DapperSpecificCallSiteAnalysis : SqlQueryDiagnosticVerifier
    {
        [Test]
        public void ValidateSingleParameter()
        {
            var query = "DELETE FROM Person.Person WHERE BusinessEntityID = @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    connection.Execute(Sql.From(""{query}""), new {{ p1 = 1}});
}}
";

            var source = SourceCodeTemplates.GetSourceCodeFromDapperTemplate(mainMethod);

            var mockupValidationProvider = new Mock<IQueryValidationProvider>();
            mockupValidationProvider
                .Setup(x => x.ValidateAsync(query, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Result.Success(ValidatedQuery.New()
                        .AddParameter("p1", typeof(int?)).Build())));

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.Object)
                .RegisterCallSiteAnalyzerInstance(new DapperAnalyzer())
                .Build();

            VerifyCSharpDiagnostic(factory, source);
        }

        [Test]
        public void ValidateDbStringParameter()
        {
            var query = "DELETE FROM Person.Person WHERE PersonType = @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    connection.Execute(Sql.From(""{query}""), new {{ p1 = new DbString {{ IsAnsi = true, Value = ""EM"" }} }});
}}
";

            var source = SourceCodeTemplates.GetSourceCodeFromDapperTemplate(mainMethod);

            var mockupValidationProvider = new Mock<IQueryValidationProvider>();
            mockupValidationProvider
                .Setup(x => x.ValidateAsync(query, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Result.Success(ValidatedQuery.New()
                        .AddParameter("p1", typeof(string)).Build())));

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.Object)
                .RegisterCallSiteAnalyzerInstance(new DapperAnalyzer())
                .Build();

            VerifyCSharpDiagnostic(factory, source);
        }

        [Test]
        public void ValidateSingleMissingParameter()
        {
            var query = "DELETE FROM Person.Person WHERE BusinessEntityID = @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    connection.Execute(Sql.From(""{query}""));
}}
";

            var source = SourceCodeTemplates.GetSourceCodeFromDapperTemplate(mainMethod);

            var mockupValidationProvider = new Mock<IQueryValidationProvider>();
            mockupValidationProvider
                .Setup(x => x.ValidateAsync(query, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Result.Success(ValidatedQuery.New()
                        .AddParameter("p1", typeof(int?)).Build())));

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.Object)
                .RegisterCallSiteAnalyzerInstance(new DapperAnalyzer())
                .Build();

            var expected = new DiagnosticResult
            {
                Id = SqlQueryAnalyzerDiagnostic.MissingParameterDiagnosticId,
                Message = "Following parameters were expected by query but were not found:" + Environment.NewLine + "p1 (int?)",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 5) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ValidateSingleMissingParameterOutOfTwo()
        {
            var query = "DELETE FROM Person.Person WHERE BusinessEntityID = @p1 AND NameStyle = @p2";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    connection.Execute(Sql.From(""{query}""), new {{ p1 = 1}});
}}
";

            var source = SourceCodeTemplates.GetSourceCodeFromDapperTemplate(mainMethod);

            var mockupValidationProvider = new Mock<IQueryValidationProvider>();
            mockupValidationProvider
                .Setup(x => x.ValidateAsync(query, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Result.Success(ValidatedQuery.New()
                        .AddParameter("p1", typeof(int?))
                        .AddParameter("p2", typeof(bool?)).Build())));

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.Object)
                .RegisterCallSiteAnalyzerInstance(new DapperAnalyzer())
                .Build();

            var expected = new DiagnosticResult
            {
                Id = SqlQueryAnalyzerDiagnostic.MissingParameterDiagnosticId,
                Message = "Following parameters were expected by query but were not found:" + Environment.NewLine + "p2 (bool?)",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 5) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ValidateSingleUnnecessaryParameter()
        {
            var query = "DELETE FROM Person.Person WHERE BusinessEntityID = 1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    connection.Execute(Sql.From(""{query}""), new {{ p1 = 1}});
}}
";

            var source = SourceCodeTemplates.GetSourceCodeFromDapperTemplate(mainMethod);

            var mockupValidationProvider = new Mock<IQueryValidationProvider>();
            mockupValidationProvider
                .Setup(x => x.ValidateAsync(query, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Result.Success(ValidatedQuery.New().Build())));

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.Object)
                .RegisterCallSiteAnalyzerInstance(new DapperAnalyzer())
                .Build();

            var expected = new DiagnosticResult
            {
                Id = SqlQueryAnalyzerDiagnostic.UnusedParameterDiagnosticId,
                Message = "Following parameters are present but not required:" + Environment.NewLine + "p1",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 5) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ValidateMissnamedParameter()
        {
            var query = "DELETE FROM Person.Person WHERE BusinessEntityID = @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    connection.Execute(Sql.From(""{query}""), new {{ p2 = 1}});
}}
";

            var source = SourceCodeTemplates.GetSourceCodeFromDapperTemplate(mainMethod);

            var mockupValidationProvider = new Mock<IQueryValidationProvider>();
            mockupValidationProvider
                .Setup(x => x.ValidateAsync(query, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Result.Success(ValidatedQuery.New()
                    .AddParameter("p1", typeof(int?)).Build())));

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.Object)
                .RegisterCallSiteAnalyzerInstance(new DapperAnalyzer())
                .Build();

            var expected1 = new DiagnosticResult
            {
                Id = SqlQueryAnalyzerDiagnostic.UnusedParameterDiagnosticId,
                Message = "Following parameters are present but not required:" + Environment.NewLine + "p2",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 5) }
            };
            var expected2 = new DiagnosticResult
            {
                Id = SqlQueryAnalyzerDiagnostic.MissingParameterDiagnosticId,
                Message = "Following parameters were expected by query but were not found:" + Environment.NewLine + "p1 (int?)",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 5) }
            };

            VerifyCSharpDiagnostic(factory, source, expected1, expected2);
        }

        [Test]
        public void ValidateMissmatchedParameterType()
        {
            var query = "DELETE FROM Person.Person WHERE BusinessEntityID = @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    connection.Execute(Sql.From(""{query}""), new {{ p1 = true}});
}}
";

            var source = SourceCodeTemplates.GetSourceCodeFromDapperTemplate(mainMethod);

            var mockupValidationProvider = new Mock<IQueryValidationProvider>();
            mockupValidationProvider
                .Setup(x => x.ValidateAsync(query, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Result.Success(ValidatedQuery.New()
                    .AddParameter("p1", typeof(int?)).Build())));

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.Object)
                .RegisterCallSiteAnalyzerInstance(new DapperAnalyzer())
                .Build();

            var expected = new DiagnosticResult
            {
                Id = SqlQueryAnalyzerDiagnostic.ParameterTypeMismatchDiagnosticId,
                Message = "For parameter 'p1' expected type 'int?', but found type 'bool'.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 5) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ExecuteCommandMultipleTimesViaArray()
        {
            var query = "INSERT INTO Person.Person(BusinessEntityID, FirstName, LastName) VALUES (@p1, @p2, @p3)";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    connection.Execute(Sql.From(""{query}""), new[] {{ new {{ p1 = 1, p2 = ""Alice""}}, new {{ p1 = 2, p2 = ""Bob"" }} }});
}}
";

            var source = SourceCodeTemplates.GetSourceCodeFromDapperTemplate(mainMethod);

            var mockupValidationProvider = new Mock<IQueryValidationProvider>();
            mockupValidationProvider
                .Setup(x => x.ValidateAsync(query, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Result.Success(ValidatedQuery.New()
                    .AddParameter("p1", typeof(int?))
                    .AddParameter("p2", typeof(string))
                    .AddParameter("p3", typeof(string)).Build())));

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.Object)
                .RegisterCallSiteAnalyzerInstance(new DapperAnalyzer())
                .Build();

            var expected = new DiagnosticResult
            {
                Id = SqlQueryAnalyzerDiagnostic.MissingParameterDiagnosticId,
                Message = "Following parameters were expected by query but were not found:" + Environment.NewLine + "p3 (string)",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 5) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ExecuteCommandMultipleTimesViaList()
        {
            var query = "INSERT INTO Person.Person(BusinessEntityID, FirstName, LastName) VALUES (@BusinessEntityID, @FirstName, @LastName)";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    connection.Execute(Sql.From(""{query}""), new List<Person> {{ new Person {{ BusinessEntityID = 1, FirstName = ""Alice""}}, new Person {{ BusinessEntityID = 2, FirstName = ""Bob"" }} }});
}}
";
            var additionalClass = @"
public class Person
{
    public int BusinessEntityID { get; set; }
    public string FirstName { get; set; }
}
";

            var source = SourceCodeTemplates.GetSourceCodeFromDapperTemplate(mainMethod, additionalClass);

            var mockupValidationProvider = new Mock<IQueryValidationProvider>();
            mockupValidationProvider
                .Setup(x => x.ValidateAsync(query, It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Result.Success(ValidatedQuery.New()
                    .AddParameter("BusinessEntityID", typeof(int?))
                    .AddParameter("FirstName", typeof(string))
                    .AddParameter("LastName", typeof(string)).Build())));

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.Object)
                .RegisterCallSiteAnalyzerInstance(new DapperAnalyzer())
                .Build();

            var expected = new DiagnosticResult
            {
                Id = SqlQueryAnalyzerDiagnostic.MissingParameterDiagnosticId,
                Message = "Following parameters were expected by query but were not found:" + Environment.NewLine + "LastName (string)",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 5) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ValidateListParameter()
        {
            var query = "DELETE FROM Person.Person WHERE BusinessEntityID IN @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    connection.Execute(Sql.From(""{query}""), new {{ p1 = new[] {{ 1, 2 }} }});
}}
";

            var source = SourceCodeTemplates.GetSourceCodeFromDapperTemplate(mainMethod);

            var mockupValidationProvider = new Mock<IQueryValidationProvider>();
            mockupValidationProvider
                .Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Result.Failure<ValidatedQuery>("Invalid query.")));
            mockupValidationProvider
                .Setup(x => x.ValidateAsync("DELETE FROM Person.Person WHERE BusinessEntityID IN (@p1, @p1__sqlaintr)", It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Result.Success(ValidatedQuery.New()
                    .AddParameter("p1", typeof(int?))
                    .AddParameter("p1__sqlaintr", typeof(int?)).Build())));

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.Object)
                .RegisterCallSiteAnalyzerInstance(new DapperAnalyzer())
                .Build();

            VerifyCSharpDiagnostic(factory, source);
        }

        // TODO provide actual diagnostic
        [Test]
        public void ValidateMissmatchedListParameter()
        {
            var query = "DELETE FROM Person.Person WHERE BusinessEntityID IN @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    connection.Execute(Sql.From(""{query}""), new {{ p1 = new[] {{ ""first"", ""second"" }} }});
}}
";

            var source = SourceCodeTemplates.GetSourceCodeFromDapperTemplate(mainMethod);

            var mockupValidationProvider = new Mock<IQueryValidationProvider>();
            mockupValidationProvider
                .Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Result.Failure<ValidatedQuery>("Invalid query.")));
            mockupValidationProvider
                .Setup(x => x.ValidateAsync("DELETE FROM Person.Person WHERE BusinessEntityID IN (@p1, @p1__sqlaintr)", It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Result.Success(ValidatedQuery.New()
                    .AddParameter("p1", typeof(int?))
                    .AddParameter("p1__sqlaintr", typeof(int?)).Build())));

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.Object)
                .RegisterCallSiteAnalyzerInstance(new DapperAnalyzer())
                .Build();

            var expected = new DiagnosticResult
            {
                Id = SqlQueryAnalyzerDiagnostic.ParameterTypeMismatchDiagnosticId,
                Message = "For parameter 'p1' expected type 'int?', but found type 'string'.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 5) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }
    }
}
