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
    public partial class DapperSpecificCallSiteAnalysis
    {
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
                .Setup(x => x.ValidateAsync(query, true, It.IsAny<CancellationToken>()))
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
                .Setup(x => x.ValidateAsync(query, true, It.IsAny<CancellationToken>()))
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 25) }
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
                .Setup(x => x.ValidateAsync(query, true, It.IsAny<CancellationToken>()))
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
                .Setup(x => x.ValidateAsync(query, true, It.IsAny<CancellationToken>()))
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 13) }
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
                .Setup(x => x.ValidateAsync(query, true, It.IsAny<CancellationToken>()))
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
                .Setup(x => x.ValidateAsync(query, true, It.IsAny<CancellationToken>()))
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 13) }
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
                .Setup(x => x.ValidateAsync(query, true, It.IsAny<CancellationToken>()))
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
                .Setup(x => x.ValidateAsync(query, true, It.IsAny<CancellationToken>()))
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 13) }
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
                .Setup(x => x.ValidateAsync(query, true, It.IsAny<CancellationToken>()))
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
                .Setup(x => x.ValidateAsync(query, true, It.IsAny<CancellationToken>()))
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 13) }
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
                .Setup(x => x.ValidateAsync(query, true, It.IsAny<CancellationToken>()))
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
                .Setup(x => x.ValidateAsync(query, true, It.IsAny<CancellationToken>()))
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 20) }
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
                .Setup(x => x.ValidateAsync(query, true, It.IsAny<CancellationToken>()))
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
                .Setup(x => x.ValidateAsync(query, true, It.IsAny<CancellationToken>()))
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 13) }
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
                .Setup(x => x.ValidateAsync(query, true, It.IsAny<CancellationToken>()))
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
        public void ValidateQuerySingleOrDefault()
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
                .Setup(x => x.ValidateAsync(query, true, It.IsAny<CancellationToken>()))
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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 21, 13) }
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
                .Setup(x => x.ValidateAsync(query, true, It.IsAny<CancellationToken>()))
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
