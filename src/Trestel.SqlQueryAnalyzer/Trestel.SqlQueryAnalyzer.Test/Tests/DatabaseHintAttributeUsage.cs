// Copyright (c) Nejc Skofic. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using Microsoft.CodeAnalysis;
using NUnit.Framework;
using TestHelper;
using Trestel.SqlQueryAnalyzer.Analyzers;
using Trestel.SqlQueryAnalyzer.Common;
using Trestel.SqlQueryAnalyzer.Design;
using Trestel.SqlQueryAnalyzer.Infrastructure;
using Trestel.SqlQueryAnalyzer.Infrastructure.QueryAnalysis;

namespace Tests
{
    [TestFixture]
    public class DatabaseHintAttributeUsage : SqlQueryDiagnosticVerifier
    {
        [Test]
        public void UseOnMethod()
        {
            var test = @"
using System;
using Trestel.Database;
using Trestel.Database.Design;

[assembly: DatabaseHint(@""Data Source=1"")]

namespace TestNamespace
{
    [DatabaseHint(@""Data Source=2"")]
    class Program
    {
        [DatabaseHint(@""Data Source=3"")]
        static void Main(string[] args)
        {
            var sql = Sql.From(""SELECT BusinessEntityID, Title, FirstName, LastName FROM Person.Person"");
            Console.WriteLine(sql);
        }
    }
}
";
            var mockupValidationProvider = new MockupValidationProvider();
            mockupValidationProvider.AddExpectedResult(
                "SELECT BusinessEntityID, Title, FirstName, LastName FROM Person.Person",
                Result.Success(ValidatedQuery.New().Build()));

            var factory = new ServiceFactory().RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection));

            VerifyCSharpDiagnostic(factory, test);

            // verify that validation provider was called
            Assert.AreEqual(1, mockupValidationProvider.ConnectionStrings.Count, "Expected one connection string.");
            Assert.AreEqual(@"Data Source=3", mockupValidationProvider.ConnectionStrings[0], "Connection strings should match.");
        }

        [Test]
        public void UseOnClass()
        {
            var test = @"
using System;
using Trestel.Database;
using Trestel.Database.Design;

[assembly: DatabaseHint(@""Data Source=1"")]

namespace TestNamespace
{
    [DatabaseHint(@""Data Source=2"")]
    class Program
    {
        static void Main(string[] args)
        {
            var sql = Sql.From(""SELECT BusinessEntityID, Title, FirstName, LastName FROM Person.Person"");
            Console.WriteLine(sql);
        }
    }
}
";
            var mockupValidationProvider = new MockupValidationProvider();
            mockupValidationProvider.AddExpectedResult(
                "SELECT BusinessEntityID, Title, FirstName, LastName FROM Person.Person",
                Result.Success(ValidatedQuery.New().Build()));

            var factory = new ServiceFactory().RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection));

            VerifyCSharpDiagnostic(factory, test);

            // verify that validation provider was called
            Assert.AreEqual(1, mockupValidationProvider.ConnectionStrings.Count, "Expected one connection string.");
            Assert.AreEqual(@"Data Source=2", mockupValidationProvider.ConnectionStrings[0], "Connection strings shuld match.");
        }

        [Test]
        public void UseOnAssembly()
        {
            var test = @"
using System;
using Trestel.Database;
using Trestel.Database.Design;

[assembly: DatabaseHint(@""Data Source=1"")]

namespace TestNamespace
{
    class Program
    {
        static void Main(string[] args)
        {
            var sql = Sql.From(""SELECT BusinessEntityID, Title, FirstName, LastName FROM Person.Person"");
            Console.WriteLine(sql);
        }
    }
}
";
            var mockupValidationProvider = new MockupValidationProvider();
            mockupValidationProvider.AddExpectedResult(
                "SELECT BusinessEntityID, Title, FirstName, LastName FROM Person.Person",
                Result.Success(ValidatedQuery.New().Build()));

            var factory = new ServiceFactory().RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection));

            VerifyCSharpDiagnostic(factory, test);

            // verify that validation provider was called
            Assert.AreEqual(1, mockupValidationProvider.ConnectionStrings.Count, "Expected one connection string.");
            Assert.AreEqual(@"Data Source=1", mockupValidationProvider.ConnectionStrings[0], "Connection strings shuld match.");
        }

        [Test]
        public void NoAttribute()
        {
            var test = @"
using System;
using Trestel.Database;
using Trestel.Database.Design;

namespace TestNamespace
{
    class Program
    {
        static void Main(string[] args)
        {
            var sql = Sql.From(""SELECT BusinessEntityID, Title, FirstName, LastName FROM Person.Person"");
            Console.WriteLine(sql);
        }
    }
}
";
            var mockupValidationProvider = new MockupValidationProvider();
            mockupValidationProvider.AddExpectedResult(
                "SELECT BusinessEntityID, Title, FirstName, LastName FROM Person.Person",
                Result.Success(ValidatedQuery.New().Build()));

            var factory = new ServiceFactory().RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection));

            var expected = new DiagnosticResult
            {
                Id = SqlQueryAnalyzerDiagnostic.MissingDatabaseHintAttributeDiagnosticId,
                Message = "Analysis cannot continue because there is no 'Trestel.Database.Design.DatabaseHintAttribute' attribute applied to method, class or assembly.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 32) }
            };

            VerifyCSharpDiagnostic(factory, test, expected);

            // verify that validation provider was called
            Assert.AreEqual(0, mockupValidationProvider.ConnectionStrings.Count, "There should be no conection string.");
        }

        [Test]
        public void UseOnMethodWithExplicitDatabaseType()
        {
            var test = @"
using System;
using Trestel.Database;
using Trestel.Database.Design;

namespace TestNamespace
{
    class Program
    {
        [DatabaseHint(@""Data Source=3"", DatabaseType.SqlServer)]
        static void Main(string[] args)
        {
            var sql = Sql.From(""SELECT BusinessEntityID, Title, FirstName, LastName FROM Person.Person"");
            Console.WriteLine(sql);
        }
    }
}
";
            var mockupValidationProvider = new MockupValidationProvider();
            mockupValidationProvider.AddExpectedResult(
                "SELECT BusinessEntityID, Title, FirstName, LastName FROM Person.Person",
                Result.Success(ValidatedQuery.New().Build()));

            var factory = new ServiceFactory().RegisterQueryValidationProviderFactory(DatabaseType.SqlServer, (connection) => mockupValidationProvider.WithConnectionString(connection));

            VerifyCSharpDiagnostic(factory, test);

            // verify that validation provider was called
            Assert.AreEqual(1, mockupValidationProvider.ConnectionStrings.Count, "Expected one connection string.");
            Assert.AreEqual(@"Data Source=3", mockupValidationProvider.ConnectionStrings[0], "Connection strings should match.");
        }
    }
}
