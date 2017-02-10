// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Moq;
using NUnit.Framework;
using TestHelper;
using Trestel.SqlQueryAnalyzer.Analyzers;
using Trestel.SqlQueryAnalyzer.Common;
using Trestel.SqlQueryAnalyzer.Design;
using Trestel.SqlQueryAnalyzer.Infrastructure;
using Trestel.SqlQueryAnalyzer.Infrastructure.CallSiteAnalysis;
using Trestel.SqlQueryAnalyzer.Infrastructure.QueryAnalysis;

namespace Tests
{
    [TestFixture]
    public class NonSpecificCallSiteAnalysis : SqlQueryDiagnosticVerifier
    {
        [Test]
        public void CheckStandAloneSqlStatement()
        {
            var source = @"
using System;
using System.Collections.Generic;
using Trestel.Database;
using Trestel.Database.Design;

[assembly: DatabaseHint(@""<connection string>"")]

namespace TestNamespace
{
    class Program
    {
        static void Main(string[] args)
        {
            var query = Sql.From(""SELECT BusinessEntityID, Title, FirstName, LastName FROM Person.Person"");
            Console.WriteLine(query);
        }
    }
}";
            var mockupValidationProvider = new Mock<IQueryValidationProvider>();
            mockupValidationProvider.Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(Result.Success(ValidatedQuery.New().Build())));
            var mockupCallSiteAnalyzer = new Mock<ICallSiteAnalyzer>();
            mockupCallSiteAnalyzer.Setup(x => x.CanAnalyzeCallSite(It.IsAny<CallSiteContext>())).Returns(false);

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.Object)
                .RegisterCallSiteAnalyzerInstance(mockupCallSiteAnalyzer.Object)
                .Build();

            VerifyCSharpDiagnostic(factory, source);

            // this should never be called if Sql.From is not direct argument to consuming function
            mockupCallSiteAnalyzer.Verify(x => x.NormalizeQueryDefinition(It.IsAny<CallSiteContext>()), Times.Never);
        }

        [Test]
        public void CheckStandAloneSqlStatementWithError()
        {
            var source = @"
using System;
using System.Collections.Generic;
using Trestel.Database;
using Trestel.Database.Design;

[assembly: DatabaseHint(@""<connection string>"")]

namespace TestNamespace
{
    class Program
    {
        static void Main(string[] args)
        {
            var query = Sql.From(""SELECTA BusinessEntityID, Title, FirstName, LastName FROM Person.Person"");
            Console.WriteLine(query);
        }
    }
}";
            var mockupValidationProvider = new Mock<IQueryValidationProvider>();
            mockupValidationProvider.Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(Result.Failure<ValidatedQuery>("Incorrect syntax near the keyword 'FROM'.")));
            var mockupCallSiteAnalyzer = new Mock<ICallSiteAnalyzer>();
            mockupCallSiteAnalyzer.Setup(x => x.CanAnalyzeCallSite(It.IsAny<CallSiteContext>())).Returns(false);

            var factory = ServiceFactory.New()
                .RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.Object)
                .RegisterCallSiteAnalyzerInstance(mockupCallSiteAnalyzer.Object)
                .Build();

            var expected = new DiagnosticResult
            {
                Id = SqlQueryAnalyzerDiagnostic.ErrorsInSqlQueryDiagnosticId,
                Message = "There are following errors in SQL query:" + Environment.NewLine + "Incorrect syntax near the keyword 'FROM'.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 25) }
            };

            VerifyCSharpDiagnostic(factory, source, expected);

            // this should never be called if Sql.From is not direct argument to consuming function
            mockupCallSiteAnalyzer.Verify(x => x.NormalizeQueryDefinition(It.IsAny<CallSiteContext>()), Times.Never);
        }
    }
}
