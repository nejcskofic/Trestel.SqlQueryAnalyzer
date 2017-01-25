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
    public class DapperSpecificCallSiteAnalysis : SqlQueryDiagnosticVerifier
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 5) }
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 5) }
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 5) }
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 5) }
            };
            var expected2 = new DiagnosticResult
            {
                Id = SqlQueryAnalyzerDiagnostic.MissingParameterDiagnosticId,
                Message = "Following parameters were expected by query but were not found:" + Environment.NewLine + "p1 (int?)",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 5) }
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 5) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ValidateExecuteAsync()
        {
            var query = "DELETE FROM Person.Person WHERE BusinessEntityID = @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    await connection.ExecuteAsync(Sql.From(""{query}""));
}}
";

            var source = SourceCodeTemplates.GetSourceCodeFromDapperTemplate(mainMethod, isAsync: true);

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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 26, 11) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ValidateExecuteReader()
        {
            var query = "DELETE FROM Person.Person WHERE BusinessEntityID = @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    using (var reader = connection.ExecuteReader(Sql.From(""{query}"")));
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 25) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ValidateExecuteReaderAsync()
        {
            var query = "DELETE FROM Person.Person WHERE BusinessEntityID = @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    using (var reader = await connection.ExecuteReaderAsync(Sql.From(""{query}"")));
}}
";

            var source = SourceCodeTemplates.GetSourceCodeFromDapperTemplate(mainMethod, isAsync: true);

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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 26, 31) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ValidateExecuteScalar()
        {
            var query = "SELECT FirstName FROM Person.Person WHERE BusinessEntityID = @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    var r = connection.ExecuteScalar(Sql.From(""{query}""));
    Console.WriteLine(r);
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 13) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ValidateExecuteScalarAsync()
        {
            var query = "SELECT FirstName FROM Person.Person WHERE BusinessEntityID = @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    var r = await connection.ExecuteScalarAsync(Sql.From(""{query}""));
    Console.WriteLine(r);
}}
";

            var source = SourceCodeTemplates.GetSourceCodeFromDapperTemplate(mainMethod, isAsync: true);

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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 26, 19) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ValidateQuery()
        {
            var query = "SELECT FirstName FROM Person.Person WHERE BusinessEntityID = @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    var r = connection.Query(Sql.From(""{query}"")).FirstOrDefault();
    Console.WriteLine(r);
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 13) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ValidateQueryAsync()
        {
            var query = "SELECT FirstName FROM Person.Person WHERE BusinessEntityID = @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    var r = (await connection.QueryAsync(Sql.From(""{query}""))).FirstOrDefault();
    Console.WriteLine(r);
}}
";

            var source = SourceCodeTemplates.GetSourceCodeFromDapperTemplate(mainMethod, isAsync: true);

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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 26, 20) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ValidateQueryFirst()
        {
            var query = "SELECT FirstName FROM Person.Person WHERE BusinessEntityID = @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    var r = connection.QueryFirst(Sql.From(""{query}""));
    Console.WriteLine(r);
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 13) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ValidateQueryFirstAsync()
        {
            var query = "SELECT FirstName FROM Person.Person WHERE BusinessEntityID = @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    var r = await connection.QueryFirstAsync(typeof(string), Sql.From(""{query}""));
    Console.WriteLine(r);
}}
";

            var source = SourceCodeTemplates.GetSourceCodeFromDapperTemplate(mainMethod, isAsync: true);

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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 26, 19) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ValidateQueryFirstOrDefault()
        {
            var query = "SELECT FirstName FROM Person.Person WHERE BusinessEntityID = @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    var r = connection.QueryFirstOrDefault(Sql.From(""{query}""));
    Console.WriteLine(r);
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 13) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ValidateQueryFirstOrDefaultAsync()
        {
            var query = "SELECT FirstName FROM Person.Person WHERE BusinessEntityID = @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    var r = await connection.QueryFirstOrDefaultAsync(typeof(string), Sql.From(""{query}""));
    Console.WriteLine(r);
}}
";

            var source = SourceCodeTemplates.GetSourceCodeFromDapperTemplate(mainMethod, isAsync: true);

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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 26, 19) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ValidateQueryMultiple()
        {
            var query = "SELECT FirstName FROM Person.Person WHERE BusinessEntityID = @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    using (var r = connection.QueryMultiple(Sql.From(""{query}"")));
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 20) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ValidateQueryMultipleAsync()
        {
            var query = "SELECT FirstName FROM Person.Person WHERE BusinessEntityID = @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    using (var r = await connection.QueryMultipleAsync(Sql.From(""{query}"")));
}}
";

            var source = SourceCodeTemplates.GetSourceCodeFromDapperTemplate(mainMethod, isAsync: true);

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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 26, 26) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ValidateQuerySingle()
        {
            var query = "SELECT FirstName FROM Person.Person WHERE BusinessEntityID = @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    var r = connection.QuerySingle(Sql.From(""{query}""));
    Console.WriteLine(r);
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 13) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ValidateQuerySingleAsync()
        {
            var query = "SELECT FirstName FROM Person.Person WHERE BusinessEntityID = @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    var r = await connection.QuerySingleAsync(typeof(string), Sql.From(""{query}""));
    Console.WriteLine(r);
}}
";

            var source = SourceCodeTemplates.GetSourceCodeFromDapperTemplate(mainMethod, isAsync: true);

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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 26, 19) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ValidateQuerSingleOrDefault()
        {
            var query = "SELECT FirstName FROM Person.Person WHERE BusinessEntityID = @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    var r = connection.QuerySingleOrDefault(Sql.From(""{query}""));
    Console.WriteLine(r);
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 20, 13) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }

        [Test]
        public void ValidateQuerySingleOrDefaultAsync()
        {
            var query = "SELECT FirstName FROM Person.Person WHERE BusinessEntityID = @p1";
            var mainMethod = $@"
using (var connection = new SqlConnection(""<connection string>""))
{{
    var r = await connection.QuerySingleOrDefaultAsync(typeof(string), Sql.From(""{query}""));
    Console.WriteLine(r);
}}
";

            var source = SourceCodeTemplates.GetSourceCodeFromDapperTemplate(mainMethod, isAsync: true);

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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 26, 19) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);
        }
    }
}
